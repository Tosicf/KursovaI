using System.Windows;
using System.Windows.Input;
using StroyControl.App.ViewModels;

namespace StroyControl.App.Views;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.SignedIn += OnSignedIn;
    }

    private void OnSignedIn(object? sender, EventArgs e)
    {
        DialogResult = true;
    }

    private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
