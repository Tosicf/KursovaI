using StroyControl.Core.Diagnostics;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;
using StroyControl.Core.Services;
using Xunit;

namespace StroyControl.Core.Tests;

/// <summary>
/// Правила фиксации сметы: изменять документ можно только в состоянии «Черновик».
/// После подтверждения состав и суммы заморожены — это защита от «подгонки» плана под факт.
/// </summary>
public sealed class EstimateLockTests
{
    [Theory]
    [InlineData(EstimateStatus.Draft, true)]
    [InlineData(EstimateStatus.OnReview, false)]
    [InlineData(EstimateStatus.Approved, false)]
    [InlineData(EstimateStatus.InWork, false)]
    [InlineData(EstimateStatus.Archived, false)]
    public void IsEditable_OnlyForDraft(EstimateStatus status, bool expected)
    {
        var estimate = new Estimate { Number = "СМ-01", Status = status };

        Assert.Equal(expected, estimate.IsEditable);
        Assert.Equal(!expected, estimate.IsFixed);
    }

    [Fact]
    public void IsArchived_OnlyForArchivedStatus()
    {
        Assert.True(new Estimate { Status = EstimateStatus.Archived }.IsArchived);
        Assert.False(new Estimate { Status = EstimateStatus.Approved }.IsArchived);
        Assert.False(new Estimate { Status = EstimateStatus.Draft }.IsArchived);
    }

    [Fact]
    public void IsInWork_OnlyForWorkStatus()
    {
        Assert.True(new Estimate { Status = EstimateStatus.InWork }.IsInWork);
        Assert.False(new Estimate { Status = EstimateStatus.Approved }.IsInWork);
        Assert.False(new Estimate { Status = EstimateStatus.Archived }.IsInWork);
    }

    [Theory]
    [InlineData(EstimateStatus.Draft)]
    [InlineData(EstimateStatus.OnReview)]
    [InlineData(EstimateStatus.Approved)]
    [InlineData(EstimateStatus.InWork)]
    [InlineData(EstimateStatus.Archived)]
    public void LockDisplay_IsNeverEmpty(EstimateStatus status)
    {
        var estimate = new Estimate { Status = status };

        Assert.False(string.IsNullOrWhiteSpace(estimate.LockDisplay));
        Assert.Contains(estimate.StatusDisplay, estimate.LockDisplay, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ServerLog_KeepsOnlyLastEntries()
    {
        var log = new ServerLog();

        for (var index = 0; index < 1200; index++)
            log.Info("SQL", $"запрос {index}");

        var entries = log.Snapshot(2000);

        Assert.Equal(1000, entries.Count);
        Assert.Equal("запрос 1199", entries[^1].Message);
        Assert.Equal(ServerLogLevel.Info, entries[^1].Level);
    }

    [Fact]
    public void ServerLog_RaisesWrittenEvent()
    {
        var log = new ServerLog();
        ServerLogEntry? captured = null;
        log.Written += (_, entry) => captured = entry;

        log.Warning("Сметы", "попытка изменить подтверждённую смету");

        Assert.NotNull(captured);
        Assert.Equal(ServerLogLevel.Warning, captured!.Level);
        Assert.Equal("Сметы", captured.Category);
    }
}
