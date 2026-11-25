using StroyControl.Core.Services;
using Xunit;

namespace StroyControl.Core.Tests;

/// <summary>Группировка документов по месяцам: единое правило для всех списков приложения.</summary>
public sealed class MonthPeriodTests
{
    [Fact]
    public void Start_ReturnsFirstDayOfMonth()
    {
        Assert.Equal(new DateTime(2026, 11, 1), MonthPeriod.Start(new DateTime(2026, 11, 24)));
        Assert.Equal(new DateTime(2026, 1, 1), MonthPeriod.Start(new DateTime(2026, 1, 31)));
    }

    [Theory]
    [InlineData(2026, 11, 3, true)]   // тот же месяц — попадает в период
    [InlineData(2026, 11, 30, true)]
    [InlineData(2026, 10, 31, false)]  // соседний месяц — не попадает
    [InlineData(2025, 11, 5, false)]   // тот же месяц, но другой год
    public void Matches_ComparesYearAndMonthOnly(int year, int month, int day, bool expected)
    {
        var monthStart = new DateTime(2026, 11, 1);

        Assert.Equal(expected, MonthPeriod.Matches(new DateTime(year, month, day), monthStart));
    }

    [Fact]
    public void Matches_WithoutMonth_AllowsAnyDate()
    {
        Assert.True(MonthPeriod.Matches(new DateTime(2020, 3, 15), null));
        Assert.True(MonthPeriod.Matches(DateTime.Today, null));
    }

    [Fact]
    public void Months_DistinctAndNewestFirst()
    {
        var dates = new DateTime?[]
        {
            new(2026, 3, 12),
            new(2026, 11, 2),
            new(2026, 3, 28),
            null,
            new(2025, 12, 31)
        };

        var months = MonthPeriod.Months(dates);

        Assert.Equal(3, months.Count);
        Assert.Equal(new DateTime(2026, 11, 1), months[0].Month);
        Assert.Equal(new DateTime(2026, 3, 1), months[1].Month);
        Assert.Equal(new DateTime(2025, 12, 1), months[2].Month);
        Assert.All(months, m => Assert.False(m.IsAll));
    }

    [Fact]
    public void Options_StartsWithAllMonthsAndContainsCurrentMonth()
    {
        var options = MonthPeriod.Options(new DateTime?[] { new(2024, 5, 20) });

        Assert.Equal("Все месяцы", options[0].Title);
        Assert.True(options[0].IsAll);
        Assert.Null(options[0].Month);

        Assert.Contains(options, o => o.Month == MonthPeriod.CurrentMonth);
        Assert.Contains(options, o => o.Month == new DateTime(2024, 5, 1));

        // Пункты месяцев идут от нового к старому, сразу после «Все месяцы».
        var months = options.Skip(1).Select(o => o.Month!.Value).ToList();
        Assert.Equal(months.OrderByDescending(m => m), months);
    }

    [Fact]
    public void Title_UsesRussianMonthName()
    {
        Assert.Equal("Ноябрь 2026", MonthPeriod.Title(new DateTime(2026, 11, 15)));
        Assert.Equal("Май 2025", MonthPeriod.Title(new DateTime(2025, 5, 1)));
        Assert.Equal("Ноя 2026", MonthPeriod.ShortTitle(new DateTime(2026, 11, 15)));
    }
}
