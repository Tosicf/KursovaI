namespace StroyControl.Core.Entities;

/// <summary>Позиция сметы: плановое количество и цена материала.</summary>
public class EstimateItem : EntityBase
{
    public int EstimateId { get; set; }

    public Estimate? Estimate { get; set; }

    public int MaterialId { get; set; }

    public Material? Material { get; set; }

    /// <summary>Раздел сметы (например, «Фундамент», «Кровля»).</summary>
    public string Section { get; set; } = string.Empty;

    public decimal PlannedQuantity { get; set; }

    public decimal PlannedUnitPrice { get; set; }

    public string? Note { get; set; }

    public decimal PlannedCost => PlannedQuantity * PlannedUnitPrice;

    public string MaterialName => Material?.Name ?? "—";

    public string UnitDisplay => Material?.Unit.ToDisplayName() ?? string.Empty;

    public string SectionDisplay => string.IsNullOrWhiteSpace(Section) ? "Без раздела" : Section;

    public override string ToString() => $"{MaterialName}: {PlannedQuantity} × {PlannedUnitPrice:N2}";
}
