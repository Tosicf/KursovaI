using System.IO;
using System.Windows;
using System.Windows.Documents;
using Microsoft.Win32;
using StroyControl.App.Services;
using StroyControl.App.Views.Reports;
using StroyControl.Core.Entities;

namespace StroyControl.App.Views.Dialogs;

/// <summary>
/// Предпросмотр акта списания материалов: тот же FlowDocument печатается
/// на принтере и сохраняется в PDF. Окно открывается сразу после оформления
/// списания и из списка списаний — до этого момента акт существует только
/// как запись в базе, а печатная форма формируется здесь.
/// </summary>
public partial class UsageActWindow : Window
{
    private readonly MaterialUsage _usage;
    private readonly string _organization;

    public UsageActWindow(MaterialUsage usage, string organization)
    {
        InitializeComponent();

        _usage = usage;
        _organization = organization;

        DataContext = this;
        Viewer.Document = UsageActDocument.Build(_usage, _organization);
    }

    /// <summary>Подпись под заголовком окна (используется в привязке).</summary>
    public string Caption =>
        $"{(string.IsNullOrWhiteSpace(_usage.DocumentNumber) ? "без номера" : _usage.DocumentNumber)} от {_usage.DateDisplay} · " +
        $"{_usage.ProjectName} · {_usage.MaterialName} · {_usage.Cost:N2} BYN";

    private void OnExportClick(object sender, RoutedEventArgs e)
    {
        if (ExportButton.ContextMenu is not { } menu) return;

        menu.PlacementTarget = ExportButton;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void OnPdfClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить акт списания в PDF",
            FileName = DefaultFileName("pdf"),
            Filter = "Файл PDF (*.pdf)|*.pdf|Все файлы (*.*)|*.*",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != true) return;

        var document = UsageActDocument.Build(_usage, _organization);
        if (PdfExporter.TryExport(document, dialog.FileName, out var error))
        {
            MessageBox.Show(this, $"PDF сохранён:\n{dialog.FileName}", "PDF", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MessageBox.Show(this, error ?? "Не удалось выгрузить акт в PDF.", "PDF",
            MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void OnPrintClick(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;

        try
        {
            var document = UsageActDocument.Build(_usage, _organization);
            document.PageWidth = dialog.PrintableAreaWidth;
            document.PageHeight = dialog.PrintableAreaHeight;
            document.PagePadding = new Thickness(36);
            document.ColumnWidth = dialog.PrintableAreaWidth;

            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
            paginator.PageSize = new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);

            dialog.PrintDocument(paginator, $"Акт списания {_usage.DocumentNumber}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось напечатать акт.\n\n{ex.Message}", "Печать",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string DefaultFileName(string extension)
    {
        var number = string.IsNullOrWhiteSpace(_usage.DocumentNumber) ? "бн" : _usage.DocumentNumber.Trim();
        var name = $"Акт списания {number} от {_usage.Date:yyyy-MM-dd}";

        foreach (var invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');

        return $"{name}.{extension}";
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
