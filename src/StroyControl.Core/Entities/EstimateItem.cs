using StroyControl.Core.Enums;

namespace StroyControl.Core.Entities;

/// <summary>
/// Позиция сметы: материал со склада либо работа (армирование, кладка, монтаж).
/// Для материала заполняется <see cref="MaterialId"/>, для работы — <see cref="WorkName"/>
/// и <see cref="WorkUnit"/>; цена и количество задаются в обоих случаях.
/// </summary>
public class EstimateItem : EntityBase
{
    public int EstimateId { get; set; }

    public Estimate? Estimate { get; set; }

    /// <summary>Материал справочника; для работ не заполняется.</summary>
    public int? MaterialId { get; set; }

    public Material? Material { get; set; }

    /// <summary>true — позиция описывает работу, false — материал.</summary>
    public bool IsWork { get; set; }

    /// <summary>Наименование работы (заполняется, когда <see cref="IsWork"/> = true).</summary>
    public string? WorkName { get; set; }

    /// <summary>Единица измерения работы: м2, м.п., шт и т. п.</summary>
    public string WorkUnit { get; set; } = string.Empty;

    /// <summary>Раздел сметы (например, «Фундамент», «Кровля»).</summary>
    public string Section { get; set; } = string.Empty;

    public decimal PlannedQuantity { get; set; }

    public decimal PlannedUnitPrice { get; set; }

    public string? Note { get; set; }

    public decimal PlannedCost => PlannedQuantity * PlannedUnitPrice;

    /// <summary>Наименование позиции для таблиц: материал или работа.</summary>
    public string NameDisplay => IsWork
        ? (string.IsNullOrWhiteSpace(WorkName) ? "Работа без названия" : WorkName!)
        : Material?.Name ?? "—";

    /// <summary>Совместимое имя: используется в таблицах смет и в анализе отклонений.</summary>
    public string MaterialName => NameDisplay;

    public string UnitDisplay => IsWork
        ? (string.IsNullOrWhiteSpace(WorkUnit) ? "раб." : WorkUnit)
        : Material?.Unit.ToDisplayName() ?? string.Empty;

    public string SectionDisplay => string.IsNullOrWhiteSpace(Section) ? "Без раздела" : Section;

    public string KindDisplay => IsWork ? "Работа" : "Материал";

    public string AmountDisplay => PlannedCost.ToString("N2");

    public override string ToString() => $"{MaterialName}: {PlannedQuantity} × {PlannedUnitPrice:N2}";
}
