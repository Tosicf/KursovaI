using StroyControl.Core.Enums;

namespace StroyControl.Core.Dtos;

/// <summary>Строка анализа отклонений: план сметы против фактического расхода и закупки.</summary>
public sealed record DeviationRow(
    int ProjectId,
    int EstimateId,
    int EstimateItemId,
    string ProjectName,
    string EstimateNumber,
    string Section,
    string MaterialName,
    string Unit,
    decimal PlannedQuantity,
    decimal ActualQuantity,
    decimal PlannedPrice,
    decimal ActualPrice,
    decimal PlannedCost,
    decimal ActualCost)
{
    /// <summary>Отклонение по количеству (факт − план).</summary>
    public decimal QuantityDeviation => ActualQuantity - PlannedQuantity;

    /// <summary>Отклонение по цене (фактическая − сметная).</summary>
    public decimal PriceDeviation => ActualPrice - PlannedPrice;

    /// <summary>Отклонение по стоимости (факт − план). Положительное значение — перерасход.</summary>
    public decimal CostDeviation => ActualCost - PlannedCost;

    /// <summary>Процент отклонения стоимости относительно сметы.</summary>
    public decimal DeviationPercent => PlannedCost == 0m
        ? ActualCost == 0m ? 0m : 100m
        : Math.Round(CostDeviation / PlannedCost * 100m, 1);

    /// <summary>Критичность отклонения (порог 5 % и 10 %).</summary>
    public DeviationSeverity Severity => DeviationCalculator.Classify(Math.Abs(DeviationPercent));

    public string SeverityDisplay => Severity.ToDisplayName();

    /// <summary>Текстовое направление отклонения для отчёта.</summary>
    public string DirectionDisplay => CostDeviation switch
    {
        > 0m => "Перерасход",
        < 0m => "Экономия",
        _ => "Соответствует смете"
    };

    public bool IsOverrun => CostDeviation > 0m;
}
