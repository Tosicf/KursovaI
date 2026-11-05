namespace StroyControl.Core.Entities;

/// <summary>Поступление материалов (приходная накладная).</summary>
public class Delivery : EntityBase
{
    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.Today;

    public int SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public int? ProjectId { get; set; }

    public Project? Project { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public string? Note { get; set; }

    public ICollection<DeliveryItem> Items { get; set; } = new List<DeliveryItem>();

    public decimal Total => Items?.Sum(i => i.Cost) ?? 0m;

    public int ItemsCount => Items?.Count ?? 0;

    public string SupplierName => Supplier?.Name ?? "—";

    public string ProjectName => Project?.Name ?? "общий склад";

    public string DateDisplay => Date.ToString("dd.MM.yyyy");

    public override string ToString() => $"{Number} от {DateDisplay}";
}
