using System.Globalization;
using System.Text;
using StroyControl.Core.Entities;

namespace StroyControl.Core.Services;

/// <summary>
/// Счёт-фактура в табличных форматах: Excel (.xlsx) и CSV. Состав тот же, что в
/// печатной форме: реквизиты, основание, позиции, итог. Нужен, чтобы документ можно было
/// сохранить не только в PDF и отправить поставщику или бухгалтерии.
/// </summary>
public sealed class ReceiptExporter
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    public string DefaultFileName(Delivery delivery, string extension = "xlsx") =>
        $"Чек_{Sanitize(delivery.Number)}_{delivery.Date:yyyyMMdd}.{extension}";

    public byte[] BuildXlsx(Delivery delivery, string organization)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        var builder = new XlsxBuilder("Чек", new double[] { 5, 46, 10, 12, 14, 16 }, frozenRows: 10);

        builder.AddRow(XlsxCell.Of("СЧЁТ-ФАКТУРА", XlsxStyle.Title));
        builder.AddRow(XlsxCell.Of($"Продавец: {organization}", XlsxStyle.Bold));
        builder.AddRow(XlsxCell.Of($"Покупатель: {delivery.ProjectName}", XlsxStyle.Bold));
        builder.AddRow(XlsxCell.Of($"Документ: {delivery.Number} от {delivery.DateDisplay}"));
        builder.AddRow(XlsxCell.Of($"Поставщик: {delivery.SupplierName}"));
        builder.AddRow(XlsxCell.Of($"Основание: накладная {delivery.InvoiceNumber}"));
        builder.AddRow(XlsxCell.Of($"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm}"));
        builder.AddEmptyRow();

        builder.AddRow(
            XlsxCell.Of("№", XlsxStyle.Header),
            XlsxCell.Of("Наименование", XlsxStyle.Header),
            XlsxCell.Of("ед.", XlsxStyle.Header),
            XlsxCell.Of("кол-во", XlsxStyle.Header),
            XlsxCell.Of("цена", XlsxStyle.Header),
            XlsxCell.Of("сумма", XlsxStyle.Header));

        var number = 1;
        foreach (var item in delivery.Items ?? Array.Empty<DeliveryItem>())
        {
            builder.AddRow(
                XlsxCell.Of(number++.ToString(Russian), XlsxStyle.Center),
                XlsxCell.Of(item.MaterialName),
                XlsxCell.Of(item.UnitDisplay, XlsxStyle.Center),
                XlsxCell.Of(item.Quantity),
                XlsxCell.Of(item.UnitPrice),
                XlsxCell.Of(item.Cost));
        }

        builder.AddRow(
            XlsxCell.Spanning("ИТОГО", 5, XlsxStyle.Section),
            XlsxCell.Of(delivery.Total, XlsxStyle.Section));

        builder.AddEmptyRow();
        builder.AddRow(XlsxCell.Of($"Всего наименований: {delivery.ItemsCount}", XlsxStyle.Bold));
        builder.AddRow(XlsxCell.Of("Отпустил: ______________________   Получил: ______________________"));

        return builder.Build();
    }

    public string BuildCsv(Delivery delivery, string organization)
    {
        ArgumentNullException.ThrowIfNull(delivery);

        var sb = new StringBuilder();
        sb.AppendLine("Счёт-фактура");
        sb.AppendLine($"Продавец;{Esc(organization)}");
        sb.AppendLine($"Покупатель;{Esc(delivery.ProjectName)}");
        sb.AppendLine($"Документ;{Esc(delivery.Number)};{delivery.DateDisplay}");
        sb.AppendLine($"Поставщик;{Esc(delivery.SupplierName)}");
        sb.AppendLine($"Основание;{Esc(delivery.InvoiceNumber)}");
        sb.AppendLine();

        sb.AppendLine("№;Наименование;ед.;кол-во;цена;сумма");

        var number = 1;
        foreach (var item in delivery.Items ?? Array.Empty<DeliveryItem>())
        {
            sb.AppendLine(string.Join(';',
                number++.ToString(Russian),
                Esc(item.MaterialName),
                Esc(item.UnitDisplay),
                Num(item.Quantity),
                Num(item.UnitPrice),
                Num(item.Cost)));
        }

        sb.AppendLine($";ИТОГО;;;;{Num(delivery.Total)}");
        sb.AppendLine();
        sb.AppendLine($"Всего наименований;{delivery.ItemsCount}");
        return sb.ToString();
    }

    public async Task ExportXlsxAsync(Delivery delivery, string organization, string filePath,
        CancellationToken cancellationToken = default) =>
        await File.WriteAllBytesAsync(filePath, BuildXlsx(delivery, organization), cancellationToken);

    public async Task ExportCsvAsync(Delivery delivery, string organization, string filePath,
        CancellationToken cancellationToken = default) =>
        await File.WriteAllTextAsync(filePath, BuildCsv(delivery, organization), new UTF8Encoding(true), cancellationToken);

    private static string Num(decimal value) => value.ToString("0.00", Russian);

    private static string Esc(string? value) => (value ?? string.Empty)
        .Replace(';', ',')
        .Replace('\n', ' ')
        .Replace('\r', ' ');

    private static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "чек";

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(c => invalid.Contains(c) || c == ' ' ? '_' : c).ToArray());
        return cleaned.Trim('_');
    }
}
