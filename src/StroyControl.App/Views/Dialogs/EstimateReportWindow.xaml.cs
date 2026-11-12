using System.Windows;
using System.Windows.Documents;
using Microsoft.Win32;
using StroyControl.App.ViewModels;
using StroyControl.App.Views.Reports;
using StroyControl.Core.Services;

namespace StroyControl.App.Views.Dialogs;

/// <summary>
/// Окно отчёта по смете: предпросмотр печатного документа, настройка коэффициентов,
/// выгрузка в Excel/CSV и печать (в том числе в PDF через «Microsoft Print to PDF»).
/// </summary>
public partial class EstimateReportWindow : Window
{
    private readonly EstimateReportViewModel _viewModel;
    private readonly EstimateReportExporter _exporter = new();

    public EstimateReportWindow(EstimateReportViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.ReportChanged += OnReportChanged;

        RefreshDocument();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.ReportChanged -= OnReportChanged;
        base.OnClosed(e);
    }

    private void OnReportChanged(object? sender, EventArgs e) => RefreshDocument();

    private void RefreshDocument() => Viewer.Document = EstimateReportDocument.Build(_viewModel.Report);

    private void OnResetClick(object sender, RoutedEventArgs e) => _viewModel.ResetCoefficientsCommand.Execute(null);

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnPrintClick(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Controls.PrintDialog();
        if (dialog.ShowDialog() != true) return;

        try
        {
            var document = EstimateReportDocument.Build(_viewModel.Report);
            document.PageWidth = dialog.PrintableAreaWidth;
            document.PageHeight = dialog.PrintableAreaHeight;
            document.PagePadding = new Thickness(40);
            document.ColumnWidth = dialog.PrintableAreaWidth;

            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
            paginator.PageSize = new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);

            dialog.PrintDocument(paginator, $"Смета {_viewModel.Report.Number}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось напечатать отчёт.\n\n{ex.Message}", "Печать",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnExcelClick(object sender, RoutedEventArgs e)
    {
        var path = AskPath("Сохранить отчёт в Excel", "xlsx",
            "Книга Excel (*.xlsx)|*.xlsx|Все файлы (*.*)|*.*");

        if (path is null) return;

        try
        {
            await _exporter.ExportXlsxAsync(_viewModel.Report, path);
            MessageBox.Show(this, $"Отчёт сохранён:\n{path}", "Excel", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось сохранить файл.\n\n{ex.Message}", "Excel",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnCsvClick(object sender, RoutedEventArgs e)
    {
        var path = AskPath("Сохранить отчёт в CSV", "csv",
            "Файлы CSV (*.csv)|*.csv|Все файлы (*.*)|*.*");

        if (path is null) return;

        try
        {
            await _exporter.ExportCsvAsync(_viewModel.Report, path);
            MessageBox.Show(this, $"Отчёт сохранён:\n{path}", "CSV", MessageBoxButton.OK, MessageBoxImage.Information);
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
            FileName = _exporter.DefaultFileName(_viewModel.Report, extension),
            Filter = filter,
            AddExtension = true,
            OverwritePrompt = true
        };

        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }
}
