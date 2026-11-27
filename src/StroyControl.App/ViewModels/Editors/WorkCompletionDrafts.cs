using CommunityToolkit.Mvvm.ComponentModel;
using StroyControl.Core.Entities;

namespace StroyControl.App.ViewModels.Editors;

/// <summary>
/// Редактируемая строка выполненных работ (до сохранения акта). Ссылается на
/// позицию-работу сметы, поэтому объём всегда сопоставим с планом.
/// </summary>
public sealed partial class WorkCompletionWorkDraft : ObservableObject
{
    public WorkCompletionWorkDraft(EstimateItem? item = null, decimal quantity = 0m, decimal unitPrice = 0m)
    {
        // Поля, сгенерированные из [ObservableProperty], заполняются напрямую: в конструкторе
        // подписчиков ещё нет, а имена параметров совпадают с именами полей.
        this.estimateItem = item;
        this.quantity = quantity > 0m ? quantity : item?.PlannedQuantity ?? 0m;
        this.unitPrice = unitPrice > 0m ? unitPrice : item?.PlannedUnitPrice ?? 0m;
    }

    [ObservableProperty]
    private EstimateItem? estimateItem;

    [ObservableProperty]
    private decimal quantity;

    [ObservableProperty]
    private decimal unitPrice;

    public decimal Cost => Math.Round(Quantity * UnitPrice, 2);

    /// <summary>Плановый объём по смете — для подсказки «осталось выполнить».</summary>
    public decimal PlannedQuantity => EstimateItem?.PlannedQuantity ?? 0m;

    public string NameDisplay => EstimateItem?.NameDisplay ?? "—";

    public string UnitDisplay => EstimateItem?.UnitDisplay ?? string.Empty;

    public string PlannedHint => $"План: {PlannedQuantity:N2} {UnitDisplay}".TrimEnd();

    public bool IsOverPlanned => PlannedQuantity > 0m && Quantity > PlannedQuantity;

    partial void OnQuantityChanged(decimal value)
    {
        OnPropertyChanged(nameof(Cost));
        OnPropertyChanged(nameof(IsOverPlanned));
    }

    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(Cost));

    partial void OnEstimateItemChanged(EstimateItem? value)
    {
        if (value is not null)
        {
            UnitPrice = value.PlannedUnitPrice;
            if (Quantity <= 0m) Quantity = value.PlannedQuantity;
        }

        OnPropertyChanged(nameof(NameDisplay));
        OnPropertyChanged(nameof(UnitDisplay));
        OnPropertyChanged(nameof(PlannedQuantity));
        OnPropertyChanged(nameof(PlannedHint));
        OnPropertyChanged(nameof(IsOverPlanned));
        OnPropertyChanged(nameof(Cost));
    }
}

/// <summary>
/// Редактируемая строка материалов акта. Материал берётся из позиции сметы (тогда
/// расход сопоставляется с планом) либо из справочника — на непредвиденные нужды.
/// При сохранении акта строка уменьшает остаток склада.
/// </summary>
public sealed partial class WorkCompletionMaterialDraft : ObservableObject
{
    public WorkCompletionMaterialDraft(
        EstimateItem? materialItem = null,
        Material? material = null,
        decimal quantity = 0m,
        decimal unitPrice = 0m,
        decimal alreadyUsed = 0m)
    {
        this.materialItem = materialItem;
        this.material = material ?? materialItem?.Material;
        this.quantity = quantity > 0m ? quantity : materialItem?.PlannedQuantity ?? 0m;
        this.unitPrice = unitPrice > 0m ? unitPrice : this.material?.Price ?? materialItem?.PlannedUnitPrice ?? 0m;
        _alreadyUsed = alreadyUsed;
    }

    [ObservableProperty]
    private EstimateItem? materialItem;

    [ObservableProperty]
    private Material? material;

    [ObservableProperty]
    private decimal quantity;

    [ObservableProperty]
    private decimal unitPrice;

    private decimal _alreadyUsed;

    public decimal Cost => Math.Round(Quantity * UnitPrice, 2);

    /// <summary>Остаток на складе с учётом количества прошлой редакции акта.</summary>
    public decimal AvailableQuantity => (Material?.StockQuantity ?? 0m) + _alreadyUsed;

    public string NameDisplay => Material?.Name ?? MaterialItem?.NameDisplay ?? "—";

    public string UnitDisplay => Material?.UnitDisplay ?? MaterialItem?.UnitDisplay ?? string.Empty;

    public string StockHint => Material is null
        ? string.Empty
        : $"Остаток: {AvailableQuantity:N2} {UnitDisplay}".TrimEnd();

    public bool IsOverStock => Material is not null && Quantity > AvailableQuantity;

    public decimal PlannedQuantity => MaterialItem?.PlannedQuantity ?? 0m;

    partial void OnQuantityChanged(decimal value)
    {
        OnPropertyChanged(nameof(Cost));
        OnPropertyChanged(nameof(IsOverStock));
    }

    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(Cost));

    partial void OnMaterialItemChanged(EstimateItem? value)
    {
        if (value?.Material is not null)
        {
            Material = value.Material;
            UnitPrice = value.Material.Price;
        }

        OnPropertyChanged(nameof(PlannedQuantity));
        OnPropertyChanged(nameof(NameDisplay));
        OnPropertyChanged(nameof(UnitDisplay));
    }

    partial void OnMaterialChanged(Material? value)
    {
        if (value is not null && UnitPrice <= 0m)
            UnitPrice = value.Price;

        OnPropertyChanged(nameof(NameDisplay));
        OnPropertyChanged(nameof(UnitDisplay));
        OnPropertyChanged(nameof(AvailableQuantity));
        OnPropertyChanged(nameof(StockHint));
        OnPropertyChanged(nameof(IsOverStock));
    }
}
