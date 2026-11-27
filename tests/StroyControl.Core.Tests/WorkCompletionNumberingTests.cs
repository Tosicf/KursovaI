using StroyControl.Core.Services;
using Xunit;

namespace StroyControl.Core.Tests;

/// <summary>
/// Нумерация актов выполненных работ: номер уникален в базе, поэтому следующий
/// документ получает первое свободное значение, а не шаблон по дате.
/// </summary>
public class WorkCompletionNumberingTests
{
    private static readonly DateTime Date = new(2026, 11, 5);

    [Fact]
    public void Next_WithoutNumbers_StartsFromFirst()
    {
        Assert.Equal("АВР-26-0001", WorkCompletionNumbering.Next(Array.Empty<string>(), Date));
    }

    [Fact]
    public void Next_AfterExistingNumbers_ContinuesSequence()
    {
        var used = new[] { "АВР-26-0001", "АВР-26-0002" };

        Assert.Equal("АВР-26-0003", WorkCompletionNumbering.Next(used, Date));
    }

    [Fact]
    public void Next_IgnoresOtherYears()
    {
        var used = new[] { "АВР-25-0007", "АВР-24-0012" };

        Assert.Equal("АВР-26-0001", WorkCompletionNumbering.Next(used, Date));
    }

    [Fact]
    public void Next_SkipsNumberTakenOutOfOrder()
    {
        // В базе мог остаться только «третий» номер — следующий всё равно свободен.
        var used = new[] { "АВР-26-0003" };

        Assert.Equal("АВР-26-0004", WorkCompletionNumbering.Next(used, Date));
    }

    [Fact]
    public void Next_ConsidersTrimmedAndCaseInsensitiveNumbers()
    {
        var used = new[] { " авр-26-0001 ", "АВР-26-0002" };

        Assert.Equal("АВР-26-0003", WorkCompletionNumbering.Next(used, Date));
    }

    [Fact]
    public void LastSequence_ReturnsZeroWhenNumberMissing()
    {
        Assert.Equal(0, WorkCompletionNumbering.LastSequence(Array.Empty<string>(), Date));
        Assert.Equal(2, WorkCompletionNumbering.LastSequence(new[] { "АВР-26-0002" }, Date));
    }

    [Fact]
    public void Format_UsesYearSuffixAndFourDigits()
    {
        Assert.Equal("АВР-26-0001", WorkCompletionNumbering.Format(Date, 1));
        Assert.Equal("АВР-26-0042", WorkCompletionNumbering.Format(Date, 42));
    }

    [Fact]
    public void IsActNumber_ChecksFormat()
    {
        Assert.True(WorkCompletionNumbering.IsActNumber("АВР-26-0001"));
        Assert.False(WorkCompletionNumbering.IsActNumber("АВР-26-1"));
        Assert.False(WorkCompletionNumbering.IsActNumber("АР-26-0001"));
        Assert.False(WorkCompletionNumbering.IsActNumber(" "));
    }
}
