using StroyControl.Core.Dtos;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;
using StroyControl.Core.Services;
using Xunit;

namespace StroyControl.Core.Tests;

public sealed class EstimateReportBuilderTests
{
    private static Estimate CreateEstimate(decimal area = 100m)
    {
        var project = new Project
        {
            Id = 1,
            Code = "ОБ-2025-01",
            Name = "ЖК «Северный берег»",
            Address = "г. Минск",
            Customer = "ОАО «Городской квартал»",
            TotalArea = area
        };

        var estimate = new Estimate
        {
            Id = 10,
            Number = "СМ-01/25",
            Name = "Смета на фундамент",
            Project = project,
            ProjectId = 1,
            Status = EstimateStatus.Approved,
            MarkupPercent = 10m,
            ProfitPercent = 12m,
            LaborSharePercent = 35m
        };

        var concrete = new Material { Id = 1, Name = "Бетон B25", Unit = UnitOfMeasure.CubicMeter, Price = 200m };

        estimate.Items.Add(new EstimateItem
        {
            Id = 1,
            EstimateId = 10,
            MaterialId = 1,
            Material = concrete,
            Section = "Фундамент",
            PlannedQuantity = 10m,
            PlannedUnitPrice = 200m
        });

        estimate.Items.Add(new EstimateItem
        {
            Id = 2,
            EstimateId = 10,
            IsWork = true,
            WorkName = "Армирование фундаментной плиты",
            WorkUnit = "т",
            Section = "Фундамент",
            PlannedQuantity = 2m,
            PlannedUnitPrice = 640m
        });

        return estimate;
    }

    [Fact]
    public void Build_AppliesMarkupToUnitPrice()
    {
        var report = EstimateReportBuilder.Build(CreateEstimate());

        var material = report.Rows.Single(r => r.IsMaterial);

        Assert.Equal(220m, material.PriceWithMarkup);   // 200 + 10 %
        Assert.Equal(2200m, material.Total);            // 10 × 220
    }

    [Fact]
    public void Build_SeparatesWorksAndMaterials()
    {
        var report = EstimateReportBuilder.Build(CreateEstimate());
        var group = report.Groups.Single();

        Assert.True(group.HasWorks);
        Assert.True(group.HasMaterials);
        Assert.Single(group.Works);
        Assert.Single(group.Materials);
        Assert.Equal("Работа", group.Works[0].KindDisplay);
        Assert.Equal("Материал", group.Materials[0].KindDisplay);

        // Внутри раздела сначала идут работы, затем материалы.
        Assert.True(group.Rows[0].IsWork);
    }

    [Fact]
    public void Build_ComputesProfitAndLaborOnlyForWorks()
    {
        var report = EstimateReportBuilder.Build(CreateEstimate());

        var material = report.Rows.Single(r => r.IsMaterial);
        var work = report.Rows.Single(r => r.IsWork);

        Assert.Equal(264m, material.Profit);            // 2200 × 12 %
        Assert.Equal(0m, material.LaborCost);

        Assert.Equal(1408m, work.Total);                // 2 × 640 × 1,10
        Assert.Equal(168.96m, work.Profit);             // 1408 × 12 %
        Assert.Equal(492.80m, work.LaborCost);          // 1408 × 35 %
    }

    [Fact]
    public void Build_ComputesTotalsAndSquareMeterPrices()
    {
        var report = EstimateReportBuilder.Build(CreateEstimate(area: 100m));

        Assert.Equal(2200m, report.MaterialsCost);
        Assert.Equal(1408m, report.WorksCost);
        Assert.Equal(3608m, report.DirectCost);
        Assert.Equal(432.96m, report.Profit);           // 264 + 168,96
        Assert.Equal(4040.96m, report.TotalCost);       // прямые затраты + прибыль
        Assert.Equal(40.41m, report.CostPerSquareMeter);
        Assert.Equal(4.33m, report.ProfitPerSquareMeter);
    }

    [Fact]
    public void Build_WithoutArea_DoesNotDivideByZero()
    {
        var report = EstimateReportBuilder.Build(CreateEstimate(area: 0m));

        Assert.Equal(0m, report.CostPerSquareMeter);
        Assert.Equal(0m, report.ProfitPerSquareMeter);
    }

    [Fact]
    public void Build_OverridesCoefficientsFromArguments()
    {
        var report = EstimateReportBuilder.Build(CreateEstimate(), area: 50m, markupPercent: 0m, profitPercent: 0m, laborSharePercent: 100m);

        Assert.Equal(200m, report.Rows.Single(r => r.IsMaterial).PriceWithMarkup);
        Assert.Equal(0m, report.Profit);
        Assert.Equal(1280m, report.LaborCost);          // 2 × 640, вся сумма рабочим
    }

    [Fact]
    public void Exporter_BuildsCsvWithHeaderAndRows()
    {
        var report = EstimateReportBuilder.Build(CreateEstimate());
        var csv = new EstimateReportExporter().BuildCsv(report);

        Assert.Contains("Наименование работ и затрат", csv);
        Assert.Contains("Работы", csv);
        Assert.Contains("Материалы", csv);
        Assert.Contains("Армирование фундаментной плиты", csv);
    }

    [Fact]
    public void Exporter_BuildsXlsxArchive()
    {
        var report = EstimateReportBuilder.Build(CreateEstimate());
        var bytes = new EstimateReportExporter().BuildWorkbook(report);

        Assert.True(bytes.Length > 2000);
        Assert.Equal(0x50, bytes[0]);   // «PK» — zip-архив OOXML
        Assert.Equal(0x4B, bytes[1]);
    }
}
