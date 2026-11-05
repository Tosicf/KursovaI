using StroyControl.Core.Dtos;
using StroyControl.Core.Enums;
using Xunit;

namespace StroyControl.Core.Tests;

public sealed class DeviationCalculatorTests
{
    [Theory]
    [InlineData(0, DeviationSeverity.Normal)]
    [InlineData(4.9, DeviationSeverity.Normal)]
    [InlineData(5, DeviationSeverity.Warning)]
    [InlineData(9.99, DeviationSeverity.Warning)]
    [InlineData(10, DeviationSeverity.Critical)]
    [InlineData(35.4, DeviationSeverity.Critical)]
    public void Classify_ReturnsSeverityByThreshold(decimal percent, DeviationSeverity expected)
    {
        Assert.Equal(expected, DeviationCalculator.Classify(percent));
    }

    [Fact]
    public void CreateRow_ComputesCostsAndPriceDeviation()
    {
        var row = DeviationCalculator.CreateRow(
            projectId: 1,
            estimateId: 2,
            estimateItemId: 3,
            projectName: "ЖК «Северный берег»",
            estimateNumber: "СМ-01/25",
            section: "Фундамент",
            materialName: "Бетон B25",
            unit: "м³",
            plannedQuantity: 100m,
            plannedPrice: 200m,
            actualQuantity: 112m,
            actualCost: 24640m);

        Assert.Equal(20_000m, row.PlannedCost);
        Assert.Equal(24_640m, row.ActualCost);
        Assert.Equal(220m, row.ActualPrice);
        Assert.Equal(4_640m, row.CostDeviation);
        Assert.Equal(23.2m, row.DeviationPercent);
        Assert.Equal(DeviationSeverity.Critical, row.Severity);
        Assert.True(row.IsOverrun);
    }

    [Fact]
    public void CreateRow_WithZeroPlan_DoesNotDivideByZero()
    {
        var row = DeviationCalculator.CreateRow(1, 1, 1, "Объект", "СМ-1", "Раздел", "Песок", "т", 0m, 0m, 0m, 0m);

        Assert.Equal(0m, row.DeviationPercent);
        Assert.Equal(DeviationSeverity.Normal, row.Severity);
        Assert.False(row.IsOverrun);
    }

    [Fact]
    public void CreateRow_EconomyIsNegativeDeviation()
    {
        var row = DeviationCalculator.CreateRow(1, 1, 1, "Объект", "СМ-1", "Отделка", "Краска", "кг", 100m, 10m, 90m, 850m);

        Assert.Equal(-150m, row.CostDeviation);
        Assert.Equal(-15m, row.DeviationPercent);
        Assert.Equal("Экономия", row.DirectionDisplay);
    }

    [Fact]
    public void WeightedAveragePrice_UsesQuantitiesAsWeights()
    {
        var price = DeviationCalculator.WeightedAveragePrice(new[]
        {
            (10m, 100m),
            (30m, 120m)
        });

        Assert.Equal(115m, price);
    }

    [Fact]
    public void Report_AggregatesTotalsAndCounts()
    {
        var rows = new List<DeviationRow>
        {
            DeviationCalculator.CreateRow(1, 1, 1, "О1", "СМ-1", "Раздел", "Материал 1", "шт", 100m, 10m, 130m, 1300m),
            DeviationCalculator.CreateRow(1, 1, 2, "О1", "СМ-1", "Раздел", "Материал 2", "шт", 100m, 10m, 95m, 950m),
            DeviationCalculator.CreateRow(1, 1, 3, "О1", "СМ-1", "Раздел", "Материал 3", "шт", 100m, 10m, 102m, 1020m)
        };

        var report = new DeviationReport("О1", rows);

        Assert.Equal(3000m, report.PlannedTotal);
        Assert.Equal(3270m, report.ActualTotal);
        Assert.Equal(270m, report.CostDeviation);
        Assert.Equal(1, report.CriticalCount);
        Assert.Equal(2, report.OverrunCount);
        Assert.Equal(1, report.EconomyCount);
    }
}
