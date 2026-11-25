using StroyControl.Core.Services;

namespace StroyControl.Core.Dtos;

/// <summary>Затраты на материалы по месяцам (для мини-диаграммы на панели управления).</summary>
public sealed record MonthlyCostRow(DateTime Month, decimal PlannedCost, decimal ActualCost)
{
    /// <summary>Подпись месяца на графике: «Ноя 26» — короткая и с годом.</summary>
    public string MonthDisplay => $"{MonthPeriod.ShortName(Month.Month)} {Month:yy}";

    public decimal Deviation => ActualCost - PlannedCost;

    public double ActualPercent => PlannedCost == 0m
        ? ActualCost > 0m ? 100d : 0d
        : Math.Min(100d, (double)Math.Round(ActualCost / PlannedCost * 100m, 0));
}
