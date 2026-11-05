using Microsoft.EntityFrameworkCore;
using StroyControl.Core.Dtos;
using StroyControl.Core.Enums;
using StroyControl.Core.Interfaces;

namespace StroyControl.Data.Services;

/// <summary>Собирает показатели для главной панели: KPI, бюджет объектов, лента операций, остатки.</summary>
public sealed class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public DashboardService(IDbContextFactory<AppDbContext> factory) => _factory = factory;

    public async Task<DashboardData> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);

        var projects = await db.Projects.AsNoTracking().ToListAsync(cancellationToken);
        var materials = await db.Materials.AsNoTracking().Include(m => m.Supplier).ToListAsync(cancellationToken);
        var estimates = await db.Estimates.AsNoTracking().Include(e => e.Items).ToListAsync(cancellationToken);
        var deliveries = await db.Deliveries
            .AsNoTracking()
            .Include(d => d.Supplier)
            .Include(d => d.Items)
            .ToListAsync(cancellationToken);
        var usages = await db.MaterialUsages
            .AsNoTracking()
            .Include(u => u.Material)
            .Include(u => u.Project)
            .ToListAsync(cancellationToken);

        var plannedByProject = estimates
            .GroupBy(e => e.ProjectId)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Items.Sum(i => i.PlannedQuantity * i.PlannedUnitPrice)));

        var actualByProject = usages
            .GroupBy(u => u.ProjectId)
            .ToDictionary(g => g.Key, g => g.Sum(u => u.Quantity * u.UnitPrice));

        var budgets = projects
            .Select(p => new ProjectBudgetRow(
                p.Id,
                p.Code,
                p.Name,
                plannedByProject.TryGetValue(p.Id, out var planned) ? planned : 0m,
                actualByProject.TryGetValue(p.Id, out var actual) ? actual : 0m,
                p.Status))
            .OrderByDescending(b => b.PlannedCost)
            .ToList();

        var activity = usages
            .Select(u => new ActivityItem(
                u.Date,
                $"Списание · {u.Material?.Name ?? "материал"}",
                $"{u.Project?.Name ?? "—"} · {u.Quantity:N2} {u.Material?.UnitDisplay} · {u.DocumentNumber}",
                "usage"))
            .Concat(deliveries.Select(d => new ActivityItem(
                d.Date,
                $"Поступление · {d.Number}",
                $"{d.Supplier?.Name ?? "—"} · {d.ItemsCount} поз. · {d.Total:N2} BYN",
                "delivery")))
            .OrderByDescending(a => a.Date)
            .Take(8)
            .ToList();

        var alerts = materials
            .Where(m => m.IsActive && m.StockQuantity <= m.MinStock)
            .OrderBy(m => m.StockQuantity)
            .ThenBy(m => m.Name)
            .Take(6)
            .ToList();

        var monthly = BuildMonthlySeries(usages
            .Select(u => (u.Date, Cost: u.Quantity * u.UnitPrice))
            .ToList());

        var plannedTotal = plannedByProject.Values.Sum();

        return new DashboardData
        {
            ActiveProjects = projects.Count(p => p.Status is ProjectStatus.InProgress or ProjectStatus.Planning),
            MaterialsCount = materials.Count,
            LowStockCount = materials.Count(m => m.IsActive && m.StockQuantity <= m.MinStock),
            StockValue = materials.Sum(m => m.StockQuantity * m.Price),
            PlannedCost = plannedTotal,
            ActualCost = actualByProject.Values.Sum(),
            UsagesCount = usages.Count,
            DeliveriesCount = deliveries.Count,
            ProjectBudgets = budgets,
            RecentActivity = activity,
            StockAlerts = alerts,
            MonthlyCosts = monthly
        };
    }

    /// <summary>Факт затрат по месяцам и среднемесячный план (смета / 6 месяцев).</summary>
    private static IReadOnlyList<MonthlyCostRow> BuildMonthlySeries(IReadOnlyList<(DateTime Date, decimal Cost)> usages)
    {
        var today = DateTime.Today;
        var months = Enumerable.Range(0, 6)
            .Select(i => new DateTime(today.Year, today.Month, 1).AddMonths(-5 + i))
            .ToList();

        var actual = usages
            .GroupBy(u => new DateTime(u.Date.Year, u.Date.Month, 1))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Cost));

        var total = actual.Values.Sum();
        var plannedMonthly = months.Count == 0 ? 0m : Math.Round(total / months.Count / 1.05m, 2);

        return months
            .Select(m => new MonthlyCostRow(
                m,
                plannedMonthly,
                actual.TryGetValue(m, out var cost) ? Math.Round(cost, 2) : 0m))
            .ToList();
    }
}
