using System.IO;
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
/// Предпросмотр акта выполненных работ: тот же FlowDocument печатается на принтере
/// и сохраняется в PDF, а данные акта выгружаются в Excel и CSV. Окно открывается
/// сразу после сохранения акта и из журнала актов работ.
/// </summary>
public partial class WorkCompletionActWindow : Window
{
    private readonly WorkCompletion _completion;
    private readonly string _organization;
    private readonly WorkCompletionExporter _exporter = new();

    public WorkCompletionActWindow(WorkCompletion completion, string organization)
    {
        InitializeComponent();

        _completion = completion;
        _organization = organization;

        DataContext = this;
        Viewer.Document = WorkCompletionDocument.Build(_completion, _organization);
    }

    /// <summary>Подпись под заголовком окна.</summary>
    public string Caption =>
        $"{(string.IsNullOrWhiteSpace(_completion.DocumentNumber) ? "без номера" : _completion.DocumentNumber)} от {_completion.DateDisplay} · " +
        $"{_completion.ProjectName} · {_completion.EstimateNumber} · {_completion.Total:N2} BYN";

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
            Title = "Сохранить акт выполненных работ в PDF",
            FileName = DefaultFileName("pdf"),
            Filter = "Файл PDF (*.pdf)|*.pdf|Все файлы (*.*)|*.*",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != true) return;

        var document = WorkCompletionDocument.Build(_completion, _organization);
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
            var document = WorkCompletionDocument.Build(_completion, _organization);
            document.PageWidth = dialog.PrintableAreaWidth;
            document.PageHeight = dialog.PrintableAreaHeight;
            document.PagePadding = new Thickness(36);
            document.ColumnWidth = dialog.PrintableAreaWidth;

            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
            paginator.PageSize = new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);

            dialog.PrintDocument(paginator, $"Акт выполненных работ {_completion.DocumentNumber}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось напечатать акт.\n\n{ex.Message}", "Печать",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnExcelClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить акт выполненных работ в Excel",
            FileName = _exporter.DefaultFileName(_completion, "xlsx"),
            Filter = "Книга Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != true) return;

        try
        {
            await _exporter.ExportXlsxAsync(_completion, _organization, dialog.FileName);
            MessageBox.Show(this, $"Акт сохранён:\n{dialog.FileName}", "Excel", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось сохранить файл.\n\n{ex.Message}", "Excel",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnCsvClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить акт выполненных работ в CSV",
            FileName = _exporter.DefaultFileName(_completion, "csv"),
            Filter = "Файлы CSV (*.csv)|*.csv|Все файлы (*.*)|*.*",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog(this) != true) return;

        try
        {
            await _exporter.ExportCsvAsync(_completion, _organization, dialog.FileName);
            MessageBox.Show(this, $"Акт сохранён:\n{dialog.FileName}", "CSV", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось сохранить файл.\n\n{ex.Message}", "CSV",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string DefaultFileName(string extension)
    {
        var number = string.IsNullOrWhiteSpace(_completion.DocumentNumber) ? "бн" : _completion.DocumentNumber.Trim();
        var name = $"Акт выполненных работ {number} от {_completion.Date:yyyy-MM-dd}";

        foreach (var invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');

        return $"{name}.{extension}";
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
