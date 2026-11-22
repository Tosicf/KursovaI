using StroyControl.Core.Enums;
using StroyControl.Core.Interfaces;

namespace StroyControl.Core.Diagnostics;

/// <summary>Короткие методы записи в журнал серверной части.</summary>
public static class ServerLogExtensions
{
    public static void Debug(this IServerLog log, string category, string message)
        => log.Write(ServerLogLevel.Debug, category, message);

    public static void Info(this IServerLog log, string category, string message)
        => log.Write(ServerLogLevel.Info, category, message);

    public static void Warning(this IServerLog log, string category, string message)
        => log.Write(ServerLogLevel.Warning, category, message);

    public static void Error(this IServerLog log, string category, string message)
        => log.Write(ServerLogLevel.Error, category, message);
}
