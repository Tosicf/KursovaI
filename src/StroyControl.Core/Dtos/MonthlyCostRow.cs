namespace StroyControl.Core.Dtos;

/// <summary>Затраты на материалы по месяцам (для мини-диаграммы на панели управления).</summary>
public sealed record MonthlyCostRow(DateTime Month, decimal PlannedCost, decimal ActualCost)
{
    public string MonthDisplay => Month.ToString("MMM");

    public decimal Deviation => ActualCost - PlannedCost;

    public double ActualPercent => PlannedCost == 0m
        ? ActualCost > 0m ? 100d : 0d
        : Math.Min(100d, (double)Math.Round(ActualCost / PlannedCost * 100m, 0));
}
