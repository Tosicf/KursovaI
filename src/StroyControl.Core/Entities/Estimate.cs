using StroyControl.Core.Enums;

namespace StroyControl.Core.Entities;

/// <summary>Смета на строительный объект (несколько смет на один объект).</summary>
public class Estimate : EntityBase
{
    public string Number { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int ProjectId { get; set; }

    public Project? Project { get; set; }

    public EstimateStatus Status { get; set; } = EstimateStatus.Draft;

    public string? Note { get; set; }

    public ICollection<EstimateItem> Items { get; set; } = new List<EstimateItem>();

    public decimal PlannedTotal => Items?.Sum(i => i.PlannedCost) ?? 0m;

    public int ItemsCount => Items?.Count ?? 0;

    public string StatusDisplay => Status.ToDisplayName();

    public string ProjectName => Project?.Name ?? "—";

    public override string ToString() => $"{Number} · {Name}";
}
