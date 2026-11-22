using StroyControl.Core.Diagnostics;
using StroyControl.Core.Enums;

namespace StroyControl.Core.Interfaces;

/// <summary>
/// Журнал серверной части. Слой данных пишет в него выполненные SQL-команды,
/// транзакции и ошибки, а клиент показывает записи в отдельном консольном окне —
/// поэтому работа с базой данных становится наблюдаемой.
/// </summary>
public interface IServerLog
{
    /// <summary>Добавляет запись в журнал. Метод безопасен при вызове из любого потока.</summary>
    void Write(ServerLogLevel level, string category, string message);

    /// <summary>Последние записи журнала (для первого заполнения консоли).</summary>
    IReadOnlyList<ServerLogEntry> Snapshot(int count = 200);

    /// <summary>Событие новой записи — на него подписывается консольное окно.</summary>
    event EventHandler<ServerLogEntry>? Written;
}
