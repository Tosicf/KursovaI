using System.Windows;
using StroyControl.App.ViewModels.Editors;

namespace StroyControl.App.Views.Dialogs;

/// <summary>
/// Универсальное окно редактирования: форма подставляется шаблоном данных
/// по типу модели представления, кнопки связаны с командами этой модели.
/// </summary>
public partial class EditorWindow : Window
{
    public EditorWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is EditorViewModelBase oldViewModel)
            oldViewModel.CloseRequested -= OnCloseRequested;

        if (e.NewValue is EditorViewModelBase newViewModel)
            newViewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, bool result)
    {
        DialogResult = result;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
