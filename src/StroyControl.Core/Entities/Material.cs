using StroyControl.Core.Enums;

namespace StroyControl.Core.Entities;

/// <summary>Строительный материал в справочнике материально-технического обеспечения.</summary>
public class Material : EntityBase
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public UnitOfMeasure Unit { get; set; } = UnitOfMeasure.Piece;

    /// <summary>Учётная цена за единицу, BYN.</summary>
    public decimal Price { get; set; }

    /// <summary>Текущий остаток на складе объекта.</summary>
    public decimal StockQuantity { get; set; }

    /// <summary>Минимальный (страховой) запас.</summary>
    public decimal MinStock { get; set; }

    public bool IsActive { get; set; } = true;

    public int? SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public string? Note { get; set; }

    public decimal StockValue => Price * StockQuantity;

    public bool IsLowStock => StockQuantity <= MinStock;

    public bool IsOutOfStock => StockQuantity <= 0;

    public string UnitDisplay => Unit.ToDisplayName();

    public string SupplierName => Supplier?.Name ?? "не указан";

    public string StockDisplay => $"{StockQuantity:N2} {UnitDisplay}";

    public string StockStatusDisplay => IsOutOfStock ? "Нет на складе" : IsLowStock ? "Ниже минимума" : "В наличии";

    public override string ToString() => Name;
}
