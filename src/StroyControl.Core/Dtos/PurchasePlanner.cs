using StroyControl.Core.Entities;

namespace StroyControl.Core.Dtos;

/// <summary>
/// Планирование закупки по дефициту: какие материалы ниже минимума и сколько их
/// дозаказать, чтобы вышел запас до двух минимумов. Логика без обращений к БД,
/// поэтому покрыта модульными тестами.
/// </summary>
public static class PurchasePlanner
{
    /// <summary>Сколько заказывать относительно минимума: два минимума на складе.</summary>
    public const decimal TargetFactor = 2m;

    /// <summary>Материалы, которых не хватает до минимума, начиная с самого дефицитного.</summary>
    public static IReadOnlyList<Material> Deficit(IEnumerable<Material> materials, int limit = 100)
    {
        ArgumentNullException.ThrowIfNull(materials);

        return materials
            .Where(m => m.IsActive && m.StockQuantity <= m.MinStock)
            .OrderBy(m => m.StockQuantity - m.MinStock)
            .ThenBy(m => m.Name, StringComparer.CurrentCulture)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Рекомендуемое количество к закупке: до двух минимумов, но не меньше, чем нужно
    /// для выхода на минимум. Округление — до тысячных, как количество в документах.
    /// </summary>
    public static decimal SuggestedQuantity(Material material)
    {
        ArgumentNullException.ThrowIfNull(material);

        var quantity = material.MinStock * TargetFactor - material.StockQuantity;

        if (quantity <= 0m)
            quantity = material.MinStock - material.StockQuantity;

        if (quantity <= 0m)
            quantity = 1m;

        return Math.Round(quantity, 3, MidpointRounding.AwayFromZero);
    }

    /// <summary>Сумма рекомендуемой закупки по цене справочника.</summary>
    public static decimal SuggestedCost(Material material) =>
        Math.Round(SuggestedQuantity(material) * material.Price, 2);
}
