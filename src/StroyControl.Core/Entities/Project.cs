using StroyControl.Core.Enums;

namespace StroyControl.Core.Entities;

/// <summary>Строительный объект (объект строительства).</summary>
public class Project : EntityBase
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Customer { get; set; } = string.Empty;

    public string ManagerName { get; set; } = string.Empty;

    /// <summary>Договорная стоимость строительства, BYN.</summary>
    public decimal ContractSum { get; set; }

    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime PlannedEndDate { get; set; } = DateTime.Today.AddMonths(12);

    public ProjectStatus Status { get; set; } = ProjectStatus.InProgress;

    public int ProgressPercent { get; set; }

    public string? Note { get; set; }

    public ICollection<Estimate> Estimates { get; set; } = new List<Estimate>();

    public ICollection<MaterialUsage> Usages { get; set; } = new List<MaterialUsage>();

    public string StatusDisplay => Status.ToDisplayName();

    public string AddressDisplay => string.IsNullOrWhiteSpace(Address) ? "адрес не указан" : Address;

    public string PeriodDisplay => $"{StartDate:dd.MM.yyyy} — {PlannedEndDate:dd.MM.yyyy}";

    public override string ToString() => $"{Code} · {Name}";
}
