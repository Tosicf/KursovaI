namespace StroyControl.Core.Entities;

/// <summary>
/// Акт выполненных работ по смете. Документ фиксирует фактически выполненные работы
/// (объём и расценка по позициям-работам сметы) и материалы, израсходованные на эти
/// работы: строки материалов уменьшают остаток склада и попадают в фактический расход
/// как обычные списания. Учитывать факт можно только по смете в состоянии «В работе».
/// </summary>
public class WorkCompletion : EntityBase
{
    public int EstimateId { get; set; }

    public Estimate? Estimate { get; set; }

    public int ProjectId { get; set; }

    public Project? Project { get; set; }

    /// <summary>Дата акта: по ней документ попадает в месяц и в анализ отклонений.</summary>
    public DateTime Date { get; set; } = DateTime.Today;

    public string DocumentNumber { get; set; } = string.Empty;

    public string? Note { get; set; }

    public int? CreatedByUserId { get; set; }

    public User? CreatedBy { get; set; }

    /// <summary>Строки выполненных работ (позиции-работы сметы).</summary>
    public ICollection<WorkCompletionItem> Items { get; set; } = new List<WorkCompletionItem>();

    /// <summary>Материалы, израсходованные на работы: это же движение склада.</summary>
    public ICollection<MaterialUsage> MaterialUsages { get; set; } = new List<MaterialUsage>();

    public decimal WorksCost => Items?.Sum(i => i.Cost) ?? 0m;

    public decimal MaterialsCost => MaterialUsages?.Sum(u => u.Cost) ?? 0m;

    /// <summary>Полная стоимость акта: работы плюс израсходованные материалы.</summary>
    public decimal Total => WorksCost + MaterialsCost;

    public int ItemsCount => Items?.Count ?? 0;

    public int MaterialsCount => MaterialUsages?.Count ?? 0;

    public bool HasMaterials => MaterialsCount > 0;

    public bool HasWorks => ItemsCount > 0;

    public string DateDisplay => Date.ToString("dd.MM.yyyy");

    public string ProjectName => Project?.Name ?? "—";

    public string EstimateNumber => Estimate?.Number ?? "—";

    public string AuthorName => CreatedBy?.DisplayName ?? "—";

    public string TotalDisplay => Total.ToString("N2") + " BYN";

    public override string ToString() => $"{DocumentNumber} от {DateDisplay}: {Total:N2} BYN";
}
