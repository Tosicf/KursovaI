using System.Globalization;
using System.Text;
using StroyControl.Core.Entities;

namespace StroyControl.Core.Services;

/// <summary>
/// Акт выполненных работ в табличных форматах: Excel (.xlsx) и CSV. Состав повторяет
/// печатную форму: шапка, основание (смета), таблица работ, таблица списанных
/// материалов, итоги и подписи. Файл нужен бухгалтерии и ПТО для сверки факта.
/// </summary>
public sealed class WorkCompletionExporter
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    public string DefaultFileName(WorkCompletion completion, string extension = "xlsx") =>
        $"Акт_выполненных_работ_{Sanitize(completion.DocumentNumber)}_{completion.Date:yyyyMMdd}.{extension}";

    public byte[] BuildXlsx(WorkCompletion completion, string organization)
    {
        ArgumentNullException.ThrowIfNull(completion);

        var builder = new XlsxBuilder("Акт выполненных работ", new double[] { 5, 52, 10, 12, 14, 16 }, frozenRows: 0);

        builder.AddRow(XlsxCell.Of("АКТ ВЫПОЛНЕННЫХ РАБОТ", XlsxStyle.Title));
        builder.AddRow(XlsxCell.Of($"№ {completion.DocumentNumber} от {completion.DateDisplay}", XlsxStyle.Bold));
        builder.AddRow(XlsxCell.Of($"Организация: {organization}"));
        builder.AddRow(XlsxCell.Of($"Объект: {completion.ProjectName}", XlsxStyle.Bold));

        if (completion.Estimate is { } estimate)
            builder.AddRow(XlsxCell.Of($"Основание: смета № {estimate.Number} от {estimate.Date:dd.MM.yyyy} — {estimate.Name}"));

        builder.AddRow(XlsxCell.Of($"Ответственный: {completion.AuthorName}"));
        builder.AddRow(XlsxCell.Of($"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm}"));
        builder.AddEmptyRow();

        builder.AddRow(XlsxCell.Of("1. Выполненные работы", XlsxStyle.Bold));
        AddHeader(builder);

        var index = 1;
        foreach (var item in completion.Items.OrderBy(i => i.SectionDisplay).ThenBy(i => i.WorkName))
        {
            builder.AddRow(
                XlsxCell.Of(index.ToString(Russian), XlsxStyle.Center),
                XlsxCell.Of(item.WorkName),
                XlsxCell.Of(item.UnitDisplay, XlsxStyle.Center),
                XlsxCell.Of(item.Quantity),
                XlsxCell.Of(item.UnitPrice),
                XlsxCell.Of(item.Cost));

            index++;
        }

        if (index == 1)
            builder.AddRow(XlsxCell.Of("Работы в акте не указаны", XlsxStyle.Text), XlsxCell.Of(string.Empty));

        builder.AddRow(
            XlsxCell.Spanning("ИТОГО по работам", 5, XlsxStyle.Section),
            XlsxCell.Of(completion.WorksCost, XlsxStyle.Section));

        builder.AddEmptyRow();

        builder.AddRow(XlsxCell.Of("2. Израсходованные материалы (списаны со склада)", XlsxStyle.Bold));
        AddHeader(builder);

        index = 1;
        foreach (var usage in completion.MaterialUsages.OrderBy(u => u.MaterialName))
        {
            builder.AddRow(
                XlsxCell.Of(index.ToString(Russian), XlsxStyle.Center),
                XlsxCell.Of(usage.MaterialName),
                XlsxCell.Of(usage.UnitDisplay, XlsxStyle.Center),
                XlsxCell.Of(usage.Quantity),
                XlsxCell.Of(usage.UnitPrice),
                XlsxCell.Of(usage.Cost));

            index++;
        }

        if (index == 1)
            builder.AddRow(XlsxCell.Of("Материалы в акте не списывались", XlsxStyle.Text), XlsxCell.Of(string.Empty));

        builder.AddRow(
            XlsxCell.Spanning("ИТОГО по материалам", 5, XlsxStyle.Section),
            XlsxCell.Of(completion.MaterialsCost, XlsxStyle.Section));

        builder.AddEmptyRow();

        builder.AddRow(XlsxCell.Spanning("Работы, BYN", 5, XlsxStyle.Bold), XlsxCell.Of(completion.WorksCost, XlsxStyle.Bold));
        builder.AddRow(XlsxCell.Spanning("Материалы, BYN", 5, XlsxStyle.Bold), XlsxCell.Of(completion.MaterialsCost, XlsxStyle.Bold));
        builder.AddRow(XlsxCell.Spanning("ВСЕГО ПО АКТУ, BYN", 5, XlsxStyle.Section), XlsxCell.Of(completion.Total, XlsxStyle.Section));

        builder.AddEmptyRow();
        builder.AddRow(XlsxCell.Of("Сдал (прораб): ______________________   Принял (инженер ПТО): ______________________"));

        return builder.Build();
    }

    public string BuildCsv(WorkCompletion completion, string organization)
    {
        ArgumentNullException.ThrowIfNull(completion);

        var sb = new StringBuilder();
        sb.AppendLine("Акт выполненных работ");
        sb.AppendLine($"Документ;{Esc(completion.DocumentNumber)};{completion.DateDisplay}");
        sb.AppendLine($"Организация;{Esc(organization)}");
        sb.AppendLine($"Объект;{Esc(completion.ProjectName)}");

        if (completion.Estimate is { } estimate)
            sb.AppendLine($"Основание;{Esc($"смета № {estimate.Number} от {estimate.Date:dd.MM.yyyy} — {estimate.Name}")}");

        sb.AppendLine($"Ответственный;{Esc(completion.AuthorName)}");
        sb.AppendLine();

        sb.AppendLine("Выполненные работы");
        sb.AppendLine("№;Наименование;ед.;кол-во;цена;сумма");

        var index = 1;
        foreach (var item in completion.Items.OrderBy(i => i.SectionDisplay).ThenBy(i => i.WorkName))
        {
            sb.AppendLine(string.Join(';',
                index.ToString(Russian),
                Esc(item.WorkName),
                Esc(item.UnitDisplay),
                Num(item.Quantity),
                Num(item.UnitPrice),
                Num(item.Cost)));

            index++;
        }

        sb.AppendLine($";ИТОГО по работам;;;;{Num(completion.WorksCost)}");
        sb.AppendLine();

        sb.AppendLine("Израсходованные материалы");
        sb.AppendLine("№;Наименование;ед.;кол-во;цена;сумма");

        index = 1;
        foreach (var usage in completion.MaterialUsages.OrderBy(u => u.MaterialName))
        {
            sb.AppendLine(string.Join(';',
                index.ToString(Russian),
                Esc(usage.MaterialName),
                Esc(usage.UnitDisplay),
                Num(usage.Quantity),
                Num(usage.UnitPrice),
                Num(usage.Cost)));

            index++;
        }

        sb.AppendLine($";ИТОГО по материалам;;;;{Num(completion.MaterialsCost)}");
        sb.AppendLine();
        sb.AppendLine($";ВСЕГО ПО АКТУ;;;;{Num(completion.Total)}");
        sb.AppendLine();
        sb.AppendLine($"Примечание;{Esc(completion.Note)}");
        return sb.ToString();
    }

    public async Task ExportXlsxAsync(WorkCompletion completion, string organization, string filePath,
        CancellationToken cancellationToken = default) =>
        await File.WriteAllBytesAsync(filePath, BuildXlsx(completion, organization), cancellationToken);

    public async Task ExportCsvAsync(WorkCompletion completion, string organization, string filePath,
        CancellationToken cancellationToken = default) =>
        await File.WriteAllTextAsync(filePath, BuildCsv(completion, organization), new UTF8Encoding(true), cancellationToken);

    private static void AddHeader(XlsxBuilder builder) =>
        builder.AddRow(
            XlsxCell.Of("№", XlsxStyle.Header),
            XlsxCell.Of("Наименование", XlsxStyle.Header),
            XlsxCell.Of("ед.", XlsxStyle.Header),
            XlsxCell.Of("кол-во", XlsxStyle.Header),
            XlsxCell.Of("цена", XlsxStyle.Header),
            XlsxCell.Of("сумма", XlsxStyle.Header));

    private static string Num(decimal value) => value.ToString("0.00", Russian);

    private static string Esc(string? value) => (value ?? string.Empty)
        .Replace(';', ',')
        .Replace('\n', ' ')
        .Replace('\r', ' ');

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "без_номера";

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(c => invalid.Contains(c) || c == ' ' ? '_' : c).ToArray());
        return cleaned.Trim('_');
    }
}
