using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StroyControl.App.Localization;
using StroyControl.App.Services;
using StroyControl.App.Startup;
using StroyControl.Core.Diagnostics;
using StroyControl.App.ViewModels;
using StroyControl.App.Views;
using StroyControl.Core.Interfaces;
using StroyControl.Core.Services;
using StroyControl.Data;
using StroyControl.Data.Seed;

namespace StroyControl.App;

/// <summary>
/// Точка входа клиента: чтение конфигурации, проверка сети, сборка контейнера
/// зависимостей, подготовка демо-данных и показ окна входа.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _services;
    private IServerLog? _serverLog;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ConfigureCulture();
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        try
        {
            await StartAsync();
        }
        catch (Exception ex)
        {
            if (_serverLog is not null)
                _serverLog.Error("Ошибка", $"Запуск прерван: {ex.GetBaseException().Message}");
            var error = ex.GetBaseException();   // ← достаём настоящую причину
            MessageBox.Show(
                $"Не удалось запустить приложение «СтройКонтроль».\n\n" +
                $"{error.GetType().Name}: {error.Message}\n\n{error.StackTrace}",
                "Ошибка запуска",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(-1);
        }
    }

    private async Task StartAsync()
    {
        // Тема применяется до показа окон, чтобы не было «мигания» светлым фоном.
        ThemeManager.Apply(ThemeManager.LoadSavedTheme(), persist: false);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        // Приоритет: переменная окружения (удобно для лабораторных и чужих ПК) →
        // ConnectionStrings:SqlServer → Database:ConnectionString в appsettings.json.
        var connectionString = Environment.GetEnvironmentVariable("STROYCONTROL_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = configuration.GetConnectionString("SqlServer")
                               ?? configuration["Database:ConnectionString"];

        connectionString ??= string.Empty;
        var demoModeAllowed = configuration.GetValue("Application:DemoModeAllowed", true);
        var organization = configuration["Application:Organization"] ?? "СУ-14, г. Минск";

        // Консоль серверной части создаётся до первого обращения к базе,
        // чтобы в ней были видны и проверка подключения, и создание схемы.
        var serverLog = new ServerLog();
        _serverLog = serverLog;

        var showConsole = configuration.GetValue("Application:ShowServerConsole", true);
        var consoleOpened = showConsole && ServerConsole.Attach(serverLog, "СтройКонтроль — серверный журнал");

        serverLog.Info("Запуск", $"Приложение запущено · организация: {organization}");
        serverLog.Info("Запуск", consoleOpened
            ? "Консоль серверного журнала открыта"
            : "Консоль не открыта (отключена в appsettings.json или недоступна) — журнал хранится в памяти");

        var demoMode = false;
        var probe = await SqlServerProbe.TryConnectAsync(connectionString);

        serverLog.Write(probe.Success ? StroyControl.Core.Enums.ServerLogLevel.Info
                                      : StroyControl.Core.Enums.ServerLogLevel.Warning,
            "Подключение", probe.Success
                ? "SQL Server доступен — проверка подключения пройдена"
                : $"SQL Server недоступен: {probe.Error}");

        if (!probe.Success)
        {
            if (!demoModeAllowed)
            {
                MessageBox.Show(
                    $"Сервер SQL Server недоступен.\n\n{probe.Error}\n\nПроверьте, запущена ли служба SQL Server, и строку подключения в appsettings.json.",
                    "Нет связи с базой данных",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var answer = MessageBox.Show(
                $"Сервер SQL Server недоступен.\n\n{probe.Error}\n\n" +
                "Запустить приложение в демонстрационном режиме? Данные будут храниться в памяти процесса " +
                "и не сохранятся после закрытия.",
                "Нет связи с базой данных",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (answer != MessageBoxResult.Yes)
            {
                Shutdown();
                return;
            }

            demoMode = true;
        }

        var environment = AppEnvironment.Create(connectionString, demoMode, organization);

        // Язык интерфейса читается до создания окон: от него зависят все подписи.
        LocalizationService.Instance.Load();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(environment);
        services.AddSingleton<IServerLog>(serverLog);

        services.AddStroyControlData(connectionString, demoMode, serverLog);
        RegisterApplicationServices(services);

        _services = services.BuildServiceProvider();

        serverLog.Info("База данных", "Проверка схемы и загрузка демонстрационных данных…");
        await DatabaseInitializer.InitializeAsync(_services);
        serverLog.Info("База данных", "Схема проверена, приложение готово к работе");
        _services.GetRequiredService<IApplicationController>().ShowLoginAndStart();
    }

    private static void RegisterApplicationServices(IServiceCollection services)
    {
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IApplicationController, ApplicationController>();

        // Переходы между разделами по запросу страницы («Объекты» → отклонения/сметы объекта).
        services.AddSingleton<PageNavigator>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginWindow>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();

        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<MaterialsViewModel>();
        services.AddSingleton<SuppliersViewModel>();
        services.AddSingleton<ProjectsViewModel>();
        services.AddSingleton<EstimatesViewModel>();
        services.AddSingleton<DeliveriesViewModel>();
        services.AddSingleton<UsagesViewModel>();
        services.AddSingleton<DeviationsViewModel>();
        services.AddSingleton<ArchiveViewModel>();
        services.AddSingleton<UsersViewModel>();
        services.AddSingleton<SettingsViewModel>();
    }

    private static void ConfigureCulture()
    {
        var culture = CultureInfo.GetCultureInfo("ru-RU");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        if (_serverLog is not null)
            _serverLog.Error("Ошибка", $"Необработанное исключение интерфейса: {e.Exception.GetBaseException().Message}");

        MessageBox.Show(
            $"Произошла непредвиденная ошибка.\n\n{e.Exception.Message}",
            "СтройКонтроль",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var message = (e.ExceptionObject as Exception)?.GetBaseException().Message ?? "неизвестная ошибка";
        if (_serverLog is not null)
            _serverLog.Error("Ошибка", $"Критическая ошибка процесса: {message}");

        try
        {
            if (_serverLog is not null)
                _serverLog.Info("Ошибка", "Приложение завершает работу");
        }
        catch
        {
            // Журнал может быть уже недоступен при аварийном завершении.
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (_serverLog is not null)
            _serverLog.Error("Ошибка", $"Ошибка фоновой задачи: {e.Exception.GetBaseException().Message}");
        e.SetObserved();
    }
}
