namespace StroyControl.Core.Entities;

/// <summary>Поставщик строительных материалов.</summary>
public class Supplier : EntityBase
{
    public string Name { get; set; } = string.Empty;

    public string Unp { get; set; } = string.Empty;

    public string ContactPerson { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    /// <summary>Надёжность поставщика по внутренней шкале 1–5.</summary>
    public int Rating { get; set; } = 5;

    public string? Note { get; set; }

    public ICollection<Material> Materials { get; set; } = new List<Material>();

    public int MaterialsCount => Materials?.Count ?? 0;

    public string RatingDisplay => $"{Rating}/5";

    public override string ToString() => Name;
}
