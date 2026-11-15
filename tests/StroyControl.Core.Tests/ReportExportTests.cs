using System.IO.Compression;
using System.Text;
using StroyControl.Core.Dtos;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;
using StroyControl.Core.Services;
using Xunit;

namespace StroyControl.Core.Tests;

public sealed class ReportExportTests
{
    [Fact]
    public void XlsxBuilder_ProducesReadableWorkbook()
    {
        var builder = new XlsxBuilder("Проверка", new double[] { 10, 20 });
        builder.AddRow(XlsxCell.Of("Заголовок", XlsxStyle.Title));
        builder.AddRow(XlsxCell.Spanning("Раздел", 2));
        builder.AddRow(XlsxCell.Of(12.5m), XlsxCell.Of("текст"));

        var bytes = builder.Build();
        Assert.NotEmpty(bytes);

        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);

        var sheet = Read(archive, "xl/worksheets/sheet1.xml");
        Assert.Contains("<row r=\"1\"", sheet);
        Assert.Contains("A2:B2", sheet);
        Assert.Contains("12.50", sheet);
    }

    [Fact]
    public void DeviationExporter_BuildsXlsxAndCsvWithTotals()
    {
        var report = new DeviationReport("Тестовый объект", new[]
        {
            new DeviationRow(1, 1, 1, "Объект", "СМ-1", "Стены", "Кирпич", "шт", 100m, 120m, 1m, 1.2m, 100m, 144m),
            new DeviationRow(1, 1, 2, "Объект", "СМ-1", "Фундамент", "Бетон", "м3", 10m, 10m, 100m, 100m, 1000m, 1000m)
        });

        var exporter = new DeviationReportExporter();
        var bytes = exporter.BuildXlsx(report);

        using (var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read))
        {
            var sheet = Read(archive, "xl/worksheets/sheet1.xml");
            Assert.Contains("ИТОГО ПО ОТЧЁТУ", sheet);
            Assert.Contains("Стены", sheet);
            Assert.Contains("Фундамент", sheet);
        }

        var csv = exporter.BuildCsv(report);
        Assert.Contains("ИТОГО ПО ОТЧЁТУ", csv);
        Assert.Contains("Кирпич", csv);
        Assert.Contains(exporter.DefaultFileName(report, "xlsx"), "Отклонения");
    }

    [Fact]
    public void ReceiptExporter_BuildsItemsAndTotal()
    {
        var material = new Material { Id = 1, Code = "ЦЕМ", Name = "Цемент", Unit = UnitOfMeasure.Kilogram };
        var delivery = new Delivery
        {
            Number = "ПН-26-1105-001",
            Date = new DateTime(2026, 11, 5),
            Items = new List<DeliveryItem>
            {
                new() { MaterialId = 1, Material = material, Quantity = 10m, UnitPrice = 5m },
                new() { MaterialId = 1, Material = material, Quantity = 2m, UnitPrice = 7m }
            }
        };

        var exporter = new ReceiptExporter();
        var csv = exporter.BuildCsv(delivery, "ООО «СтройБаза»");

        Assert.Contains("Товарный чек", csv);
        Assert.Contains("ИТОГО;;;;64.00", csv);
        Assert.Contains("ПН-26-1105-001", exporter.DefaultFileName(delivery, "pdf"));

        using var archive = new ZipArchive(new MemoryStream(exporter.BuildXlsx(delivery, "ООО «СтройБаза»")), ZipArchiveMode.Read);
        Assert.Contains("Цемент", Read(archive, "xl/worksheets/sheet1.xml"));
    }

    private static string Read(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path);
        Assert.NotNull(entry);

        using var reader = new StreamReader(entry!.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
