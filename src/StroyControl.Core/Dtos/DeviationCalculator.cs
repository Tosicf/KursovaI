using StroyControl.Core.Dtos;
using StroyControl.Core.Enums;

namespace StroyControl.Core.Dtos;

/// <summary>
/// Чистая бизнес-логика анализа отклонений: расчёт процентов и классификация.
/// Выделена в отдельный класс, чтобы её можно было покрыть модульными тестами
/// независимо от WPF и СУБД.
/// </summary>
public static class DeviationCalculator
{
    /// <summary>Порог «внимание», %.</summary>
    public const decimal WarningThreshold = 5m;

    /// <summary>Порог «критично», %.</summary>
    public const decimal CriticalThreshold = 10m;

    public static DeviationSeverity Classify(decimal absolutePercent)
    {
        if (absolutePercent >= CriticalThreshold) return DeviationSeverity.Critical;
        if (absolutePercent >= WarningThreshold) return DeviationSeverity.Warning;
        return DeviationSeverity.Normal;
    }

    /// <summary>Строит строку анализа по плановым и фактическим значениям.</summary>
    public static DeviationRow CreateRow(
        int projectId,
        int estimateId,
        int estimateItemId,
        string projectName,
        string estimateNumber,
        string section,
        string materialName,
        string unit,
        decimal plannedQuantity,
        decimal plannedPrice,
        decimal actualQuantity,
        decimal actualCost)
    {
        var actualPrice = actualQuantity == 0m ? 0m : Math.Round(actualCost / actualQuantity, 2);
        return new DeviationRow(
            projectId,
            estimateId,
            estimateItemId,
            projectName,
            estimateNumber,
            section,
            materialName,
            unit,
            plannedQuantity,
            actualQuantity,
            plannedPrice,
            actualPrice,
            Math.Round(plannedQuantity * plannedPrice, 2),
            Math.Round(actualCost, 2));
    }

    /// <summary>Средневзвешенная цена по набору пар «количество — цена».</summary>
    public static decimal WeightedAveragePrice(IEnumerable<(decimal Quantity, decimal UnitPrice)> lots)
    {
        decimal quantity = 0m;
        decimal total = 0m;
        foreach (var lot in lots)
        {
            quantity += lot.Quantity;
            total += lot.Quantity * lot.UnitPrice;
        }

        return quantity == 0m ? 0m : Math.Round(total / quantity, 2);
    }
}
