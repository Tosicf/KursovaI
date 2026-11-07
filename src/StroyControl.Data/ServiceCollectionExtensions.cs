using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StroyControl.Core.Interfaces;
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
        bool useInMemory = false)
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
        });

        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IMaterialRepository, MaterialRepository>();
        services.AddSingleton<ISupplierRepository, SupplierRepository>();
        services.AddSingleton<IProjectRepository, ProjectRepository>();
        services.AddSingleton<IEstimateRepository, EstimateRepository>();
        services.AddSingleton<IDeliveryRepository, DeliveryRepository>();
        services.AddSingleton<IUsageRepository, UsageRepository>();

        services.AddSingleton<IInventoryService, InventoryService>();
        services.AddSingleton<IDeviationService, DeviationService>();
        services.AddSingleton<IDashboardService, DashboardService>();
        services.AddSingleton<IDatabaseHealthService, DatabaseHealthService>();

        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<ISessionService, SessionService>();

        return services;
    }
}
