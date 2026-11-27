using StroyControl.Core.Entities;
using Xunit;

namespace StroyControl.Core.Tests;

/// <summary>
/// Акт выполненных работ: стоимость акта складывается из работ и списанных
/// материалов, а строки документа не теряются при пересчёте.
/// </summary>
public class WorkCompletionTests
{
    private static WorkCompletion CreateSample() => new()
    {
        Date = new DateTime(2025, 5, 12),
        DocumentNumber = "АВР-25-0001",
        Items =
        {
            new WorkCompletionItem { Quantity = 12m, UnitPrice = 45m },
            new WorkCompletionItem { Quantity = 3m, UnitPrice = 100.5m }
        },
        MaterialUsages =
        {
            new MaterialUsage { Quantity = 4.5m, UnitPrice = 20.25m },
            new MaterialUsage { Quantity = 2m, UnitPrice = 15m }
        }
    };

    [Fact]
    public void WorksCost_SumsWorkRows()
    {
        var completion = CreateSample();

        Assert.Equal(841.5m, completion.WorksCost);
        Assert.Equal(121.125m, completion.MaterialsCost);
    }

    [Fact]
    public void Total_IncludesWorksAndMaterials()
    {
        var completion = CreateSample();

        Assert.Equal(962.625m, completion.Total);
        Assert.True(completion.HasWorks);
        Assert.True(completion.HasMaterials);
        Assert.Equal(2, completion.ItemsCount);
        Assert.Equal(2, completion.MaterialsCount);
    }

    [Fact]
    public void Total_IsZero_WhenActHasNoRows()
    {
        var completion = new WorkCompletion { DocumentNumber = "АВР-25-0002" };

        Assert.Equal(0m, completion.Total);
        Assert.False(completion.HasWorks);
        Assert.False(completion.HasMaterials);
    }

    [Fact]
    public void DateDisplay_UsesDayMonthYear()
    {
        var completion = CreateSample();

        Assert.Equal("12.05.2025", completion.DateDisplay);
    }
}
