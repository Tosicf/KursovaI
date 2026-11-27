using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StroyControl.App.ViewModels;
using StroyControl.Core.Entities;

namespace StroyControl.App.Views;

public partial class WorkCompletionsView : UserControl
{
    public WorkCompletionsView() => InitializeComponent();

    /// <summary>
    /// Двойной щелчок по строке открывает акт на редактирование — привычный способ
    /// поправить объём работ или израсходованные материалы.
    /// </summary>
    private void OnRowDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid || grid.SelectedItem is not WorkCompletion act) return;
        if (DataContext is not WorkCompletionsViewModel viewModel) return;

        if (viewModel.EditCommand.CanExecute(act))
            viewModel.EditCommand.Execute(act);
    }
}
