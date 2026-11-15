using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace StroyControl.App.Services;

/// <summary>
/// Печать отчёта в PDF средствами Windows: документ уходит на виртуальный принтер
/// «Microsoft Print to PDF», который сам предлагает выбрать имя файла. Отдельная
/// PDF-библиотека не нужна — поведение совпадает с обычной печатью.
/// </summary>
public static class PdfPrinter
{
    public const string PrinterName = "Microsoft Print to PDF";

    /// <summary>Установлен ли виртуальный PDF-принтер.</summary>
    public static bool IsAvailable => FindQueue() is not null;

    /// <summary>
    /// Печатает документ в PDF. Возвращает false, если принтер не найден или
    /// пользователь отменил выбор файла.
    /// </summary>
    public static bool TryPrint(FlowDocument document, string jobName, out string? error)
    {
        error = null;

        try
        {
            var queue = FindQueue();
            if (queue is null)
            {
                error = $"В системе не найден принтер «{PrinterName}». " +
                        "Включите компонент Windows «Печать в PDF»: Параметры → Дополнительные компоненты.";
                return false;
            }

            var dialog = new PrintDialog { PrintQueue = queue };
            var width = dialog.PrintableAreaWidth > 0 ? dialog.PrintableAreaWidth : document.PageWidth;
            var height = dialog.PrintableAreaHeight > 0 ? dialog.PrintableAreaHeight : document.PageHeight;

            document.PageWidth = width;
            document.PageHeight = height;
            document.PagePadding = new Thickness(36);
            document.ColumnWidth = width;

            var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
            paginator.PageSize = new Size(width, height);

            dialog.PrintDocument(paginator, jobName);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static PrintQueue? FindQueue()
    {
        try
        {
            // Сервер и коллекцию очередей не освобождаем: PrintQueue привязан к ним
            // и должен остаться работоспособным до конца печати.
            var server = new LocalPrintServer();
            var queues = server.GetPrintQueues(new[]
            {
                EnumeratedPrintQueueTypes.Local,
                EnumeratedPrintQueueTypes.Connections
            });

            return queues.FirstOrDefault(q =>
                q.Name.Contains(PrinterName, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }
    }
}
