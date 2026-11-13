using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.App.Services;
using StroyControl.Core.Dtos;
using StroyControl.Core.Entities;
using StroyControl.Core.Interfaces;

namespace StroyControl.App.ViewModels;

/// <summary>Справочник строительных материалов: поиск, фильтры и полный CRUD.</summary>
public sealed partial class MaterialsViewModel : ViewModelBase
{
    public const string AllCategories = "Все категории";
    public const string AllItemsFilter = "Все материалы";
    public const string LowStockFilter = "Ниже минимума";
    public const string OutOfStockFilter = "Нет на складе";
    public const string InactiveFilter = "Неактивные";

    private readonly IMaterialRepository _materials;
    private readonly ISupplierRepository _suppliers;
    private readonly IProjectRepository _projects;
    private readonly IDeliveryRepository _deliveries;
    private readonly IInventoryService _inventory;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly ISessionService _session;

    public MaterialsViewModel(
        IMaterialRepository materials,
        ISupplierRepository suppliers,
        IProjectRepository projects,
        IDeliveryRepository deliveries,
        IInventoryService inventory,
        IDialogService dialogs,
        IToastService toasts,
        ISessionService session)
    {
        _materials = materials;
        _suppliers = suppliers;
        _projects = projects;
        _deliveries = deliveries;
        _inventory = inventory;
        _dialogs = dialogs;
        _toasts = toasts;
        _session = session;

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterItem;

        Categories.Add(AllCategories);
        StockFilters.Add(AllItemsFilter);
        StockFilters.Add(LowStockFilter);
        StockFilters.Add(OutOfStockFilter);
        StockFilters.Add(InactiveFilter);
    }

    public override string Title => "Материалы";

    public ObservableCollection<Material> Items { get; } = new();

    public ObservableCollection<Supplier> SupplierOptions { get; } = new();

    public ObservableCollection<string> Categories { get; } = new();

    public ObservableCollection<string> StockFilters { get; } = new();

    public ICollectionView ItemsView { get; }

    public bool CanEdit => _session.CanEditMaterials;

    /// <summary>Оформлять поступления могут снабженец, прораб, администратор и инженер.</summary>
    public bool CanPurchase => _session.CanEditDeliveries;


    public bool IsEmpty => Items.Count == 0;

    public bool IsFilteredEmpty => Items.Count > 0 && VisibleCount == 0;

    public int TotalCount => Items.Count;

    public string StockValueDisplay => Items.Sum(i => i.StockValue).ToString("N2") + " BYN";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedCategory = AllCategories;

    [ObservableProperty]
    private string selectedStockFilter = AllItemsFilter;

    [ObservableProperty]
    private int visibleCount;

    [ObservableProperty]
    private int lowStockCount;

    [ObservableProperty]
    private Material? selectedMaterial;

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    partial void OnSelectedCategoryChanged(string value) => ApplyFilters();

    partial void OnSelectedStockFilterChanged(string value) => ApplyFilters();

    partial void OnSelectedMaterialChanged(Material? value)
    {
        OnPropertyChanged(nameof(HasSelection));
    }

    public bool HasSelection => SelectedMaterial is not null;

    public override async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        var ok = await ExecuteAsync(async () =>
        {
            var materials = await _materials.GetAllWithSupplierAsync();
            var suppliers = await _suppliers.GetAllAsync();
            var categories = await _materials.GetCategoriesAsync();

            Items.Clear();
            foreach (var material in materials)
                Items.Add(material);

            SupplierOptions.Clear();
            foreach (var supplier in suppliers)
                SupplierOptions.Add(supplier);

            // Очистка списка обнуляет SelectedCategory (привязка ComboBox TwoWay) — возвращаем выбор.
            var previousCategory = SelectedCategory;

            Categories.Clear();
            Categories.Add(AllCategories);
            foreach (var category in categories.Where(c => !string.IsNullOrWhiteSpace(c)))
                Categories.Add(category);

            SelectedCategory = !string.IsNullOrWhiteSpace(previousCategory) && Categories.Contains(previousCategory)
                ? previousCategory
                : AllCategories;

            UpdateStatistics();
        }, "Не удалось загрузить справочник материалов.");

        IsBusy = false;

