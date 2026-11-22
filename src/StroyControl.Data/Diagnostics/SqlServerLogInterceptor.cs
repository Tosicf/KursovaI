using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Diagnostics;
using StroyControl.Core.Diagnostics;
using StroyControl.Core.Interfaces;

namespace StroyControl.Data.Diagnostics;

/// <summary>
/// Перехватчик EF Core: каждую выполненную команду записывает в журнал серверной
/// части — текст SQL, число параметров, время выполнения и число затронутых строк.
/// Благодаря этому консоль показывает реальную работу с базой данных.
/// </summary>
public sealed partial class SqlServerLogInterceptor : DbCommandInterceptor
{
    private const int MaxSqlLength = 180;

    private readonly IServerLog _log;

    public SqlServerLogInterceptor(IServerLog log) => _log = log;

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        LogCommand(eventData, command);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
        DbDataReader result, CancellationToken cancellationToken = default)
    {
        LogCommand(eventData, command);
        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        LogCommand(eventData, command, result);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
        int result, CancellationToken cancellationToken = default)
    {
        LogCommand(eventData, command, result);
        return ValueTask.FromResult(result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        LogCommand(eventData, command);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
        object? result, CancellationToken cancellationToken = default)
    {
        LogCommand(eventData, command);
        return ValueTask.FromResult(result);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        => _log.Error("SQL", $"{Format(command)} — ОШИБКА: {eventData.Exception.GetBaseException().Message}");

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _log.Error("SQL", $"{Format(command)} — ОШИБКА: {eventData.Exception.GetBaseException().Message}");
        return Task.CompletedTask;
    }

    private void LogCommand(CommandExecutedEventData eventData, DbCommand command, int? affected = null)
    {
        var ms = eventData.Duration.TotalMilliseconds;
        var tail = affected.HasValue ? $"{affected} стр. за {ms:0.0} мс" : $"{ms:0.0} мс";
        _log.Debug("SQL", $"{Format(command)} · {tail}");
    }

    private static string Format(DbCommand command) => Shorten(Whitespace().Replace(command.CommandText, " ").Trim());

    private static string Shorten(string sql)
        => sql.Length <= MaxSqlLength ? sql : sql[..MaxSqlLength] + " …";

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
