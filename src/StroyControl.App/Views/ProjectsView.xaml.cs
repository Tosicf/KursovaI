using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StroyControl.App.Views;

public partial class ProjectsView : UserControl
{
    public ProjectsView() => InitializeComponent();

    /// <summary>
    /// Правый щелчок выделяет строку под курсором: пункты контекстного меню
    /// («Сметы объекта», «Анализ отклонений по объекту») работают с тем объектом,
    /// на который нажали, а не с ранее выбранным.
    /// </summary>
    private void OnRowRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid) return;

        var row = FindRow(e.OriginalSource as DependencyObject);
        if (row is null) return;

        row.IsSelected = true;
        grid.SelectedItem = row.Item;
    }

    private static DataGridRow? FindRow(DependencyObject? source)
    {
        while (source is not null && source is not DataGridRow)
            source = source is Visual ? VisualTreeHelper.GetParent(source) : LogicalTreeHelper.GetParent(source);

        return source as DataGridRow;
    }
}
