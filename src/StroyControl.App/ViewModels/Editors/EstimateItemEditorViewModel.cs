using CommunityToolkit.Mvvm.ComponentModel;
using StroyControl.Core.Entities;

namespace StroyControl.App.ViewModels.Editors;

/// <summary>
/// Позиция сметы: материал из справочника или работа. Для работы цена и единица
/// измерения вводятся вручную, для материала цена подставляется из базы при выборе.
/// </summary>
public sealed partial class EstimateItemEditorViewModel : EditorViewModelBase
{
    private readonly EstimateItem? _source;

    public EstimateItemEditorViewModel(EstimateItem? source, int estimateId, IReadOnlyList<Material> materials)
    {
        _source = source;
        EstimateId = estimateId;
        Materials = materials;

        IsWork = source?.IsWork ?? false;
        WorkName = source?.WorkName ?? string.Empty;
        WorkUnit = string.IsNullOrWhiteSpace(source?.WorkUnit) ? "м2" : source!.WorkUnit;
        SelectedMaterial = materials.FirstOrDefault(m => m.Id == source?.MaterialId) ?? materials.FirstOrDefault();
        Section = source?.Section ?? "Общестроительные работы";
        PlannedQuantity = source?.PlannedQuantity ?? 0m;
        PlannedUnitPrice = source?.PlannedUnitPrice ?? SelectedMaterial?.Price ?? 0m;
        Note = source?.Note;

        _initializing = false;
    }

    private bool _initializing = true;

    public override string Title => _source is null ? "Новая позиция сметы" : "Редактирование позиции сметы";

    public int EstimateId { get; }

    public IReadOnlyList<Material> Materials { get; }

    /// <summary>true — позиция описывает работу (цена и единица измерения вводятся вручную).</summary>
    [ObservableProperty]
    private bool isWork;

    [ObservableProperty]
    private Material? selectedMaterial;

    [ObservableProperty]
    private string workName;

    [ObservableProperty]
    private string workUnit;

    [ObservableProperty]
    private string section;

    [ObservableProperty]
    private decimal plannedQuantity;

    [ObservableProperty]
    private decimal plannedUnitPrice;

    [ObservableProperty]
    private string? note;

    public string UnitDisplay => IsWork
        ? (string.IsNullOrWhiteSpace(WorkUnit) ? "раб." : WorkUnit)
        : SelectedMaterial?.UnitDisplay ?? string.Empty;

    public string StockHint => IsWork
        ? "Работа не списывается со склада — остаток не учитывается."
        : SelectedMaterial is null
            ? string.Empty
            : $"Остаток на складе: {SelectedMaterial.StockDisplay}";

    public string CataloguePriceHint => IsWork
        ? string.Empty
        : PlannedUnitPrice > 0m
            ? $"Цена из справочника: {PlannedUnitPrice:N2} BYN за {UnitDisplay}"
            : string.Empty;

    public decimal PlannedCost => PlannedQuantity * PlannedUnitPrice;

    public string KindDisplay => IsWork ? "Работа" : "Материал";

    partial void OnIsWorkChanged(bool value)
    {
        // При переключении вида цены разных справочников несопоставимы — сбрасываем,
        // чтобы пользователь не сохранил работу с ценой материала.
        if (!_initializing)
        {
            PlannedUnitPrice = value ? 0m : SelectedMaterial?.Price ?? 0m;
        }

        NotifyComputed();
    }

    partial void OnSelectedMaterialChanged(Material? value)
    {
        // Главное исправление: цена подтягивается из базы при каждом выборе материала,
        // а не только когда поле цены пустое (раньше оставалась цена прошлой позиции).
        if (!_initializing && !IsWork && value is not null)
            PlannedUnitPrice = value.Price;

        NotifyComputed();
    }

    partial void OnWorkUnitChanged(string value) => NotifyComputed();

    partial void OnPlannedQuantityChanged(decimal value) => OnPropertyChanged(nameof(PlannedCost));

    partial void OnPlannedUnitPriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(PlannedCost));
        OnPropertyChanged(nameof(CataloguePriceHint));
    }

    private void NotifyComputed()
    {
        OnPropertyChanged(nameof(UnitDisplay));
        OnPropertyChanged(nameof(StockHint));
        OnPropertyChanged(nameof(CataloguePriceHint));
        OnPropertyChanged(nameof(KindDisplay));
    }

    protected override string? Validate()
    {
        if (IsWork)
        {
            if (string.IsNullOrWhiteSpace(WorkName)) return "Укажите наименование работы.";
            if (string.IsNullOrWhiteSpace(WorkUnit)) return "Укажите единицу измерения работы (м2, м.п., шт).";
        }
        else if (SelectedMaterial is null)
        {
            return "Выберите материал.";
        }

        if (PlannedQuantity <= 0m) return "Плановое количество должно быть больше нуля.";
        if (PlannedUnitPrice <= 0m) return "Плановая цена должна быть больше нуля.";
        return null;
    }

    public EstimateItem BuildEntity() => new()
    {
        Id = _source?.Id ?? 0,
        RowVersion = _source?.RowVersion ?? 0,
        CreatedAt = _source?.CreatedAt ?? DateTime.UtcNow,
        EstimateId = EstimateId,
        IsWork = IsWork,
        MaterialId = IsWork ? null : SelectedMaterial?.Id,
        Material = IsWork ? null : SelectedMaterial,
        WorkName = IsWork ? (WorkName ?? string.Empty).Trim() : null,
        WorkUnit = IsWork ? (WorkUnit ?? string.Empty).Trim() : string.Empty,
        Section = (Section ?? string.Empty).Trim(),
        PlannedQuantity = PlannedQuantity,
        PlannedUnitPrice = PlannedUnitPrice,
        Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()
    };
}
