using System.Windows;

namespace StroyControl.App.Views.Dialogs;

/// <summary>Модальное подтверждение опасного действия (удаление, отмена изменений).</summary>
public partial class ConfirmWindow : Window
{
    public ConfirmWindow(string title, string message, string confirmText = "Удалить")
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmText;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
