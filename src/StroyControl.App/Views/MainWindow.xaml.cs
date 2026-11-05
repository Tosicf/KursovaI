using System.Windows;
using StroyControl.App.Startup;
using StroyControl.App.ViewModels;

namespace StroyControl.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IApplicationController _controller;

    public MainWindow(MainViewModel viewModel, IApplicationController controller)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _controller = controller;
        DataContext = viewModel;
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.InitializeAsync();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Closed -= OnClosed;
        _controller.OnMainWindowClosed();
    }
}
