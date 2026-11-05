using CommunityToolkit.Mvvm.ComponentModel;
using StroyControl.Core.Entities;

namespace StroyControl.App.ViewModels.Editors;

public sealed partial class EstimateItemEditorViewModel : EditorViewModelBase
{
    private readonly EstimateItem? _source;

    public EstimateItemEditorViewModel(EstimateItem? source, int estimateId, IReadOnlyList<Material> materials)
    {
        _source = source;
        EstimateId = estimateId;
        Materials = materials;

        SelectedMaterial = materials.FirstOrDefault(m => m.Id == source?.MaterialId) ?? materials.FirstOrDefault();
        Section = source?.Section ?? "Общестроительные работы";
        PlannedQuantity = source?.PlannedQuantity ?? 0m;
        PlannedUnitPrice = source?.PlannedUnitPrice ?? source?.Material?.Price ?? SelectedMaterial?.Price ?? 0m;
        Note = source?.Note;
    }

    public override string Title => _source is null ? "Новая позиция сметы" : "Редактирование позиции сметы";

    public int EstimateId { get; }

    public IReadOnlyList<Material> Materials { get; }

    [ObservableProperty]
    private Material? selectedMaterial;

    [ObservableProperty]
    private string section;

    [ObservableProperty]
    private decimal plannedQuantity;

    [ObservableProperty]
    private decimal plannedUnitPrice;

    [ObservableProperty]
    private string? note;

    public string UnitDisplay => SelectedMaterial?.UnitDisplay ?? string.Empty;

    public string StockHint => SelectedMaterial is null
        ? string.Empty
        : $"Остаток на складе: {SelectedMaterial.StockDisplay}";

    public decimal PlannedCost => PlannedQuantity * PlannedUnitPrice;

    partial void OnSelectedMaterialChanged(Material? value)
    {
        if (value is not null && PlannedUnitPrice == 0m)
            PlannedUnitPrice = value.Price;

        OnPropertyChanged(nameof(UnitDisplay));
        OnPropertyChanged(nameof(StockHint));
    }

    partial void OnPlannedQuantityChanged(decimal value) => OnPropertyChanged(nameof(PlannedCost));

    partial void OnPlannedUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(PlannedCost));

    protected override string? Validate()
    {
        if (SelectedMaterial is null) return "Выберите материал.";
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
        MaterialId = SelectedMaterial?.Id ?? 0,
        Material = SelectedMaterial,
        Section = (Section ?? string.Empty).Trim(),
        PlannedQuantity = PlannedQuantity,
        PlannedUnitPrice = PlannedUnitPrice,
        Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()
    };
}
