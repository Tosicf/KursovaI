using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;
using StroyControl.App.Services;
using StroyControl.App.Views.Reports;
using StroyControl.Core.Dtos;
using StroyControl.Core.Enums;
using StroyControl.Core.Services;

namespace StroyControl.App.Views.Dialogs;

/// <summary>
/// Окно печатного отчёта по отклонениям: предпросмотр, печать, выгрузка CSV и PDF.
/// </summary>
public partial class DeviationReportWindow : Window
{
    private readonly DeviationReport _report;
    private readonly CsvReportExporter _csv = new();

    public DeviationReportWindow(DeviationReport report)
    {
        InitializeComponent();

        _report = report;
        DataContext = this;
        Viewer.Document = DeviationReportDocument.Build(_report);
    }

    public string Caption =>
        $"{_report.ScopeName} · позиций: {_report.Rows.Count} · сформирован {DateTime.Now:dd.MM.yyyy HH:mm}";

    public string SummaryBadge =>
        $"План {_report.PlannedTotal:N2} · Факт {_report.ActualTotal:N2} · " +
        $"{(int)_report.DeviationPercent:+0.0;-0.0;0} %";

    public string FormulaHint =>
        "Отклонение = факт − план, процент = отклонение ÷ план × 100 %";

    public Brush SeverityBackground => _report.Severity switch
    {
        DeviationSeverity.Critical => (Brush)FindResource("DangerSoftBrush"),
        DeviationSeverity.Warning => (Brush)FindResource("WarningSoftBrush"),
        _ => (Brush)FindResource("SuccessSoftBrush")
    };

    public Brush SeverityForeground => _report.Severity switch
    {
        DeviationSeverity.Critical => (Brush)FindResource("DangerBrush"),
        DeviationSeverity.Warning => (Brush)FindResource("WarningBrush"),
        _ => (Brush)FindResource("SuccessBrush")
    };

    private void OnExportClick(object sender, RoutedEventArgs e)
    {
        if (ExportButton.ContextMenu is not { } menu) return;

        menu.PlacementTarget = ExportButton;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private async void OnCsvClick(object sender, RoutedEventArgs e)
    {
        var path = AskPath("Сохранить отчёт в CSV", "csv",
            "Файлы CSV (*.csv)|*.csv|Все файлы (*.*)|*.*");

        if (path is null) return;

        try
        {
            await _csv.ExportDeviationReportAsync(_report, path);
            MessageBox.Show(this, $"Отчёт сохранён:\n{path}", "CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось сохранить файл.\n\n{ex.Message}", "CSV",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnPdfClick(object sender, RoutedEventArgs e)
    {
        var document = DeviationReportDocument.Build(_report);

        if (PdfPrinter.TryPrint(document, $"Отклонения {_report.ScopeName}", out var error))
        {
            MessageBox.Show(this, "Готово: PDF сохранён через принтер «Microsoft Print to PDF».",
                "PDF", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MessageBox.Show(this, error ?? "Не удалось выгрузить отчёт в PDF.", "PDF",
            MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void OnPrintClick(object sender, RoutedEventArgs e)
    {
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;

        try
        {
            var document = DeviationReportDocument.Build(_report);
            document.PageWidth = dialog.PrintableAreaWidth;
            document.PageHeight = dialog.PrintableAreaHeight;
            document.PagePadding = new Thickness(36);
            document.ColumnWidth = dialog.PrintableAreaWidth;

            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
            paginator.PageSize = new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);

            dialog.PrintDocument(paginator, $"Отклонения {_report.ScopeName}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось напечатать отчёт.\n\n{ex.Message}", "Печать",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private string? AskPath(string title, string extension, string filter)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            FileName = _csv.DefaultFileName(_report).Replace(".csv", "." + extension),
            Filter = filter,
            AddExtension = true,
            OverwritePrompt = true
        };

        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }
}
