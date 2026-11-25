using StroyControl.Core.Dtos;
using StroyControl.Core.Services;
using Xunit;

namespace StroyControl.Core.Tests;

/// <summary>Периоды документов: год, затем месяц; будущие даты в выборку не попадают.</summary>
public sealed class MonthPeriodTests
{
    [Fact]
    public void Start_ReturnsFirstDayOfMonth()
    {
        Assert.Equal(new DateTime(2026, 11, 1), MonthPeriod.Start(new DateTime(2026, 11, 24)));
        Assert.Equal(new DateTime(2026, 1, 1), MonthPeriod.Start(new DateTime(2026, 1, 31)));
    }

    [Theory]
    [InlineData(2026, 11, 3, true)]    // тот же год и месяц — попадает в период
    [InlineData(2026, 11, 30, true)]
    [InlineData(2026, 10, 31, false)]  // соседний месяц — не попадает
    [InlineData(2025, 11, 5, false)]   // тот же месяц, но другой год
    public void Matches_ComparesYearAndMonth(int year, int month, int day, bool expected)
    {
        Assert.Equal(expected, MonthPeriod.Matches(new DateTime(year, month, day), 2026, 11));
    }

    [Fact]
    public void Matches_WithoutYearAndMonth_AllowsAnyDate()
    {
        Assert.True(MonthPeriod.Matches(new DateTime(2020, 3, 15), null, null));
        Assert.True(MonthPeriod.Matches(DateTime.Today, null, null));
    }

    [Fact]
    public void Years_SkipFutureYearsAndGoNewestFirst()
    {
        var current = DateTime.Today.Year;
        var dates = new DateTime?[]
        {
            new(current, 3, 12),
            new(current + 3, 1, 10),   // будущий год — в список не попадает
            new(current - 1, 12, 31),
            null
        };

        var years = MonthPeriod.Years(dates);

        Assert.Equal(new[] { current, current - 1 }, years);
        Assert.DoesNotContain(current + 3, years);
    }

    [Fact]
    public void Years_AlwaysContainCurrentYear()
    {
        var years = MonthPeriod.Years(Array.Empty<DateTime?>());

        Assert.Single(years);
        Assert.Equal(DateTime.Today.Year, years[0]);
    }

    [Fact]
    public void Months_DoNotExceedCurrentMonth()
    {
        var today = DateTime.Today;
        var dates = new DateTime?[]
        {
            new(today.Year, 12, 15),                        // декабрь текущего года
            new(today.Year - 1, 12, 15),                    // декабрь прошлого года
            new(today.Year, today.Month, 1)
        };

        var currentYearMonths = MonthPeriod.Months(today.Year, dates);
        var previousYearMonths = MonthPeriod.Months(today.Year - 1, dates);

        Assert.All(currentYearMonths, month => Assert.True(month <= today.Month));
        Assert.Contains(today.Month, currentYearMonths);
        Assert.Equal(new[] { 12 }, previousYearMonths);
        Assert.Equal(previousYearMonths.OrderByDescending(m => m), previousYearMonths);
    }

    [Fact]
    public void MonthName_UsesRussianNames()
    {
        Assert.Equal("Ноябрь", MonthPeriod.MonthName(11));
        Assert.Equal("Май", MonthPeriod.MonthName(5));
        Assert.Equal(string.Empty, MonthPeriod.MonthName(13));
        Assert.Equal("Ноябрь 2026", MonthPeriod.Title(new DateTime(2026, 11, 15)));
        Assert.Equal("Ноя 2026", MonthPeriod.ShortTitle(new DateTime(2026, 11, 15)));
    }

    [Fact]
    public void PeriodFilter_DescribesPeriodAndItsEnd()
    {
        var all = PeriodFilter.All;
        Assert.True(all.IsAll);
        Assert.Equal("за всё время", all.Caption);
        Assert.Null(all.End);

        var year = new PeriodFilter(2026, null);
        Assert.Equal("за 2026 год", year.Caption);
        Assert.Equal(new DateTime(2026, 12, 31), year.End);
        Assert.Equal("по состоянию на конец 2026 года", year.BalanceCaption);

        var month = new PeriodFilter(2026, 11);
        Assert.Equal("за ноябрь 2026", month.Caption);
        Assert.Equal(new DateTime(2026, 11, 30), month.End);
        Assert.Equal("по состоянию на ноябрь 2026", month.BalanceCaption);
        Assert.Equal(new DateTime(2026, 11, 1), month.MonthStart);
    }

    [Fact]
    public void PeriodFilter_ContainsOnlySelectedYearAndMonth()
    {
        var month = new PeriodFilter(2026, 11);

        Assert.True(month.Contains(new DateTime(2026, 11, 30)));
        Assert.False(month.Contains(new DateTime(2026, 10, 31)));
        Assert.False(month.Contains(new DateTime(2025, 11, 5)));

        var year = new PeriodFilter(2026, null);

        Assert.True(year.Contains(new DateTime(2026, 1, 15)));
        Assert.False(year.Contains(new DateTime(2025, 12, 31)));
        Assert.True(PeriodFilter.All.Contains(new DateTime(2019, 7, 1)));
    }
}
