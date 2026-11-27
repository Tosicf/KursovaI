using StroyControl.Core.Enums;

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

    /// <summary>Позиция сметы (материал), к которой отнесён расход: нужно для плана/факта по строкам.</summary>
    public int? EstimateItemId { get; set; }

    public EstimateItem? EstimateItem { get; set; }

    /// <summary>Акт выполненных работ, в котором материал израсходован (если списание сделано из акта).</summary>
    public int? WorkCompletionId { get; set; }

    public WorkCompletion? WorkCompletion { get; set; }

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

    /// <summary>Списание сделано строкой акта выполненных работ.</summary>
    public bool IsFromWorkCompletion => WorkCompletionId is not null;

    public string ProjectName => Project?.Name ?? "—";

    public string UnitDisplay => Material?.Unit.ToDisplayName() ?? string.Empty;

    public string DateDisplay => Date.ToString("dd.MM.yyyy");

    public string AuthorName => CreatedBy?.DisplayName ?? "—";
}
