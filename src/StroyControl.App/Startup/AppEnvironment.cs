namespace StroyControl.App.Startup;

/// <summary>Параметры запуска клиента: режим хранения данных и реквизиты подключения.</summary>
public sealed class AppEnvironment
{
    public bool IsDemoMode { get; init; }

    public string ConnectionString { get; init; } = string.Empty;

    public string Organization { get; init; } = "СУ-14, г. Минск";

    public string ProviderDisplay => IsDemoMode ? "Демо-режим (данные в памяти)" : "Microsoft SQL Server (сетевая БД)";

    public string ServerDisplay { get; init; } = "—";

    public string DatabaseDisplay { get; init; } = "—";

    public string Version => typeof(AppEnvironment).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    public static AppEnvironment Create(string connectionString, bool demoMode, string organization)
    {
        var server = @".\SQLEXPRESS";
        var port = "1433";
        var database = "StroyControl";

        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;

            switch (kv[0].Trim().ToLowerInvariant())
            {
                case "server":
                case "data source":
                case "host":
                    // SQL Server допускает запись Server=host,порт — разбираем на части.
                    var address = kv[1].Trim();
                    var comma = address.LastIndexOf(',');
                    if (comma > 0)
                    {
                        server = address[..comma].Trim();
                        port = address[(comma + 1)..].Trim();
                    }
                    else
                    {
                        server = address;
                    }
                    break;
                case "port":
                    port = kv[1].Trim();
                    break;
                case "database":
                case "initial catalog":
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
