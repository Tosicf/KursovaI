using StroyControl.Core.Enums;

namespace StroyControl.Core.Diagnostics;

/// <summary>
/// Одна запись журнала серверной части: время, уровень, категория (узел системы)
/// и текст сообщения. Записи неизменяемы и безопасно передаются между потоками.
/// </summary>
/// <param name="Timestamp">Момент события (локальное время).</param>
/// <param name="Level">Уровень важности.</param>
/// <param name="Category">Категория: «SQL», «Сметы», «Запуск», «Ошибка» и т. п.</param>
/// <param name="Message">Текст сообщения для консоли.</param>
public sealed record ServerLogEntry(DateTime Timestamp, ServerLogLevel Level, string Category, string Message)
{
    /// <summary>Строка для вывода в консоль: «12:03:44.512  SQL      SELECT …».</summary>
    public override string ToString() => $"{Timestamp:HH:mm:ss.fff}  {Category,-12} {Message}";
}
