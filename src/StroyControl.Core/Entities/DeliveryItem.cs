namespace StroyControl.Core.Entities;

/// <summary>Строка поступления: материал, количество и фактическая цена.</summary>
public class DeliveryItem : EntityBase
{
    public int DeliveryId { get; set; }

    public Delivery? Delivery { get; set; }

    public int MaterialId { get; set; }

    public Material? Material { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Cost => Quantity * UnitPrice;

    public string MaterialName => Material?.Name ?? "—";

    public string UnitDisplay => Material?.UnitDisplay ?? string.Empty;

    public string QuantityDisplay => $"{Quantity:N2} {UnitDisplay}";
}
