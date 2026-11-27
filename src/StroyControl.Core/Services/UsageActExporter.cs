using System.Globalization;
using System.Text;
using StroyControl.Core.Entities;

namespace StroyControl.Core.Services;

/// <summary>
/// Акт списания материалов в табличных форматах: Excel (.xlsx) и CSV. Состав тот же,
/// что в печатной форме: шапка акта, объект, основание, строка списания, итог и
/// подписи. Нужен, чтобы акт можно было передать бухгалтерии файлом, а не только
/// распечатать.
/// </summary>
public sealed class UsageActExporter
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    public string DefaultFileName(MaterialUsage usage, string extension = "xlsx") =>
        $"Акт_списания_{Sanitize(usage.DocumentNumber)}_{usage.Date:yyyyMMdd}.{extension}";

    public byte[] BuildXlsx(MaterialUsage usage, string organization)
    {
        ArgumentNullException.ThrowIfNull(usage);

        var builder = new XlsxBuilder("Акт списания", new double[] { 5, 46, 10, 12, 14, 16 }, frozenRows: 10);

        builder.AddRow(XlsxCell.Of("АКТ СПИСАНИЯ МАТЕРИАЛОВ", XlsxStyle.Title));
        builder.AddRow(XlsxCell.Of($"№ {usage.DocumentNumber} от {usage.DateDisplay}", XlsxStyle.Bold));
        builder.AddRow(XlsxCell.Of($"Организация: {organization}"));
        builder.AddRow(XlsxCell.Of($"Объект: {usage.ProjectName}", XlsxStyle.Bold));
        builder.AddRow(XlsxCell.Of($"Основание: {BuildBasis(usage)}"));
        builder.AddRow(XlsxCell.Of($"Материально ответственное лицо: {usage.AuthorName}"));
        builder.AddRow(XlsxCell.Of($"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm}"));
        builder.AddEmptyRow();

        builder.AddRow(
            XlsxCell.Of("№", XlsxStyle.Header),
            XlsxCell.Of("Наименование материала", XlsxStyle.Header),
            XlsxCell.Of("ед.", XlsxStyle.Header),
            XlsxCell.Of("кол-во", XlsxStyle.Header),
            XlsxCell.Of("цена", XlsxStyle.Header),
            XlsxCell.Of("сумма", XlsxStyle.Header));

        builder.AddRow(
            XlsxCell.Of("1", XlsxStyle.Center),
            XlsxCell.Of(usage.MaterialName),
            XlsxCell.Of(usage.UnitDisplay, XlsxStyle.Center),
            XlsxCell.Of(usage.Quantity),
            XlsxCell.Of(usage.UnitPrice),
            XlsxCell.Of(usage.Cost));

        builder.AddRow(
            XlsxCell.Spanning("ИТОГО", 5, XlsxStyle.Section),
            XlsxCell.Of(usage.Cost, XlsxStyle.Section));

        builder.AddEmptyRow();
        builder.AddRow(XlsxCell.Of($"Списано на сумму {usage.Cost.ToString("N2", Russian)} BYN", XlsxStyle.Bold));
        builder.AddRow(XlsxCell.Of("Списал (прораб): ______________________   Проверил (инженер ПТО): ______________________"));

        return builder.Build();
    }

    public string BuildCsv(MaterialUsage usage, string organization)
    {
        ArgumentNullException.ThrowIfNull(usage);

        var sb = new StringBuilder();
        sb.AppendLine("Акт списания материалов");
        sb.AppendLine($"Документ;{Esc(usage.DocumentNumber)};{usage.DateDisplay}");
        sb.AppendLine($"Организация;{Esc(organization)}");
        sb.AppendLine($"Объект;{Esc(usage.ProjectName)}");
        sb.AppendLine($"Основание;{Esc(BuildBasis(usage))}");
        sb.AppendLine($"Материально ответственное лицо;{Esc(usage.AuthorName)}");
        sb.AppendLine();

        sb.AppendLine("№;Наименование;ед.;кол-во;цена;сумма");
        sb.AppendLine(string.Join(';',
            "1",
            Esc(usage.MaterialName),
            Esc(usage.UnitDisplay),
            Num(usage.Quantity),
            Num(usage.UnitPrice),
            Num(usage.Cost)));

        sb.AppendLine($";ИТОГО;;;;{Num(usage.Cost)}");
        sb.AppendLine();
        sb.AppendLine($"Примечание;{Esc(usage.Note)}");
        return sb.ToString();
    }

    public async Task ExportXlsxAsync(MaterialUsage usage, string organization, string filePath,
        CancellationToken cancellationToken = default) =>
        await File.WriteAllBytesAsync(filePath, BuildXlsx(usage, organization), cancellationToken);

    public async Task ExportCsvAsync(MaterialUsage usage, string organization, string filePath,
        CancellationToken cancellationToken = default) =>
        await File.WriteAllTextAsync(filePath, BuildCsv(usage, organization), new UTF8Encoding(true), cancellationToken);

    private static string BuildBasis(MaterialUsage usage) =>
        usage.Estimate is { } estimate
            ? $"смета № {estimate.Number} от {estimate.Date:dd.MM.yyyy} — {estimate.Name}"
            : "производственная необходимость (без привязки к смете)";

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
