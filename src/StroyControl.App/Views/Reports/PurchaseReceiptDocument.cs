using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using StroyControl.Core.Entities;

namespace StroyControl.App.Views.Reports;

/// <summary>
/// Собирает печатный «Товарный чек» по поступлению материалов (FlowDocument, A5):
/// реквизиты продавца и покупателя, таблица позиций, итог и подписи.
/// Тот же документ показывается в предпросмотре и уходит на печать.
/// </summary>
public static class PurchaseReceiptDocument
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");

    private static readonly Brush HeaderFill = Create(0xF2, 0xEC, 0xE6);
    private static readonly Brush LineBrush = Create(0x93, 0xA1, 0xB5);
    private static readonly Brush Ink = Create(0x1F, 0x2A, 0x37);
    private static readonly Brush Muted = Create(0x52, 0x60, 0x6D);

    public static FlowDocument Build(Delivery delivery, string organization)
    {
        HeaderFill.Freeze();
        LineBrush.Freeze();
        Ink.Freeze();
        Muted.Freeze();

        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Times New Roman"),
            FontSize = 11,
            Foreground = Brushes.Black,
            Background = Brushes.White,
            PagePadding = new Thickness(28),
            PageWidth = 559,   // A5 при 96 dpi
            PageHeight = 794,
            ColumnWidth = double.PositiveInfinity
        };

        doc.Blocks.Add(new Paragraph(new Run("ТОВАРНЫЙ ЧЕК"))
        {
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 2)
        });

        doc.Blocks.Add(new Paragraph(new Run($"№ {delivery.Number} от {delivery.Date:dd.MM.yyyy}"))
        {
            FontSize = 11.5,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        });

        doc.Blocks.Add(BuildParty("Продавец", BuildSupplier(delivery)));
        doc.Blocks.Add(BuildParty("Покупатель", BuildBuyer(delivery, organization)));

        if (!string.IsNullOrWhiteSpace(delivery.InvoiceNumber))
        {
            doc.Blocks.Add(BuildParty("Основание",
                $"приходная накладная № {delivery.Number}, ТТН № {delivery.InvoiceNumber.Trim()}"));
        }

        doc.Blocks.Add(BuildTable(delivery));

        doc.Blocks.Add(new Paragraph(new Run(
                $"Всего наименований: {delivery.ItemsCount}, на сумму {Money(delivery.Total)} BYN"))
        {
            FontSize = 10.5,
            Margin = new Thickness(0, 0, 0, 2)
        });

        doc.Blocks.Add(new Paragraph(new Run(
                $"Сумма к оплате: {Money(delivery.Total)} BYN (НДС не облагается)"))
        {
            FontSize = 11.5,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        });

        if (!string.IsNullOrWhiteSpace(delivery.Note))
        {
            doc.Blocks.Add(new Paragraph(new Run($"Примечание: {delivery.Note.Trim()}"))
            {
                FontSize = 10,
                Foreground = Muted,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        doc.Blocks.Add(BuildSignatures());
        doc.Blocks.Add(new Paragraph(new Run(
                $"Документ сформирован {DateTime.Now:dd.MM.yyyy HH:mm} в системе «СтройКонтроль»."))
        {
            FontSize = 9,
            Foreground = Muted,
            Margin = new Thickness(0, 10, 0, 0)
        });

        return doc;
    }

    // ------------------------------------------------------------------

    private static string BuildSupplier(Delivery delivery)
    {
        var supplier = delivery.Supplier;
        if (supplier is null) return "—";

        var parts = new List<string> { supplier.Name };

        if (!string.IsNullOrWhiteSpace(supplier.Unp))
            parts.Add($"УНП {supplier.Unp.Trim()}");

        if (!string.IsNullOrWhiteSpace(supplier.Address))
            parts.Add(supplier.Address.Trim());

        if (!string.IsNullOrWhiteSpace(supplier.Phone))
            parts.Add($"тел. {supplier.Phone.Trim()}");

        return string.Join(", ", parts);
    }

    private static string BuildBuyer(Delivery delivery, string organization)
    {
        var buyer = string.IsNullOrWhiteSpace(organization) ? "СтройКонтроль" : organization.Trim();

        return delivery.ProjectId is null
            ? $"{buyer}, общий склад"
            : $"{buyer}, объект «{delivery.ProjectName}»";
    }

    private static Paragraph BuildParty(string label, string value)
    {
        var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 3), FontSize = 10.5 };
        paragraph.Inlines.Add(new Run($"{label}: ") { FontWeight = FontWeights.Bold, Foreground = Ink });
        paragraph.Inlines.Add(new Run(value) { Foreground = Ink });
        return paragraph;
    }

    private static Table BuildTable(Delivery delivery)
    {
        var table = CreateTable(new[] { 24d, 200d, 40d, 66d, 72d, 79d });

        AddRow(table,
            Cell("№", TextAlignment.Center, header: true),
            Cell("Наименование", TextAlignment.Left, header: true),
            Cell("ед.", TextAlignment.Center, header: true),
            Cell("кол-во", TextAlignment.Center, header: true),
            Cell("цена, BYN", TextAlignment.Center, header: true),
            Cell("сумма, BYN", TextAlignment.Center, header: true));

        var number = 1;
        foreach (var item in delivery.Items.OrderBy(i => i.Id))
        {
            AddRow(table,
                Cell(number++.ToString(Russian), TextAlignment.Center),
                Cell(item.MaterialName),
                Cell(item.UnitDisplay, TextAlignment.Center),
                Cell(Number(item.Quantity), TextAlignment.Right),
                Cell(Money(item.UnitPrice), TextAlignment.Right),
                Cell(Money(item.Cost), TextAlignment.Right));
        }

        AddRow(table,
            Cell("ИТОГО", bold: true, fill: HeaderFill, colSpan: 5),
            Cell(Money(delivery.Total), TextAlignment.Right, bold: true, fill: HeaderFill));

        return table;
    }

    private static Table BuildSignatures()
    {
        var table = CreateTable(new[] { 230d, 239d }, borders: false);

        AddRow(table,
            Cell("Отпустил (продавец) ______________", header: true),
            Cell("Получил (покупатель) ______________", header: true));

        return table;
    }

    // ------------------------------------------------------------------

    private static Table CreateTable(IReadOnlyList<double> widths, bool borders = true)
    {
        var table = new Table
        {
            CellSpacing = 0,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(borders ? 0.6 : 0),
            Margin = new Thickness(0, 6, 0, 8)
        };

        foreach (var width in widths)
            table.Columns.Add(new TableColumn { Width = new GridLength(width) });

        table.RowGroups.Add(new TableRowGroup());
        return table;
    }

    private static void AddRow(Table table, params TableCell[] cells)
    {
        var row = new TableRow();
        foreach (var cell in cells)
            row.Cells.Add(cell);

        table.RowGroups[0].Rows.Add(row);
    }

    private static TableCell Cell(
        string text,
        TextAlignment alignment = TextAlignment.Left,
        bool bold = false,
        bool header = false,
        Brush? fill = null,
        int colSpan = 1)
    {
        var paragraph = new Paragraph(new Run(text))
        {
            TextAlignment = alignment,
            Margin = new Thickness(1),
            FontSize = header ? 9.5 : 10.5,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = Ink,
            LineHeight = 12
        };

        return new TableCell(paragraph)
        {
            Background = fill ?? Brushes.Transparent,
            BorderBrush = LineBrush,
            BorderThickness = new Thickness(0.5),
            Padding = new Thickness(3, 2, 3, 2),
            ColumnSpan = colSpan
        };
    }

    private static string Money(decimal value) => value.ToString("N2", Russian);

    private static string Number(decimal value) => value.ToString("N3", Russian);

    private static Brush Create(byte r, byte g, byte b) => new SolidColorBrush(Color.FromRgb(r, g, b));
}
