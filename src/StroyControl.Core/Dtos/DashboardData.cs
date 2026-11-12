using StroyControl.Core.Entities;

namespace StroyControl.Core.Dtos;

/// <summary>Агрегированные показатели для главной панели приложения.</summary>
public sealed class DashboardData
{
    public int ActiveProjects { get; init; }

    public int MaterialsCount { get; init; }

    public int LowStockCount { get; init; }

    public decimal StockValue { get; init; }

    public decimal PlannedCost { get; init; }

    public decimal ActualCost { get; init; }

    public int UsagesCount { get; init; }

    public int DeliveriesCount { get; init; }

    public decimal Deviation => ActualCost - PlannedCost;

    public decimal DeviationPercent => PlannedCost == 0m ? 0m : Math.Round(Deviation / PlannedCost * 100m, 1);

    public string DeviationDirection => Deviation switch
    {
        > 0m => "перерасход",
        < 0m => "экономия",
        _ => "по смете"
    };

    public IReadOnlyList<ProjectBudgetRow> ProjectBudgets { get; init; } = Array.Empty<ProjectBudgetRow>();

    public IReadOnlyList<ActivityItem> RecentActivity { get; init; } = Array.Empty<ActivityItem>();

    public IReadOnlyList<Material> StockAlerts { get; init; } = Array.Empty<Material>();

    public IReadOnlyList<MonthlyCostRow> MonthlyCosts { get; init; } = Array.Empty<MonthlyCostRow>();

    /// <summary>Затраты по категориям материалов (кольцевая диаграмма).</summary>
    public IReadOnlyList<CategoryCostRow> CategoryCosts { get; init; } = Array.Empty<CategoryCostRow>();
}
