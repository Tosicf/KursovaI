using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using StroyControl.App.Services;
using StroyControl.App.Views;
using StroyControl.Core.Interfaces;

namespace StroyControl.App.Startup;

public sealed class ApplicationController : IApplicationController
{
    private readonly IServiceProvider _services;
    private bool _isShuttingDown;

    /// <summary>Закрытие главного окна при перестройке оболочки не считается выходом.</summary>
    private bool _suppressCloseHandler;

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

    public void ReloadShell()
    {
        if (_isShuttingDown) return;
        if (Application.Current.MainWindow is not MainWindow current) return;

        // Шаблоны и стили разбираются один раз при загрузке приложения, поэтому тексты
        // внутри них нужно перечитать: словари ресурсов пересобираются заново.
        ReloadResourceDictionaries();

        var next = _services.GetRequiredService<MainWindow>();

        // Новое окно показывается до закрытия старого: приложение не остаётся без окон,
        // а обработчик закрытия не воспринимает событие как выход из системы.
        Application.Current.MainWindow = next;
        next.Show();

        _suppressCloseHandler = true;
        try
        {
            current.Close();
        }
        finally
        {
            _suppressCloseHandler = false;
        }
    }

    /// <summary>
    /// Перечитывает объединённые словари ресурсов приложения (стили, шаблоны, палитру),
    /// чтобы подписи внутри шаблонов были созданы уже на новом языке.
    /// </summary>
    private static void ReloadResourceDictionaries()
    {
        if (Application.Current is not { } app) return;

        var merged = app.Resources.MergedDictionaries;
        var sources = merged.Select(dictionary => dictionary.Source).ToList();

        merged.Clear();
        foreach (var source in sources)
        {
            if (source is null) continue;
            merged.Add(new ResourceDictionary { Source = source });
        }

        // Палитру возвращаем на место: ThemeManager подменяет её отдельным словарём.
        ThemeManager.Apply(ThemeManager.Current, persist: false);
    }

    public void OnMainWindowClosed()
    {
        if (_isShuttingDown || _suppressCloseHandler) return;

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
