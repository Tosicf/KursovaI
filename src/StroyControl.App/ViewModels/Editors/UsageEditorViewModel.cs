using CommunityToolkit.Mvvm.ComponentModel;
using StroyControl.Core.Entities;

namespace StroyControl.App.ViewModels.Editors;

public sealed partial class UsageEditorViewModel : EditorViewModelBase
{
    private readonly MaterialUsage? _source;
    private readonly decimal _originalQuantity;

    public UsageEditorViewModel(
        MaterialUsage? source,
        IReadOnlyList<Project> projects,
        IReadOnlyList<Material> materials,
        IReadOnlyList<Estimate> estimates,
        int? currentUserId)
    {
        _source = source;
        Projects = projects;
        Materials = materials;
        Estimates = estimates;
        CurrentUserId = currentUserId;
        _originalQuantity = source?.Quantity ?? 0m;

        SelectedProject = projects.FirstOrDefault(p => p.Id == source?.ProjectId) ?? projects.FirstOrDefault();
        SelectedMaterial = materials.FirstOrDefault(m => m.Id == source?.MaterialId) ?? materials.FirstOrDefault();
        SelectedEstimate = estimates.FirstOrDefault(e => e.Id == source?.EstimateId);
        Quantity = source?.Quantity ?? 0m;
        UnitPrice = source?.UnitPrice ?? SelectedMaterial?.Price ?? 0m;
        Date = source?.Date ?? DateTime.Today;
        DocumentNumber = source?.DocumentNumber ?? $"АР-{DateTime.Now:yy}-{DateTime.Now:MMddHH}";
        Note = source?.Note;
    }

    public override string Title => _source is null ? "Списание материала" : "Редактирование списания";

    public override double ContentMaxHeight => 520;

    public IReadOnlyList<Project> Projects { get; }

    public IReadOnlyList<Material> Materials { get; }

    public IReadOnlyList<Estimate> Estimates { get; }

    public int? CurrentUserId { get; }

    public IReadOnlyList<Estimate> FilteredEstimates =>
        SelectedProject is null
            ? Array.Empty<Estimate>()
            : Estimates.Where(e => e.ProjectId == SelectedProject.Id).ToList();

    [ObservableProperty]
    private Project? selectedProject;

    [ObservableProperty]
    private Material? selectedMaterial;

    [ObservableProperty]
    private Estimate? selectedEstimate;

    [ObservableProperty]
    private decimal quantity;

    [ObservableProperty]
    private decimal unitPrice;

    [ObservableProperty]
    private DateTime date;

    [ObservableProperty]
    private string documentNumber;

    [ObservableProperty]
    private string? note;

    public decimal Cost => Quantity * UnitPrice;

    public string UnitDisplay => SelectedMaterial?.UnitDisplay ?? string.Empty;

    /// <summary>Доступно к списанию с учётом количества, которое вернётся при редактировании.</summary>
    public decimal AvailableQuantity => (SelectedMaterial?.StockQuantity ?? 0m) + _originalQuantity;

    public string StockHint => SelectedMaterial is null
        ? "Выберите материал, чтобы увидеть остаток"
        : $"Доступно на складе: {AvailableQuantity:N2} {UnitDisplay}";

    public bool IsOverStock => SelectedMaterial is not null && Quantity > AvailableQuantity;

    public string OverStockMessage => IsOverStock
        ? $"Количество превышает остаток на {(Quantity - AvailableQuantity):N2} {UnitDisplay}. Списание пройдёт с отрицательным остатком."
        : string.Empty;

    partial void OnSelectedMaterialChanged(Material? value)
    {
        if (value is not null)
            UnitPrice = value.Price;

        OnPropertyChanged(nameof(UnitDisplay));
        OnPropertyChanged(nameof(AvailableQuantity));
        OnPropertyChanged(nameof(StockHint));
        OnPropertyChanged(nameof(IsOverStock));
        OnPropertyChanged(nameof(OverStockMessage));
    }

    partial void OnQuantityChanged(decimal value)
    {
        OnPropertyChanged(nameof(Cost));
        OnPropertyChanged(nameof(IsOverStock));
        OnPropertyChanged(nameof(OverStockMessage));
    }

    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(Cost));

    partial void OnSelectedProjectChanged(Project? value)
    {
        OnPropertyChanged(nameof(FilteredEstimates));
        if (SelectedEstimate is not null && SelectedEstimate.ProjectId != value?.Id)
            SelectedEstimate = null;
    }

    protected override string? Validate()
    {
        if (SelectedProject is null) return "Выберите строительный объект.";
        if (SelectedMaterial is null) return "Выберите материал.";
        if (Quantity <= 0m) return "Количество должно быть больше нуля.";
        if (UnitPrice < 0m) return "Цена не может быть отрицательной.";
        if (string.IsNullOrWhiteSpace(DocumentNumber)) return "Укажите номер акта списания.";
        return null;
    }

    public MaterialUsage BuildEntity() => new()
    {
        Id = _source?.Id ?? 0,
        RowVersion = _source?.RowVersion ?? 0,
        CreatedAt = _source?.CreatedAt ?? DateTime.UtcNow,
        ProjectId = SelectedProject?.Id ?? 0,
        MaterialId = SelectedMaterial?.Id ?? 0,
        EstimateId = SelectedEstimate?.Id,
        Quantity = Quantity,
        UnitPrice = UnitPrice,
        Date = Date.Date,
        DocumentNumber = DocumentNumber.Trim(),
        Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim(),
        CreatedByUserId = _source?.CreatedByUserId ?? CurrentUserId
    };
}
