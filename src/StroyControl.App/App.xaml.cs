using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StroyControl.App.Services;
using StroyControl.App.Startup;
using StroyControl.App.ViewModels;
using StroyControl.App.Views;
using StroyControl.Core.Interfaces;
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

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ConfigureCulture();
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        try
        {
            await StartAsync();
        }
        catch (Exception ex)
        {
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

        var demoMode = false;
        var probe = await SqlServerProbe.TryConnectAsync(connectionString);

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

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(environment);

        services.AddStroyControlData(connectionString, demoMode);
        RegisterApplicationServices(services);

        _services = services.BuildServiceProvider();

        await DatabaseInitializer.InitializeAsync(_services);
        _services.GetRequiredService<IApplicationController>().ShowLoginAndStart();
    }

    private static void RegisterApplicationServices(IServiceCollection services)
    {
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IApplicationController, ApplicationController>();

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

    private static void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"Произошла непредвиденная ошибка.\n\n{e.Exception.Message}",
            "СтройКонтроль",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }
}
