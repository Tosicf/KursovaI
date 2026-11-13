using System;
using System.Linq;
using StroyControl.Core.Services;
using Xunit;

namespace StroyControl.Core.Tests;

public sealed class DeliveryNumbersTests
{
    private static readonly DateTime Day = new(2026, 11, 13);

    [Fact]
    public void Next_WithoutDocuments_StartsFromFirst()
    {
        Assert.Equal("ПН-26-1113-001", DeliveryNumbers.Next(Array.Empty<string>(), Day));
    }

    [Fact]
    public void Next_TakesMaximumSequencePlusOne()
    {
        var numbers = new[] { "ПН-26-1113-001", "ПН-26-1113-007", "ПН-26-1113-003" };

        Assert.Equal("ПН-26-1113-008", DeliveryNumbers.Next(numbers, Day));
    }

    [Fact]
    public void Next_IgnoresOtherDaysAndYear()
    {
        var numbers = new[]
        {
            "ПН-26-1112-099",   // предыдущий день
            "ПН-25-1113-042",   // прошлый год
            "ПН-26-1213-005",   // другой день того же месяца
            "ПН-26-1113-002"
        };

        Assert.Equal("ПН-26-1113-003", DeliveryNumbers.Next(numbers, Day));
    }

    [Fact]
    public void Next_IgnoresLegacyHourBasedNumbers()
    {
        // Старый формат «ПН-26-111318»: час вместо последовательности.
        var numbers = new[] { "ПН-26-111318", "ПН-26-111318" };

        Assert.Equal("ПН-26-1113-001", DeliveryNumbers.Next(numbers, Day));
    }

    [Fact]
    public void Next_HandlesCopySuffixesAndSpaces()
    {
        var numbers = new[] { " ПН-26-1113-004-2 ", "ПН-26-1113-005" };

        Assert.Equal("ПН-26-1113-006", DeliveryNumbers.Next(numbers, Day));
    }

    [Fact]
    public void Next_SkipsBlankAndForeignNumbers()
    {
        var numbers = new[] { "", "   ", "АКТ-001", "ПН-26-1113-010" };

        Assert.Equal("ПН-26-1113-011", DeliveryNumbers.Next(numbers, Day));
    }

    [Fact]
    public void Next_PadsSequenceToThreeDigits()
    {
        var numbers = Enumerable.Range(1, 9).Select(i => $"ПН-26-1113-{i:000}").ToList();

        Assert.Equal("ПН-26-1113-010", DeliveryNumbers.Next(numbers, Day));
    }

    [Fact]
    public void Next_ContinuesBeyondThreeDigits()
    {
        var numbers = new[] { "ПН-26-1113-999" };

        Assert.Equal("ПН-26-1113-1000", DeliveryNumbers.Next(numbers, Day));
    }

    [Fact]
    public void PrefixFor_UsesTwoDigitYearAndMonthDay()
    {
        Assert.Equal("ПН-26-0105-", DeliveryNumbers.PrefixFor(new DateTime(2026, 1, 5)));
    }

    [Fact]
    public void Next_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => DeliveryNumbers.Next(null!, Day));
    }
}
