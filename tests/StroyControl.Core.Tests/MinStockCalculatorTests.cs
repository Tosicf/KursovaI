using StroyControl.Core.Dtos;
using Xunit;

namespace StroyControl.Core.Tests;

public sealed class MinStockCalculatorTests
{
    [Theory]
    [InlineData(100, 40, 60)]     // нужно 100, на складе 40 → держим минимум 60
    [InlineData(100, 0, 100)]     // склада нет — минимум равен потребности
    [InlineData(100, 100, 0)]     // потребность полностью покрыта остатком
    [InlineData(100, 250, 0)]     // запас больше потребности — минимум не нужен
    [InlineData(0, 30, 0)]        // материал вне смет — значение не навязываем
    [InlineData(12.3456, 0, 12.346)] // округление до тысячных, как в документах
    public void SuggestedMinStock_ReturnsRemainingNeed(decimal need, decimal stock, decimal expected)
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
