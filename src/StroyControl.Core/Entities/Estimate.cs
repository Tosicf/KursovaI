using StroyControl.Core.Enums;
using StroyControl.Core.Services;

namespace StroyControl.Core.Entities;

/// <summary>Смета на строительный объект (несколько смет на один объект).</summary>
public class Estimate : EntityBase
{
    public string Number { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int ProjectId { get; set; }

    public Project? Project { get; set; }

    public EstimateStatus Status { get; set; } = EstimateStatus.Draft;

    /// <summary>
    /// Дата сметы (месяц, к которому относится документ). Именно по ней сметы
    /// группируются и сортируются во всех списках приложения.
    /// </summary>
    public DateTime Date { get; set; } = DateTime.Today;

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

    /// <summary>
    /// Смета изменяема только в состоянии «Черновик». После подтверждения состав
    /// и суммы фиксируются, поэтому перерасход нельзя «подогнать» под факт.
    /// </summary>
    public bool IsEditable => Status == EstimateStatus.Draft;

    /// <summary>Смета подтверждена или отправлена в архив — правки запрещены.</summary>
    public bool IsFixed => Status != EstimateStatus.Draft;

    /// <summary>Смета в работе: план утверждён и сравнивается с фактическими затратами.</summary>
    public bool IsInWork => Status == EstimateStatus.InWork;

    /// <summary>Смета снята с учёта и показывается только во вкладке «Архив».</summary>
    public bool IsArchived => Status == EstimateStatus.Archived;

    /// <summary>Подсказка для интерфейса о текущем режиме работы со сметой.</summary>
    public string LockDisplay => Status switch
    {
        EstimateStatus.Draft => "Черновик: состав и суммы можно изменять",
        EstimateStatus.OnReview => "Смета на проверке: изменения запрещены",
        EstimateStatus.Approved => "Смета утверждена: нажмите «В работу», чтобы сравнивать план с фактом",
        EstimateStatus.InWork => "Смета в работе: план зафиксирован, факт собирается по актам списания",
        EstimateStatus.Archived => "Смета в архиве: только просмотр и отчёты",
        _ => string.Empty
    };

    public string ProjectName => Project?.Name ?? "—";

    public string DateDisplay => Date.ToString("dd.MM.yyyy");

    /// <summary>Месяц сметы для группировки списков: «Ноябрь 2026».</summary>
    public string MonthDisplay => MonthPeriod.Title(Date);

    public override string ToString() => $"{Number} · {Name}";
}
