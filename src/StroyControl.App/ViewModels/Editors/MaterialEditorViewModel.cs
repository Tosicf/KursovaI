using CommunityToolkit.Mvvm.ComponentModel;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;

namespace StroyControl.App.ViewModels.Editors;

public sealed partial class MaterialEditorViewModel : EditorViewModelBase
{
    private readonly Material? _source;

    public MaterialEditorViewModel(Material? source, IReadOnlyList<Supplier> suppliers, IReadOnlyList<string> categories)
    {
        _source = source;
        Suppliers = suppliers;
        Categories = categories;

        Code = source?.Code ?? string.Empty;
        Name = source?.Name ?? string.Empty;
        Category = source?.Category ?? categories.FirstOrDefault() ?? string.Empty;
        SelectedUnit = Units.FirstOrDefault(u => u.Value == (source?.Unit ?? UnitOfMeasure.Piece)) ?? Units[0];
        Price = source?.Price ?? 0m;
        StockQuantity = source?.StockQuantity ?? 0m;
        MinStock = source?.MinStock ?? 0m;
        IsActive = source?.IsActive ?? true;
        SelectedSupplier = suppliers.FirstOrDefault(s => s.Id == source?.SupplierId);
        Note = source?.Note;
    }

    public override string Title => _source is null ? "Новый материал" : "Редактирование материала";

    public IReadOnlyList<Supplier> Suppliers { get; }

    public IReadOnlyList<string> Categories { get; }

    public IReadOnlyList<EnumOption<UnitOfMeasure>> Units { get; } = EnumExtensions.ToOptions<UnitOfMeasure>();

    [ObservableProperty]
    private string code;

    [ObservableProperty]
    private string name;

    [ObservableProperty]
    private string category;

    [ObservableProperty]
    private EnumOption<UnitOfMeasure> selectedUnit;

    [ObservableProperty]
    private decimal price;

    [ObservableProperty]
    private decimal stockQuantity;

    [ObservableProperty]
    private decimal minStock;

    [ObservableProperty]
    private bool isActive;

    [ObservableProperty]
    private Supplier? selectedSupplier;

    [ObservableProperty]
    private string? note;

    protected override string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Code)) return "Укажите артикул материала.";
        if (string.IsNullOrWhiteSpace(Name)) return "Укажите наименование материала.";
        if (Price < 0m) return "Цена не может быть отрицательной.";
        if (StockQuantity < 0m) return "Остаток не может быть отрицательным.";
        if (MinStock < 0m) return "Минимальный запас не может быть отрицательным.";
        return null;
    }

    public Material BuildEntity() => new()
    {
        Id = _source?.Id ?? 0,
        RowVersion = _source?.RowVersion ?? 0,
        CreatedAt = _source?.CreatedAt ?? DateTime.UtcNow,
        Code = Code.Trim(),
        Name = Name.Trim(),
        Category = (Category ?? string.Empty).Trim(),
        Unit = SelectedUnit.Value,
        Price = Price,
        StockQuantity = StockQuantity,
        MinStock = MinStock,
        IsActive = IsActive,
        SupplierId = SelectedSupplier?.Id,
        Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()
    };
}
