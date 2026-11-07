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
            ?? @"Server=.\SQLEXPRESS;Database=StroyControl;Trusted_Connection=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
