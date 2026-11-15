using StroyControl.Core.Dtos;
using Xunit;

namespace StroyControl.Core.Tests;

public sealed class MinStockCalculatorTests
{
    [Theory]
    [InlineData(100, 40, 60)]       // нужно 100, на складе 40 → не хватает 60
    [InlineData(100, 0, 100)]       // склада нет — минимум равен потребности
    [InlineData(100, 100, 10)]      // потребность покрыта, но резерв 10 % остаётся
    [InlineData(100, 250, 10)]      // запас больше потребности — резерв 10 %
    [InlineData(200, 250, 20)]      // количество в смете выросло → минимум тоже
    [InlineData(0, 30, 0)]          // материал вне смет — значение не навязываем
    [InlineData(12.3456, 0, 12.346)] // округление до тысячных, как в документах
    public void SuggestedMinStock_ReturnsRemainingNeedOrReserve(decimal need, decimal stock, decimal expected)
    {
        Assert.Equal(expected, MinStockCalculator.SuggestedMinStock(need, stock));
    }

    [Fact]
    public void Suggestion_ReportsChangesAndDelta()
    {
        var suggestion = new MinStockSuggestion(1, "ЦЕМ-500", "Цемент ПЦ 500-Д0", "кг", 40m, 100m, 20m, 60m);

        Assert.True(suggestion.HasChanges);
        Assert.Equal(40m, suggestion.Delta);

        var unchanged = suggestion with { CurrentMinStock = 60m };
        Assert.False(unchanged.HasChanges);
        Assert.Equal(0m, unchanged.Delta);
    }
}
