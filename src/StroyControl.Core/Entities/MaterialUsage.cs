namespace StroyControl.Core.Entities;

/// <summary>Фактическое списание материала на объект (акт расхода).</summary>
public class MaterialUsage : EntityBase
{
    public int ProjectId { get; set; }

    public Project? Project { get; set; }

    public int MaterialId { get; set; }

    public Material? Material { get; set; }

    public int? EstimateId { get; set; }

    public Estimate? Estimate { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>Цена на момент списания (снимок учётной цены).</summary>
    public decimal UnitPrice { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    public string DocumentNumber { get; set; } = string.Empty;

    public string? Note { get; set; }

    public int? CreatedByUserId { get; set; }

    public User? CreatedBy { get; set; }

    public decimal Cost => Quantity * UnitPrice;

    public string MaterialName => Material?.Name ?? "—";

    public string ProjectName => Project?.Name ?? "—";

    public string UnitDisplay => Material?.Unit.ToDisplayName() ?? string.Empty;

    public string DateDisplay => Date.ToString("dd.MM.yyyy");

    public string AuthorName => CreatedBy?.DisplayName ?? "—";
}
