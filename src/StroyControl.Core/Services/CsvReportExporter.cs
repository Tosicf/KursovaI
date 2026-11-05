using System.Globalization;
using System.Text;
using StroyControl.Core.Dtos;

namespace StroyControl.Core.Services;

/// <summary>Экспорт отчёта об отклонениях в CSV (открывается в Excel, разделитель «;»).</summary>
public sealed class CsvReportExporter
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("ru-RU");

    public string BuildDeviationCsv(DeviationReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Отчёт об отклонениях от сметы;{report.ScopeName};{DateTime.Now:dd.MM.yyyy HH:mm}");
        sb.AppendLine("Объект;Смета;Раздел;Материал;Ед.;План, кол-во;Факт, кол-во;Отклонение, кол-во;План, цена;Факт, цена;План, сумма;Факт, сумма;Отклонение, сумма;Отклонение, %;Статус");

        foreach (var row in report.Rows)
        {
            sb.AppendLine(string.Join(';',
                Escape(row.ProjectName),
                Escape(row.EstimateNumber),
                Escape(row.Section),
                Escape(row.MaterialName),
                Escape(row.Unit),
                Number(row.PlannedQuantity),
                Number(row.ActualQuantity),
                Number(row.QuantityDeviation),
                Number(row.PlannedPrice),
                Number(row.ActualPrice),
                Number(row.PlannedCost),
                Number(row.ActualCost),
                Number(row.CostDeviation),
                Number(row.DeviationPercent),
                Escape(row.SeverityDisplay)));
        }

        sb.AppendLine();
        sb.AppendLine($"Итого по смете;;;{Number(report.PlannedTotal)};;;{Number(report.ActualTotal)};;;;{Number(report.CostDeviation)};{Number(report.DeviationPercent)};");
        return sb.ToString();
    }

    public async Task ExportDeviationReportAsync(DeviationReport report, string filePath, CancellationToken cancellationToken = default)
    {
        var csv = BuildDeviationCsv(report);
        await File.WriteAllTextAsync(filePath, csv, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), cancellationToken);
    }

    public string DefaultFileName(DeviationReport report) =>
        $"Отклонения_{Sanitize(report.ScopeName)}_{DateTime.Now:yyyyMMdd_HHmm}.csv";

    private static string Number(decimal value) => value.ToString("0.##", Culture);

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var cleaned = value.Replace(';', ',').Replace('\n', ' ').Replace('\r', ' ');
        return cleaned;
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }
}
