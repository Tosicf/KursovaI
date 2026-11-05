using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.Core.Entities;

namespace StroyControl.App.ViewModels.Editors;

public sealed partial class DeliveryEditorViewModel : EditorViewModelBase
{
    private readonly Delivery? _source;

    public DeliveryEditorViewModel(
        Delivery? source,
        IReadOnlyList<Supplier> suppliers,
        IReadOnlyList<Project> projects,
        IReadOnlyList<Material> materials)
    {
        _source = source;
        Suppliers = suppliers;
        Projects = projects;
        Materials = materials;

        Number = source?.Number ?? $"ПН-{DateTime.Now:yy}-{DateTime.Now:MMddHH}";
        Date = source?.Date ?? DateTime.Today;
        SelectedSupplier = suppliers.FirstOrDefault(s => s.Id == source?.SupplierId) ?? suppliers.FirstOrDefault();
        SelectedProject = projects.FirstOrDefault(p => p.Id == source?.ProjectId);
        InvoiceNumber = source?.InvoiceNumber ?? string.Empty;
        Note = source?.Note;

        Items = new ObservableCollection<DeliveryItemDraft>();
        Items.CollectionChanged += OnItemsCollectionChanged;

        if (source is not null)
        {
            foreach (var item in source.Items)
            {
                Items.Add(new DeliveryItemDraft
                {
                    Material = Materials.FirstOrDefault(m => m.Id == item.MaterialId),
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }
        }
    }

    public override string Title => _source is null ? "Новое поступление материалов" : "Поступление материалов";

    public override double PreferredWidth => 920;

    public override double ContentMaxHeight => 460;

    public IReadOnlyList<Supplier> Suppliers { get; }

    public IReadOnlyList<Project> Projects { get; }

    public IReadOnlyList<Material> Materials { get; }

    public ObservableCollection<DeliveryItemDraft> Items { get; }

    [ObservableProperty]
    private string number;

    [ObservableProperty]
    private DateTime date;

    [ObservableProperty]
    private Supplier? selectedSupplier;

    [ObservableProperty]
    private Project? selectedProject;

    [ObservableProperty]
    private string invoiceNumber;

    [ObservableProperty]
    private string? note;

    public decimal Total => Items.Sum(i => i.Cost);

    public string ItemsSummary => $"{Items.Count} поз. · {Total:N2} BYN";

    [RelayCommand]
    private void AddItem()
    {
        Items.Add(new DeliveryItemDraft { Material = Materials.FirstOrDefault() });
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(ItemsSummary));
    }

    [RelayCommand]
    private void RemoveItem(DeliveryItemDraft? item)
    {
        if (item is not null && Items.Contains(item))
            Items.Remove(item);
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (DeliveryItemDraft item in e.OldItems)
                item.PropertyChanged -= OnItemPropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (DeliveryItemDraft item in e.NewItems)
                item.PropertyChanged += OnItemPropertyChanged;
        }

        RaiseTotals();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DeliveryItemDraft.Cost))
            RaiseTotals();
    }

    private void RaiseTotals()
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(ItemsSummary));
    }

    protected override string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Number)) return "Укажите номер приходной накладной.";
        if (SelectedSupplier is null) return "Выберите поставщика.";
        if (!Items.Any(i => i.Material is not null && i.Quantity > 0m))
            return "Добавьте хотя бы одну строку: материал и количество больше нуля.";

        if (Items.Any(i => i.Material is not null && i.Quantity <= 0m))
            return "Количество в каждой строке должно быть больше нуля.";

        if (Items.Any(i => i.Material is not null && i.UnitPrice < 0m))
            return "Цена не может быть отрицательной.";

        return null;
    }

    public Delivery BuildEntity()
    {
        var delivery = new Delivery
        {
            Id = _source?.Id ?? 0,
            RowVersion = _source?.RowVersion ?? 0,
            CreatedAt = _source?.CreatedAt ?? DateTime.UtcNow,
            Number = Number.Trim(),
            Date = Date.Date,
            SupplierId = SelectedSupplier?.Id ?? 0,
            ProjectId = SelectedProject?.Id,
            InvoiceNumber = (InvoiceNumber ?? string.Empty).Trim(),
            Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim()
        };

        foreach (var item in Items.Where(i => i.Material is not null && i.Quantity > 0m))
        {
            delivery.Items.Add(new DeliveryItem
            {
                MaterialId = item.Material!.Id,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            });
        }

        return delivery;
    }
}
