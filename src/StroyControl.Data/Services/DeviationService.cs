using Microsoft.EntityFrameworkCore;
using StroyControl.Core.Dtos;
using StroyControl.Core.Interfaces;

namespace StroyControl.Data.Services;

/// <summary>
/// Анализ отклонений: сопоставляет позиции смет (план) с фактическими списаниями
/// материалов на объект. Расчёт сосредоточен в <see cref="DeviationCalculator"/>.
/// </summary>
public sealed class DeviationService : IDeviationService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public DeviationService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<DeviationReport> BuildReportAsync(int? projectId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var estimatesQuery = db.Estimates
            .AsNoTracking()
            .Include(e => e.Project)
            .Include(e => e.Items)
            .ThenInclude(i => i.Material)
            .AsQueryable();

        if (projectId.HasValue)
            estimatesQuery = estimatesQuery.Where(e => e.ProjectId == projectId.Value);

        var estimates = await estimatesQuery.ToListAsync(cancellationToken);

        var usageQuery = db.MaterialUsages.AsNoTracking();
        if (projectId.HasValue)
            usageQuery = usageQuery.Where(u => u.ProjectId == projectId.Value);

        var usages = await usageQuery
            .Select(u => new { u.ProjectId, u.MaterialId, u.Quantity, u.UnitPrice })
            .ToListAsync(cancellationToken);

        var actualByMaterial = usages
            .GroupBy(u => (u.ProjectId, u.MaterialId))
            .ToDictionary(
                g => g.Key,
                g => (Quantity: g.Sum(x => x.Quantity), Cost: g.Sum(x => x.Quantity * x.UnitPrice)));

        // Плановые показатели агрегируются по паре «объект — материал», чтобы один
        // материал, встречающийся в нескольких сметах, не учитывался дважды.
        var plannedGroups = estimates
            .SelectMany(e => e.Items.Select(i => new { Estimate = e, Item = i }))
            .Where(x => x.Estimate.Project is not null)
            .GroupBy(x => new { ProjectId = x.Estimate.ProjectId, x.Item.MaterialId })
            .ToList();

        var rows = new List<DeviationRow>(plannedGroups.Count);
        foreach (var group in plannedGroups)
        {
            var first = group.First();
            var plannedQuantity = group.Sum(x => x.Item.PlannedQuantity);
            var plannedCost = group.Sum(x => x.Item.PlannedQuantity * x.Item.PlannedUnitPrice);
            var plannedPrice = plannedQuantity == 0m ? first.Item.PlannedUnitPrice : Math.Round(plannedCost / plannedQuantity, 2);

            actualByMaterial.TryGetValue((group.Key.ProjectId, group.Key.MaterialId), out var actual);

            var sections = string.Join(", ", group
                .Select(x => x.Item.SectionDisplay)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Distinct());
            var estimateNumbers = string.Join(", ", group.Select(x => x.Estimate.Number).Distinct());

            rows.Add(DeviationCalculator.CreateRow(
                projectId: group.Key.ProjectId,
                estimateId: first.Estimate.Id,
                estimateItemId: first.Item.Id,
                projectName: first.Estimate.Project?.Name ?? "—",
                estimateNumber: estimateNumbers,
                section: sections,
                materialName: first.Item.Material?.Name ?? "—",
                unit: first.Item.Material?.UnitDisplay ?? string.Empty,
                plannedQuantity: plannedQuantity,
                plannedPrice: plannedPrice,
                actualQuantity: actual.Quantity,
                actualCost: actual.Cost));
        }

        var scopeName = projectId.HasValue
            ? estimates.FirstOrDefault()?.Project?.Name ?? "Объект"
            : "Все объекты";

        return new DeviationReport(scopeName, rows
            .OrderByDescending(r => Math.Abs(r.CostDeviation))
            .ThenBy(r => r.MaterialName)
            .ToList());
    }
}
