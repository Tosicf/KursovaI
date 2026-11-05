namespace StroyControl.App.Startup;

/// <summary>Параметры запуска клиента: режим хранения данных и реквизиты подключения.</summary>
public sealed class AppEnvironment
{
    public bool IsDemoMode { get; init; }

    public string ConnectionString { get; init; } = string.Empty;

    public string Organization { get; init; } = "СУ-14, г. Минск";

    public string ProviderDisplay => IsDemoMode ? "Демо-режим (данные в памяти)" : "MySQL 8 (сетевая БД)";

    public string ServerDisplay { get; init; } = "—";

    public string DatabaseDisplay { get; init; } = "—";

    public string Version => typeof(AppEnvironment).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    public static AppEnvironment Create(string connectionString, bool demoMode, string organization)
    {
        var server = "localhost";
        var port = "3306";
        var database = "stroycontrol";

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;

            switch (kv[0].Trim().ToLowerInvariant())
            {
                case "server":
                case "host":
                    server = kv[1].Trim();
                    break;
                case "port":
                    port = kv[1].Trim();
                    break;
                case "database":
                    database = kv[1].Trim();
                    break;
            }
        }

        return new AppEnvironment
        {
            IsDemoMode = demoMode,
            ConnectionString = connectionString,
            Organization = organization,
            ServerDisplay = demoMode ? "в памяти процесса" : $"{server}:{port}",
            DatabaseDisplay = demoMode ? "StroyControlDemo" : database
        };
    }
}
