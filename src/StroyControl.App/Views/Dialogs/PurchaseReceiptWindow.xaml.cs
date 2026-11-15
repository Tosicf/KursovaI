using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Win32;
using StroyControl.App.Services;
using StroyControl.App.Views.Reports;
using StroyControl.Core.Entities;
using StroyControl.Core.Services;

namespace StroyControl.App.Views.Dialogs;

/// <summary>
/// Предпросмотр товарного чека по поступлению: тот же FlowDocument печатается
/// на принтере и сохраняется в PDF, Excel и CSV.
/// </summary>
public partial class PurchaseReceiptWindow : Window
{
    private readonly Delivery _delivery;
    private readonly string _organization;
    private readonly ReceiptExporter _exporter = new();

    public PurchaseReceiptWindow(Delivery delivery, string organization)
    {
        InitializeComponent();

        _delivery = delivery;
        _organization = organization;

        DataContext = this;
        Viewer.Document = PurchaseReceiptDocument.Build(_delivery, _organization);
    }

    /// <summary>Подпись под заголовком окна (используется в привязке).</summary>
    public string Caption =>
        $"{_delivery.Number} от {_delivery.DateDisplay} · {_delivery.SupplierName} · {_delivery.Total:N2} BYN";

    private void OnExportClick(object sender, RoutedEventArgs e)
    {
        if (ExportButton.ContextMenu is not { } menu) return;

        menu.PlacementTarget = ExportButton;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void OnPdfClick(object sender, RoutedEventArgs e)
    {
        var path = AskPath("Сохранить чек в PDF", "pdf", "Файл PDF (*.pdf)|*.pdf|Все файлы (*.*)|*.*");
        if (path is null) return;

        var document = PurchaseReceiptDocument.Build(_delivery, _organization);

        if (PdfExporter.TryExport(document, path, out var error))
        {
            MessageBox.Show(this, $"PDF сохранён:\n{path}", "PDF", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MessageBox.Show(this, error ?? "Не удалось выгрузить чек в PDF.", "PDF",
            MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private async void OnExcelClick(object sender, RoutedEventArgs e)
    {
        var path = AskPath("Сохранить чек в Excel", "xlsx",
            "Книга Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*");
        if (path is null) return;

        try
        {
            await _exporter.ExportXlsxAsync(_delivery, _organization, path);
            MessageBox.Show(this, $"Чек сохранён:\n{path}", "Excel", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось сохранить файл.\n\n{ex.Message}", "Excel",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnCsvClick(object sender, RoutedEventArgs e)
    {
        var path = AskPath("Сохранить чек в CSV", "csv", "Файлы CSV (*.csv)|*.csv|Все файлы (*.*)|*.*");
        if (path is null) return;

        try
        {
            await _exporter.ExportCsvAsync(_delivery, _organization, path);
            MessageBox.Show(this, $"Чек сохранён:\n{path}", "CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось сохранить файл.\n\n{ex.Message}", "CSV",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string? AskPath(string title, string extension, string filter)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            FileName = _exporter.DefaultFileName(_delivery, extension),
            Filter = filter,
            AddExtension = true,
            OverwritePrompt = true
        };

        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }

    private void OnPrintClick(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;

        try
        {
            var document = PurchaseReceiptDocument.Build(_delivery, _organization);
            document.PageWidth = dialog.PrintableAreaWidth;
            document.PageHeight = dialog.PrintableAreaHeight;
            document.PagePadding = new Thickness(36);
            document.ColumnWidth = dialog.PrintableAreaWidth;

            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
            paginator.PageSize = new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);

            dialog.PrintDocument(paginator, $"Товарный чек {_delivery.Number}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось напечатать чек.\n\n{ex.Message}", "Печать",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
