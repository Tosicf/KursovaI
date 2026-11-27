using Microsoft.EntityFrameworkCore;
using StroyControl.Core.Dtos;
using StroyControl.Core.Enums;
using StroyControl.Core.Interfaces;

namespace StroyControl.Data.Services;

/// <summary>
/// Минимальный запас из смет: суммирует плановые количества материалов по действующим
/// сметам (черновик, на проверке, утверждена — архив не учитывается) и вычитает остаток
/// на складе. Материалы вне смет не трогаются — их минимум остаётся экспертным.
/// </summary>
public sealed class MinStockService : IMinStockService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public MinStockService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<IReadOnlyList<MinStockSuggestion>> BuildSuggestionsAsync(int? projectId = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var items = db.EstimateItems
            .AsNoTracking()
            .Where(i => !i.IsWork && i.MaterialId != null)
            // Потребность считаем только по сметам в работе: черновик и смета на
            // проверке ещё могут измениться, архивная — уже выполнена.
            .Where(i => i.Estimate != null && i.Estimate.Status == EstimateStatus.InWork);

        if (projectId.HasValue)
            items = items.Where(i => i.Estimate!.ProjectId == projectId.Value);

        var needs = await items
            .GroupBy(i => i.MaterialId!.Value)
            .Select(g => new { MaterialId = g.Key, Quantity = g.Sum(i => i.PlannedQuantity) })
            .ToListAsync(cancellationToken);

        if (needs.Count == 0)
            return Array.Empty<MinStockSuggestion>();

        var ids = needs.Select(n => n.MaterialId).ToList();
        var materials = await db.Materials
            .AsNoTracking()
            .Where(m => ids.Contains(m.Id))
            .ToListAsync(cancellationToken);

        var byId = materials.ToDictionary(m => m.Id);
        var suggestions = new List<MinStockSuggestion>(needs.Count);

        foreach (var need in needs)
        {
            if (!byId.TryGetValue(need.MaterialId, out var material)) continue;

            var suggested = MinStockCalculator.SuggestedMinStock(need.Quantity, material.StockQuantity);

            suggestions.Add(new MinStockSuggestion(
                material.Id,
                material.Code,
                material.Name,
                material.UnitDisplay,
                material.StockQuantity,
                Math.Round(need.Quantity, 3, MidpointRounding.AwayFromZero),
                material.MinStock,
                suggested));
        }

        return suggestions
            .OrderByDescending(s => s.Delta)
            .ThenBy(s => s.Name, StringComparer.CurrentCulture)
            .ToList();
    }

    public async Task<int> RecalculateAsync(int? projectId = null, CancellationToken cancellationToken = default)
    {
        var suggestions = await BuildSuggestionsAsync(projectId, cancellationToken);
        var changes = suggestions.Where(s => s.HasChanges).ToList();

        return changes.Count == 0 ? 0 : await ApplyAsync(changes, cancellationToken);
    }

    public async Task<int> ApplyAsync(IReadOnlyList<MinStockSuggestion> suggestions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(suggestions);

        var ids = suggestions.Select(s => s.MaterialId).Distinct().ToList();
        if (ids.Count == 0) return 0;

        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var materials = await db.Materials
            .Where(m => ids.Contains(m.Id))
            .ToListAsync(cancellationToken);

        var byId = suggestions
            .GroupBy(s => s.MaterialId)
            .ToDictionary(g => g.Key, g => g.First().SuggestedMinStock);

        var updated = 0;
        foreach (var material in materials)
        {
            if (!byId.TryGetValue(material.Id, out var suggested)) continue;
            if (material.MinStock == suggested) continue;

            material.MinStock = suggested;
            updated++;
        }

        if (updated > 0)
            await db.SaveChangesAsync(cancellationToken);

        return updated;
    }
}