        if (!ok && ErrorMessage is not null)
            _toasts.Error("Материалы", ErrorMessage);
    }

    /// <summary>
    /// Оформляет поступление по материалам, которые ниже минимума: строки накладной
    /// уже заполнены, количество рассчитано до двух минимумов.
    /// </summary>
    [RelayCommand]
    private async Task PurchaseDeficitAsync()
    {
        if (!CanPurchase)
        {
            _toasts.Info("Недостаточно прав", "Оформлять поступления может снабженец, прораб или администратор.");
            return;
        }

        try
        {
            var materials = await _materials.GetAllWithSupplierAsync();
            var deficit = PurchasePlanner.Deficit(materials);

            if (deficit.Count == 0)
            {
                _toasts.Info("Дефицита нет", "Все материалы обеспечены запасом не ниже минимума.");
                return;
            }

            var suppliers = await _suppliers.GetAllAsync();
            var projects = await _projects.GetAllAsync();

            var delivery = _dialogs.EditDelivery(null, suppliers, projects, materials, deficit);
            if (delivery is null) return;

            var saved = await _inventory.CreateDeliveryAsync(delivery);

            _toasts.Success("Поступление оформлено", $"{saved.Number} · {saved.ItemsCount} поз. · {saved.Total:N2} BYN");
            await LoadAsync();

            // Печатаем товарный чек по сохранённой накладной (с наименованиями и поставщиком).
            var full = await _deliveries.GetWithItemsAsync(saved.Id) ?? saved;
            _dialogs.ShowPurchaseReceipt(full);
        }
        catch (Exception ex)
        {
            _toasts.Error("Не удалось оформить поступление", ex.Message);
        }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (!CanEdit)
        {
            _toasts.Info("Недостаточно прав", "Роль «Бухгалтер» работает в режиме просмотра.");
            return;
        }

        var entity = _dialogs.EditMaterial(null, SupplierOptions, Categories.Skip(1).ToList());
        if (entity is null) return;

        // Навигация не заполняется до сохранения: EF Core не должен тянуть
        // связанного поставщика в граф изменений.
        Items.Insert(0, entity);
        UpdateStatistics();

        try
        {
            await _materials.AddAsync(entity);

            entity.Supplier = SupplierOptions.FirstOrDefault(s => s.Id == entity.SupplierId);
            var addedIndex = Items.IndexOf(entity);
            if (addedIndex >= 0) Items[addedIndex] = entity;

            _toasts.Success("Материал добавлен", $"{entity.Name} · {entity.Price:N2} BYN");
        }
        catch (Exception ex)
        {
            Items.Remove(entity);
            UpdateStatistics();
            _toasts.Error("Не удалось сохранить материал", ex.Message);
        }
    }

    [RelayCommand]
    private async Task EditAsync(Material? material)
    {
        if (!CanEdit)
        {
            _toasts.Info("Недостаточно прав", "Роль «Бухгалтер» работает в режиме просмотра.");
            return;
        }

        if (material is null) return;

        var updated = _dialogs.EditMaterial(material, SupplierOptions, Categories.Skip(1).ToList());
        if (updated is null) return;

        var index = Items.IndexOf(material);
        if (index < 0) return;

        Items[index] = updated;
        UpdateStatistics();

        try
        {
            await _materials.UpdateAsync(updated);

            updated.Supplier = SupplierOptions.FirstOrDefault(s => s.Id == updated.SupplierId);
            Items[index] = updated;

            _toasts.Success("Изменения сохранены", updated.Name);
        }
        catch (Exception ex)
        {
            Items[index] = material;
            UpdateStatistics();
            _toasts.Error("Не удалось изменить материал", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Material? material)
    {
        if (!CanEdit)
        {
            _toasts.Info("Недостаточно прав", "Роль «Бухгалтер» работает в режиме просмотра.");
            return;
        }

        if (material is null) return;

        var confirmed = _dialogs.Confirm(
            "Удалить материал?",
            $"Материал «{material.Name}» будет удалён из справочника. Позиции смет и документы, ссылающиеся на него, удалить не позволят.",
            "Удалить");

        if (!confirmed) return;

        var index = Items.IndexOf(material);
        Items.Remove(material);
        UpdateStatistics();

        try
        {
            await _materials.DeleteAsync(material.Id);
            _toasts.Success("Материал удалён", material.Name);
        }
        catch (Exception ex)
        {
            if (index >= 0 && index <= Items.Count)
                Items.Insert(index, material);

            UpdateStatistics();
            _toasts.Error("Не удалось удалить материал", ex.Message);
        }
    }

    [RelayCommand]
    private void ResetFilters()
    {
        SearchText = string.Empty;
        SelectedCategory = AllCategories;
        SelectedStockFilter = AllItemsFilter;
    }

    private bool FilterItem(object item)
    {
        if (item is not Material material) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            var matches = material.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                          || material.Code.Contains(term, StringComparison.OrdinalIgnoreCase)
                          || material.Category.Contains(term, StringComparison.OrdinalIgnoreCase);
            if (!matches) return false;
        }

        if (!string.IsNullOrWhiteSpace(SelectedCategory) &&
            !string.Equals(SelectedCategory, AllCategories, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(material.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase))
            return false;

        return SelectedStockFilter switch
        {
            LowStockFilter => material.IsLowStock && material.IsActive,
            OutOfStockFilter => material.IsOutOfStock,
            InactiveFilter => !material.IsActive,
            _ => true
        };
    }

    private void ApplyFilters()
    {
        ItemsView.Refresh();
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        VisibleCount = ItemsView.Cast<object>().Count();
        LowStockCount = Items.Count(i => i.IsLowStock && i.IsActive);

        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(StockValueDisplay));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsFilteredEmpty));
    }
}
