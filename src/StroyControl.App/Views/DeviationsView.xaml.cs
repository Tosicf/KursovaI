using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace StroyControl.App.Views;

/// <summary>
/// Форма «Анализ отклонений». Отдельный код нужен из-за особенности WPF: в таблице
/// с группировкой колонка с Width="*" при первом показе окна получает неверную ширину,
/// и все столбцы, кроме первого, уезжают за правый край (помогало только свёртывание
/// и повторное развёртывание окна). Поэтому «Материал» имеет обычную ширину, а растягивается
/// по фактическому размеру таблицы вручную — см. <see cref="FitNameColumn"/>.
/// </summary>
public partial class DeviationsView : UserControl
{
    private const double NameColumnMinWidth = 240d;
    private const double NameColumnMaxWidth = 900d;

    /// <summary>Место под рамку таблицы, отступы и вертикальную полосу прокрутки.</summary>
    private const double ReservedWidth = 40d;

    public DeviationsView()
    {
        InitializeComponent();

        // После полной раскладки окна ширина таблицы уже настоящая — подгоняем колонку.
        Loaded += (_, _) => Dispatcher.BeginInvoke(new Action(FitNameColumn), DispatcherPriority.Loaded);
    }

    private void OnRowsGridSizeChanged(object sender, SizeChangedEventArgs e) => FitNameColumn();

    /// <summary>
    /// Растягивает колонку «Материал» на всё свободное место таблицы: суммарная ширина
    /// остальных столбцов вычитается из фактической ширины DataGrid.
    /// </summary>
    private void FitNameColumn()
    {
        if (RowsGrid is null || RowsGrid.Columns.Count == 0) return;

        var total = RowsGrid.ActualWidth;
        if (total <= 0) return;

        double used = 0;
        for (var i = 1; i < RowsGrid.Columns.Count; i++)
        {
            var column = RowsGrid.Columns[i];
            used += column.ActualWidth > 0 ? column.ActualWidth : column.Width.Value;
        }

        var available = total - used - ReservedWidth;
        var target = Math.Max(NameColumnMinWidth, Math.Min(NameColumnMaxWidth, available));

        var nameColumn = RowsGrid.Columns[0];
        if (nameColumn.Width.IsAbsolute && Math.Abs(nameColumn.Width.Value - target) < 4d) return;

        nameColumn.Width = new DataGridLength(target);
    }
}
