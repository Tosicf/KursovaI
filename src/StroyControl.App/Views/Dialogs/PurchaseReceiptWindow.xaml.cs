using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using StroyControl.App.Views.Reports;
using StroyControl.Core.Entities;

namespace StroyControl.App.Views.Dialogs;

/// <summary>
/// Предпросмотр товарного чека по поступлению: тот же FlowDocument печатается
/// на принтере (PDF — через «Microsoft Print to PDF»).
/// </summary>
public partial class PurchaseReceiptWindow : Window
{
    private readonly Delivery _delivery;
    private readonly string _organization;

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
