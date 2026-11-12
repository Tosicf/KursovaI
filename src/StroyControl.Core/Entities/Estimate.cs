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

    /// <summary>Наценка на единичную цену, % (закладывается в «цену с наценкой» отчёта).</summary>
    public decimal MarkupPercent { get; set; } = 10m;

    /// <summary>Прибыль в процентах от стоимости позиции.</summary>
    public decimal ProfitPercent { get; set; } = 12m;

    /// <summary>Доля зарплаты рабочих в стоимости работ, %.</summary>
    public decimal LaborSharePercent { get; set; } = 35m;

    public ICollection<EstimateItem> Items { get; set; } = new List<EstimateItem>();

    public decimal PlannedTotal => Items?.Sum(i => i.PlannedCost) ?? 0m;

    public int ItemsCount => Items?.Count ?? 0;

    public string StatusDisplay => Status.ToDisplayName();

    public string ProjectName => Project?.Name ?? "—";

    public override string ToString() => $"{Number} · {Name}";
}
