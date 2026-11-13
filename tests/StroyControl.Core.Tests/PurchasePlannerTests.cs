using System;
using System.Linq;
using StroyControl.Core.Dtos;
using StroyControl.Core.Entities;
using Xunit;

namespace StroyControl.Core.Tests;

public sealed class PurchasePlannerTests
{
    private static Material CreateMaterial(
        string name, decimal stock, decimal minStock, bool isActive = true, decimal price = 100m) => new()
    {
        Code = name.Length > 3 ? name[..3].ToUpperInvariant() : name.ToUpperInvariant(),
        Name = name,
        Category = "Прочее",
        Price = price,
        StockQuantity = stock,
        MinStock = minStock,
        IsActive = isActive
    };

    [Fact]
    public void Deficit_IncludesMaterialsAtOrBelowMinimum()
    {
        var materials = new[]
        {
            CreateMaterial("Пена монтажная", 82.5m, 150m),
            CreateMaterial("Кабель ВВГнг", 1100m, 2000m),
            CreateMaterial("Гипсокартон", 500m, 200m),
            CreateMaterial("Песок", 10m, 10m)
        };

        var deficit = PurchasePlanner.Deficit(materials);

        Assert.Equal(3, deficit.Count);
        Assert.DoesNotContain(deficit, m => m.Name == "Гипсокартон");
        Assert.Contains(deficit, m => m.Name == "Песок"); // равенство минимуму — уже дефицит
    }

    [Fact]
    public void Deficit_SkipsInactiveMaterials()
    {
        var materials = new[]
        {
            CreateMaterial("Активный", 1m, 10m),
            CreateMaterial("Снятый с учёта", 1m, 10m, isActive: false)
        };

        var deficit = PurchasePlanner.Deficit(materials);

        var only = Assert.Single(deficit);
        Assert.Equal("Активный", only.Name);
    }

    [Fact]
    public void Deficit_OrdersByShortageThenByName()
    {
        var materials = new[]
        {
            CreateMaterial("Верхний", 90m, 100m),   // -10
            CreateMaterial("Нижний Я", 0m, 100m),   // -100
            CreateMaterial("Нижний А", 0m, 100m)    // -100, но имя раньше
        };

        var deficit = PurchasePlanner.Deficit(materials);

        Assert.Equal(new[] { "Нижний А", "Нижний Я", "Верхний" }, deficit.Select(m => m.Name));
    }

    [Fact]
    public void Deficit_RespectsLimit()
    {
        var materials = Enumerable.Range(0, 10)
            .Select(i => CreateMaterial("Материал " + i, i, 100m))
            .ToArray();

        var deficit = PurchasePlanner.Deficit(materials, limit: 4);

        Assert.Equal(4, deficit.Count);
    }

    [Fact]
    public void Deficit_RejectsNull()
    {
        Assert.Throws<ArgumentNullException>(() => PurchasePlanner.Deficit(null!));
    }

    [Fact]
    public void SuggestedQuantity_BringsStockUpToDoubleMinimum()
    {
        var material = CreateMaterial("Пена монтажная", stock: 82.5m, minStock: 150m);

        // 2 × 150 − 82,5 = 217,5
        Assert.Equal(217.5m, PurchasePlanner.SuggestedQuantity(material));
    }

    [Fact]
    public void SuggestedQuantity_EmptyStockEqualsDoubleMinimum()
    {
        var material = CreateMaterial("Кабель", stock: 0m, minStock: 2000m);

        Assert.Equal(4000m, PurchasePlanner.SuggestedQuantity(material));
    }

    [Fact]
    public void SuggestedQuantity_NeverBelowOne()
    {
        // Остаток выше минимума: докупать нечего, но строка должна быть непустой.
        var material = CreateMaterial("Запас", stock: 500m, minStock: 100m);

        Assert.Equal(1m, PurchasePlanner.SuggestedQuantity(material));
    }

    [Fact]
    public void SuggestedQuantity_RoundsUpToThousandths()
    {
        var material = CreateMaterial("Провод", stock: 0.3335m, minStock: 1m);

        // 2 × 1 − 0,3335 = 1,6665 → 1,667
        Assert.Equal(1.667m, PurchasePlanner.SuggestedQuantity(material));
    }

    [Fact]
    public void SuggestedCost_MultipliesPriceByQuantity()
    {
        var material = CreateMaterial("Пена монтажная", stock: 82.5m, minStock: 150m, price: 12.4m);

        // 217,5 × 12,4 = 2697,00
        Assert.Equal(2697.00m, PurchasePlanner.SuggestedCost(material));
    }

    [Fact]
    public void Deficit_OfEmptyListIsEmpty()
    {
        Assert.Empty(PurchasePlanner.Deficit(Array.Empty<Material>()));
    }
}
