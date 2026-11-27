using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StroyControl.Core.Diagnostics;
using StroyControl.Core.Interfaces;
using StroyControl.Data.Diagnostics;
using StroyControl.Core.Services;
using StroyControl.Data.Repositories;
using StroyControl.Data.Services;

namespace StroyControl.Data;

/// <summary>Регистрация слоя данных и бизнес-сервисов в контейнере зависимостей.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Подключает Microsoft SQL Server (EF Core + SqlClient) либо, при
    /// <paramref name="useInMemory"/> = true, демонстрационную базу в памяти.
    /// Фабрика контекста позволяет WPF-клиенту создавать короткоживущий DbContext
    /// на каждую операцию.
    /// </summary>
    public static IServiceCollection AddStroyControlData(
        this IServiceCollection services,
        string connectionString,
        bool useInMemory = false,
        IServerLog? serverLog = null)
    {
        services.AddDbContextFactory<AppDbContext>(options =>
        {
            if (useInMemory)
            {
                options.UseInMemoryDatabase("StroyControlDemo");
                options.EnableSensitiveDataLogging();
            }
            else
            {
                options.UseSqlServer(
                    connectionString,
                    sql => sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(3), null));
            }

            // Все команды, которые EF Core отправляет в Microsoft SQL Server,
            // попадают в журнал серверной части и видны в консольном окне.
            if (serverLog is not null)
                options.AddInterceptors(new SqlServerLogInterceptor(serverLog));
        });

        if (serverLog is not null)
        {
            serverLog.Info("Запуск", useInMemory
                ? "Провайдер данных: InMemory (демонстрационный режим, данные не сохраняются)"
                : $"Провайдер данных: Microsoft SQL Server, экземпляр {DescribeServer(connectionString)}");
        }

        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IMaterialRepository, MaterialRepository>();
        services.AddSingleton<ISupplierRepository, SupplierRepository>();
        services.AddSingleton<IProjectRepository, ProjectRepository>();
        services.AddSingleton<IEstimateRepository, EstimateRepository>();
        services.AddSingleton<IDeliveryRepository, DeliveryRepository>();
        services.AddSingleton<IUsageRepository, UsageRepository>();
        services.AddSingleton<IWorkCompletionRepository, WorkCompletionRepository>();

        services.AddSingleton<IInventoryService, InventoryService>();
        services.AddSingleton<IWorkCompletionService, WorkCompletionService>();
        services.AddSingleton<IDeviationService, DeviationService>();
        services.AddSingleton<IMinStockService, MinStockService>();
        services.AddSingleton<IDashboardService, DashboardService>();
        services.AddSingleton<IDatabaseHealthService, DatabaseHealthService>();

        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<ISessionService, SessionService>();

        return services;
    }

    /// <summary>Короткое описание целевого сервера для журнала (без учётных данных).</summary>
    private static string DescribeServer(string connectionString)
    {
        try
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
            var server = string.IsNullOrWhiteSpace(builder.DataSource) ? "не указан" : builder.DataSource;
            var database = string.IsNullOrWhiteSpace(builder.InitialCatalog) ? "StroyControl" : builder.InitialCatalog;
            return $"{server} / база {database}";
        }
        catch
        {
            return "строка подключения не разобрана";
        }
    }
}
