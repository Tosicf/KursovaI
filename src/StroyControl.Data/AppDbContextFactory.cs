using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StroyControl.Data;

/// <summary>
/// Фабрика для консольных команд EF Core (миграции):
/// dotnet ef migrations add InitialCreate --project src/StroyControl.Data --startup-project src/StroyControl.App
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("STROYCONTROL_CONNECTION")
            ?? "Server=localhost;Port=3306;Database=stroycontrol;User=root;Password=root;";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        return new AppDbContext(options);
    }
}
