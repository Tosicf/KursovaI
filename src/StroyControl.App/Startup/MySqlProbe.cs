using MySqlConnector;

namespace StroyControl.App.Startup;

/// <summary>Быстрая проверка доступности сервера MySQL перед запуском приложения.</summary>
public static class MySqlProbe
{
    public static async Task<(bool Success, string Error)> TryConnectAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return (false, "Строка подключения не задана в appsettings.json (Database:ConnectionString).");

        try
        {
            var builder = new MySqlConnectionStringBuilder(connectionString)
            {
                ConnectionTimeout = 4
            };

            await using var connection = new MySqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
