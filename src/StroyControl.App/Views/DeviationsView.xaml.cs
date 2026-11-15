using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace StroyControl.App.Views;

public partial class DeviationsView : UserControl
{
    public DeviationsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) =>
        Dispatcher.BeginInvoke(new Action(RecalculateColumns), DispatcherPriority.Loaded);

    /// <summary>
    /// WPF при первом показе таблицы с группировкой иногда считает «резиновую» колонку
    /// по неполной ширине: она забирает всё окно, а остальные столбцы уезжают за край.
    /// Повторное присвоение звёздной ширины заставляет DataGrid пересчитать ширины всех
    /// столбцов уже по фактическому размеру таблицы.
    /// </summary>
    private void RecalculateColumns()
    {
        if (RowsGrid.Columns.Count == 0) return;

        var column = RowsGrid.Columns[0];
        if (column.Width.UnitType != DataGridLengthUnitType.Star) return;

        var min = column.MinWidth;
        var max = column.MaxWidth;

        column.MinWidth = 0;
        column.MaxWidth = double.PositiveInfinity;
        column.Width = new DataGridLength(min > 0 ? min : 240);
        column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
        column.MinWidth = min;
        column.MaxWidth = max;
    }
}
