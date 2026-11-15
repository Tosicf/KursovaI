using System.Globalization;
using System.Text;
using StroyControl.Core.Dtos;
using StroyControl.Core.Enums;

namespace StroyControl.Core.Services;

/// <summary>
/// Экспорт анализа отклонений в Excel (.xlsx) и CSV без сторонних библиотек.
/// Структура повторяет печатный отчёт: шапка, сводка, позиции по разделам сметы,
/// подытоги по разделам и итог по всему отчёту.
/// </summary>
public sealed class DeviationReportExporter
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    /// <summary>Имя файла по умолчанию: «Отклонения_Все_объекты_20261105_1830.xlsx».</summary>
    public string DefaultFileName(DeviationReport report, string extension = "xlsx") =>
        $"Отклонения_{Sanitize(report.ScopeName)}_{DateTime.Now:yyyyMMdd_HHmm}.{extension}";

    public async Task ExportXlsxAsync(DeviationReport report, string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        await File.WriteAllBytesAsync(filePath, BuildXlsx(report), cancellationToken);
    }

    public byte[] BuildXlsx(DeviationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new XlsxBuilder("Отклонения", new double[] { 5, 44, 9, 12, 12, 12, 12, 15, 15, 15, 11, 12 }, frozenRows: 10);

        builder.AddRow(XlsxCell.Of("Анализ отклонений фактического расхода от сметы", XlsxStyle.Title));
        builder.AddRow(XlsxCell.Of($"Область: {report.ScopeName}"));
        builder.AddRow(XlsxCell.Of($"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm} · позиций: {report.Rows.Count}"));
        builder.AddEmptyRow();

        builder.AddRow(
            XlsxCell.Of("Сметная стоимость, BYN", XlsxStyle.Bold), XlsxCell.Of(report.PlannedTotal),
            XlsxCell.Of("Фактические затраты, BYN", XlsxStyle.Bold), XlsxCell.Of(report.ActualTotal));
        builder.AddRow(
            XlsxCell.Of("Отклонение, BYN", XlsxStyle.Bold), XlsxCell.Of(report.CostDeviation),
            XlsxCell.Of("Отклонение, %", XlsxStyle.Bold), XlsxCell.Of(report.DeviationPercent));
        builder.AddRow(
            XlsxCell.Of("Сводная оценка", XlsxStyle.Bold), XlsxCell.Of(report.Severity.ToDisplayName()),
            XlsxCell.Of("Позиций с перерасходом", XlsxStyle.Bold), XlsxCell.Of(report.OverrunCount, XlsxStyle.Center));
        builder.AddRow(
            XlsxCell.Of("Внимание (5–10 %)", XlsxStyle.Bold), XlsxCell.Of(report.WarningCount, XlsxStyle.Center),
            XlsxCell.Of("Критично (≥ 10 %)", XlsxStyle.Bold), XlsxCell.Of(report.CriticalCount, XlsxStyle.Center));
        builder.AddEmptyRow();

        builder.AddRow(
            XlsxCell.Of("№", XlsxStyle.Header),
            XlsxCell.Of("Материал", XlsxStyle.Header),
            XlsxCell.Of("ед.", XlsxStyle.Header),
            XlsxCell.Of("кол-во план", XlsxStyle.Header),
            XlsxCell.Of("кол-во факт", XlsxStyle.Header),
            XlsxCell.Of("цена план", XlsxStyle.Header),
            XlsxCell.Of("цена факт", XlsxStyle.Header),
            XlsxCell.Of("стоимость план", XlsxStyle.Header),
            XlsxCell.Of("стоимость факт", XlsxStyle.Header),
            XlsxCell.Of("отклонение", XlsxStyle.Header),
            XlsxCell.Of("откл., %", XlsxStyle.Header),
            XlsxCell.Of("оценка", XlsxStyle.Header));

        var number = 1;
        var sectionNumber = 1;

        foreach (var section in GroupSections(report))
        {
            builder.AddRow(XlsxCell.Spanning($"{sectionNumber++}. {section.Name}", 12));

            foreach (var row in section.Rows)
            {
                builder.AddRow(
                    XlsxCell.Of(number++.ToString(Russian), XlsxStyle.Center),
                    XlsxCell.Of(row.MaterialName),
                    XlsxCell.Of(row.Unit, XlsxStyle.Center),
                    XlsxCell.Of(row.PlannedQuantity),
                    XlsxCell.Of(row.ActualQuantity),
                    XlsxCell.Of(row.PlannedPrice),
                    XlsxCell.Of(row.ActualPrice),
                    XlsxCell.Of(row.PlannedCost),
                    XlsxCell.Of(row.ActualCost),
                    XlsxCell.Of(row.CostDeviation),
                    XlsxCell.Of(row.DeviationPercent, XlsxStyle.Center),
                    XlsxCell.Of(row.SeverityDisplay, XlsxStyle.Center));
            }

            builder.AddRow(
                XlsxCell.Spanning($"Итого по разделу «{section.Name}»", 7, XlsxStyle.Subtotal),
                XlsxCell.Of(section.Planned, XlsxStyle.Subtotal),
                XlsxCell.Of(section.Actual, XlsxStyle.Subtotal),
                XlsxCell.Of(section.Deviation, XlsxStyle.Subtotal),
                XlsxCell.Of(section.Percent, XlsxStyle.Subtotal),
                XlsxCell.Of(string.Empty, XlsxStyle.Subtotal));
        }

        builder.AddRow(
            XlsxCell.Spanning("ИТОГО ПО ОТЧЁТУ", 7, XlsxStyle.Section),
            XlsxCell.Of(report.PlannedTotal, XlsxStyle.Section),
            XlsxCell.Of(report.ActualTotal, XlsxStyle.Section),
            XlsxCell.Of(report.CostDeviation, XlsxStyle.Section),
            XlsxCell.Of(report.DeviationPercent, XlsxStyle.Section),
            XlsxCell.Of(report.Severity.ToDisplayName(), XlsxStyle.Section));

        builder.AddEmptyRow();
        builder.AddRow(XlsxCell.Spanning(
            "Отклонение = факт − план; процент = отклонение ÷ план × 100 %; " +
            "порог внимания — 5 %, критично — 10 % (по модулю процента).",
            12, XlsxStyle.Text));

        return builder.Build();
    }

    /// <summary>CSV того же состава: открывается в Excel, разделитель «;», UTF-8 с BOM.</summary>
    public string BuildCsv(DeviationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var sb = new StringBuilder();
        sb.AppendLine("Анализ отклонений фактического расхода от сметы");
        sb.AppendLine($"Область;{Esc(report.ScopeName)}");
        sb.AppendLine($"Сформировано;{DateTime.Now:dd.MM.yyyy HH:mm};позиций;{report.Rows.Count}");
        sb.AppendLine();

        sb.AppendLine($"Сметная стоимость;{Num(report.PlannedTotal)};Фактические затраты;{Num(report.ActualTotal)}");
        sb.AppendLine($"Отклонение;{Num(report.CostDeviation)};Отклонение, %;{Num(report.DeviationPercent)}");
        sb.AppendLine($"Сводная оценка;{report.Severity.ToDisplayName()}");
        sb.AppendLine();

        sb.AppendLine("№;Материал;ед.;кол-во план;кол-во факт;цена план;цена факт;стоимость план;стоимость факт;отклонение;откл., %;оценка");

        var number = 1;
        var sectionNumber = 1;

        foreach (var section in GroupSections(report))
        {
            sb.AppendLine($"{sectionNumber++}. {Esc(section.Name)}");

            foreach (var row in section.Rows)
            {
                sb.AppendLine(string.Join(';',
                    number++.ToString(Russian),
                    Esc(row.MaterialName),
                    Esc(row.Unit),
                    Num(row.PlannedQuantity),
                    Num(row.ActualQuantity),
                    Num(row.PlannedPrice),
                    Num(row.ActualPrice),
                    Num(row.PlannedCost),
                    Num(row.ActualCost),
                    Num(row.CostDeviation),
                    Num(row.DeviationPercent),
                    Esc(row.SeverityDisplay)));
            }

            sb.AppendLine($";Итого по разделу «{Esc(section.Name)}»;;;;;;{Num(section.Planned)};{Num(section.Actual)};{Num(section.Deviation)};{Num(section.Percent)};");
        }

        sb.AppendLine($";ИТОГО ПО ОТЧЁТУ;;;;;;{Num(report.PlannedTotal)};{Num(report.ActualTotal)};{Num(report.CostDeviation)};{Num(report.DeviationPercent)};{Esc(report.Severity.ToDisplayName())}");
        sb.AppendLine();
        sb.AppendLine("Отклонение = факт − план; процент = отклонение ÷ план × 100 %; порог внимания — 5 %, критично — 10 %.");

        return sb.ToString();
    }

    public async Task ExportCsvAsync(DeviationReport report, string filePath, CancellationToken cancellationToken = default)
    {
        var csv = BuildCsv(report);
        await File.WriteAllTextAsync(filePath, csv, new UTF8Encoding(true), cancellationToken);
    }

    // ------------------------------------------------------------------

    private sealed record SectionBlock(
        string Name,
        IReadOnlyList<DeviationRow> Rows,
        decimal Planned,
        decimal Actual)
    {
        public decimal Deviation => Actual - Planned;

        public decimal Percent => Planned == 0m ? 0m : Math.Round(Deviation / Planned * 100m, 1);
    }

    private static IReadOnlyList<SectionBlock> GroupSections(DeviationReport report) =>
        report.Rows
            .GroupBy(r => string.IsNullOrWhiteSpace(r.Section) ? "Без раздела" : r.Section)
            .OrderBy(g => g.Key, StringComparer.CurrentCulture)
            .Select(g => new SectionBlock(
                g.Key,
                g.OrderBy(r => r.MaterialName, StringComparer.CurrentCulture).ToList(),
                g.Sum(r => r.PlannedCost),
                g.Sum(r => r.ActualCost)))
            .ToList();

    private static string Num(decimal value) => value.ToString("0.00", Russian);

    private static string Esc(string? value) => (value ?? string.Empty)
        .Replace(';', ',')
        .Replace('\n', ' ')
        .Replace('\r', ' ');

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "отчёт";

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(c => invalid.Contains(c) || c == ' ' ? '_' : c).ToArray());
        return cleaned.Trim('_');
    }
}
