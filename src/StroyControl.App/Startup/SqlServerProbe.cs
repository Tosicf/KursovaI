using Microsoft.Data.SqlClient;

namespace StroyControl.App.Startup;

/// <summary>
/// Быстрая проверка доступности сервера SQL Server перед запуском приложения.
/// Подключение выполняется к служебной базе <c>master</c>: базы приложения может
/// ещё не быть — её создаст EF Core (<c>EnsureCreated</c>) при первом запуске.
/// </summary>
public static class SqlServerProbe
{
    public static async Task<(bool Success, string Error)> TryConnectAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, "Строка подключения не задана в appsettings.json (Database:ConnectionString).");

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = 4,
                InitialCatalog = "master"
            };

            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
