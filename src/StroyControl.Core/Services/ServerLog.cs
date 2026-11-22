using StroyControl.Core.Diagnostics;
using StroyControl.Core.Enums;
using StroyControl.Core.Interfaces;

namespace StroyControl.Core.Services;

/// <summary>
/// Потокобезопасный журнал серверной части с кольцевым буфером на 1000 записей.
/// Реализация не зависит от интерфейса: слой данных пишет в журнал, а клиент
/// подписывается на событие <see cref="Written"/> и выводит строки в консоль.
/// </summary>
public sealed class ServerLog : IServerLog
{
    private const int Capacity = 1000;

    private readonly object _gate = new();
    private readonly Queue<ServerLogEntry> _entries = new(Capacity);

    public event EventHandler<ServerLogEntry>? Written;

    public void Write(ServerLogLevel level, string category, string message)
    {
        var entry = new ServerLogEntry(DateTime.Now, level, category ?? "—", message ?? string.Empty);

        lock (_gate)
        {
            _entries.Enqueue(entry);
            while (_entries.Count > Capacity)
                _entries.Dequeue();
        }

        Written?.Invoke(this, entry);
    }

    public IReadOnlyList<ServerLogEntry> Snapshot(int count = 200)
    {
        lock (_gate)
        {
            if (count <= 0) return Array.Empty<ServerLogEntry>();
            return _entries.Count <= count
                ? _entries.ToArray()
                : _entries.Skip(_entries.Count - count).ToArray();
        }
    }
}
