using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using StroyControl.App.Views;
using StroyControl.Core.Interfaces;

namespace StroyControl.App.Startup;

public sealed class ApplicationController : IApplicationController
{
    private readonly IServiceProvider _services;
    private bool _isShuttingDown;

    public ApplicationController(IServiceProvider services) => _services = services;

    public void ShowLoginAndStart()
    {
        var session = _services.GetRequiredService<ISessionService>();
        var login = _services.GetRequiredService<LoginWindow>();

        if (login.ShowDialog() == true && session.IsAuthenticated)
            ShowMainWindow();
        else
            Shutdown();
    }

    public void Logout()
    {
        _services.GetRequiredService<ISessionService>().SignOut();
        Application.Current.MainWindow?.Close();
    }

    public void OnMainWindowClosed()
    {
        if (_isShuttingDown) return;

        var session = _services.GetRequiredService<ISessionService>();
        if (session.IsAuthenticated)
        {
            Shutdown();
            return;
        }

        ShowLoginAndStart();
    }

    private void ShowMainWindow()
    {
        var window = _services.GetRequiredService<MainWindow>();
        Application.Current.MainWindow = window;
        window.Show();
    }

    private void Shutdown()
    {
        _isShuttingDown = true;
        Application.Current.Shutdown();
    }
}
