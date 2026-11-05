using StroyControl.Core.Enums;

namespace StroyControl.Core.Dtos;

/// <summary>Исполнение бюджета объекта: смета против фактических затрат.</summary>
public sealed record ProjectBudgetRow(
    int ProjectId,
    string Code,
    string Name,
    decimal PlannedCost,
    decimal ActualCost,
    ProjectStatus Status)
{
    public decimal Deviation => ActualCost - PlannedCost;

    public bool IsOverrun => Deviation > 0m;

    public decimal DeviationPercent => PlannedCost == 0m ? 0m : Math.Round(Deviation / PlannedCost * 100m, 1);

    public double ProgressPercent => PlannedCost == 0m
        ? 0d
        : Math.Min(100d, (double)Math.Round(ActualCost / PlannedCost * 100m, 1));

    public string StatusDisplay => Status.ToDisplayName();
}
