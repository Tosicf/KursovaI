using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using StroyControl.Core.Diagnostics;
using StroyControl.Core.Enums;
using StroyControl.Core.Interfaces;

namespace StroyControl.App.Services;

/// <summary>
/// Консольное окно серверной части. WPF-приложение — оконное (WinExe), поэтому
/// консоли по умолчанию нет: при старте она создаётся вызовом AllocConsole, и в неё
/// построчно выводится журнал слоя данных (SQL-команды, транзакции, ошибки).
/// Если приложение запущено из консоли (dotnet run), используется существующая консоль.
/// </summary>
public static class ServerConsole
{
    private static readonly object Gate = new();
    private static IServerLog? _log;
    private static bool _attached;

    /// <summary>Создаёт (при необходимости) консоль и подписывает её на журнал.</summary>
    public static bool Attach(IServerLog log, string title = "СтройКонтроль — серверный журнал")
    {
        lock (Gate)
        {
            if (_attached) return true;

            if (!TryOpenConsole()) return false;

            _log = log;
            _log.Written += OnEntryWritten;
            _attached = true;
        }

        try
        {
            Console.Title = title;
        }
        catch
        {
            // Заголовок недоступен (например, консоль перенаправлена) — не критично.
        }

        WriteLine(string.Empty);
        WriteLine("================================================================================", ConsoleColor.DarkCyan);
        WriteLine("  СТРОЙКОНТРОЛЬ — СЕРВЕРНАЯ ЧАСТЬ", ConsoleColor.Cyan);
        WriteLine("  журнал слоя данных: подключение к SQL Server, команды EF Core, транзакции, ошибки", ConsoleColor.DarkGray);
        WriteLine("  окно можно свернуть; закрытие консоли завершает работу приложения", ConsoleColor.DarkGray);
        WriteLine("================================================================================", ConsoleColor.DarkCyan);
        WriteLine(string.Empty);

        // Показываем последние записи: консоль подключается уже после первых событий.
        foreach (var entry in log.Snapshot(40))
            Print(entry);

        WriteLine(string.Empty);

        return true;
    }

    private static void OnEntryWritten(object? sender, ServerLogEntry entry) => Print(entry);

    private static void Print(ServerLogEntry entry)
    {
        var (color, prefix) = entry.Level switch
        {
            ServerLogLevel.Error => (ConsoleColor.Red, "ОШИБКА"),
            ServerLogLevel.Warning => (ConsoleColor.Yellow, "ВНИМАНИЕ"),
            ServerLogLevel.Debug => (ConsoleColor.DarkGray, "SQL"),
            _ => (ConsoleColor.Gray, "ИНФО")
        };

        WriteLine($"{entry.Timestamp:HH:mm:ss.fff}  [{prefix,-8}] {entry.Category,-10} {entry.Message}", color);
    }

    private static void WriteLine(string text, ConsoleColor color = ConsoleColor.Gray)
    {
        lock (Gate)
        {
            try
            {
                var previous = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(text);
                Console.ForegroundColor = previous;
            }
            catch
            {
                // Консоль закрыта пользователем или недоступна — журнал остаётся в памяти.
            }
        }
    }

    private static bool TryOpenConsole()
    {
        try
        {
            if (GetConsoleWindow() == IntPtr.Zero && !AllocConsole())
                return false;

            // После AllocConsole стандартные потоки нужно открыть заново,
            // иначе Console.WriteLine(nothing) уходит в никуда.
            var stdout = Console.OpenStandardOutput();
            var writer = new StreamWriter(stdout, new UTF8Encoding(false)) { AutoFlush = true };
            Console.SetOut(writer);

            try
            {
                Console.OutputEncoding = new UTF8Encoding(false);
            }
            catch
            {
                // Кодировка консоли уже задана — кириллица выводится системной кодовой страницей.
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    // Импорт оставлен классическим (DllImport), а не LibraryImport: генератор
    // последнего создаёт unsafe-код и требует AllowUnsafeBlocks, что для двух
    // вызовов ядра избыточно. Предложения анализатора SYSLIB1054 здесь не нужны.
#pragma warning disable SYSLIB1054
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();
#pragma warning restore SYSLIB1054
}
