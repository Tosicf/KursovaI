using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.App.Services;
using StroyControl.Core.Entities;
using StroyControl.Core.Interfaces;

namespace StroyControl.App.ViewModels;

/// <summary>Поступления материалов: приходные накладные и их влияние на остатки склада.</summary>
public sealed partial class DeliveriesViewModel : ViewModelBase
{
    public const string AllProjects = "Все объекты";

    private readonly IDeliveryRepository _deliveries;
    private readonly IInventoryService _inventory;
    private readonly ISupplierRepository _suppliers;
    private readonly IProjectRepository _projects;
    private readonly IMaterialRepository _materials;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly ISessionService _session;

    public DeliveriesViewModel(
        IDeliveryRepository deliveries,
        IInventoryService inventory,
        ISupplierRepository suppliers,
        IProjectRepository projects,
        IMaterialRepository materials,
        IDialogService dialogs,
        IToastService toasts,
        ISessionService session)
    {
        _deliveries = deliveries;
        _inventory = inventory;
        _suppliers = suppliers;
        _projects = projects;
        _materials = materials;
        _dialogs = dialogs;
        _toasts = toasts;
        _session = session;

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterItem;

        ProjectFilterOptions.Add(AllProjects);
    }

    public override string Title => "Поступления";

    public ObservableCollection<Delivery> Items { get; } = new();

    public ObservableCollection<Supplier> Suppliers { get; } = new();

    public ObservableCollection<Project> Projects { get; } = new();

    public ObservableCollection<Material> Materials { get; } = new();

    public ObservableCollection<string> ProjectFilterOptions { get; } = new();

    public ICollectionView ItemsView { get; }

    public bool CanEdit => _session.CanEditDeliveries;

    public bool IsEmpty => Items.Count == 0;

    public bool IsFilteredEmpty => Items.Count > 0 && VisibleCount == 0;

    public int TotalCount => Items.Count;

    public string TotalValueDisplay => Items.Sum(d => d.Total).ToString("N2") + " BYN";

    public int TotalPositions => Items.Sum(d => d.ItemsCount);

    public string LastDeliveryDisplay => Items.Count == 0 ? "—" : Items.Max(d => d.Date).ToString("dd.MM.yyyy");

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedProjectFilter = AllProjects;

    [ObservableProperty]
    private int visibleCount;

    [ObservableProperty]
    private Delivery? selectedDelivery;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedProjectFilterChanged(string value) => ApplyFilter();

    public override async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        var ok = await ExecuteAsync(async () =>
        {
            var deliveries = await _deliveries.GetAllWithItemsAsync();
            var suppliers = await _suppliers.GetAllAsync();
            var projects = await _projects.GetAllWithEstimatesAsync();
            var materials = await _materials.GetAllWithSupplierAsync();

            var previousId = SelectedDelivery?.Id;

            Items.Clear();
            foreach (var delivery in deliveries)
                Items.Add(delivery);

            Suppliers.Clear();
            foreach (var supplier in suppliers)
                Suppliers.Add(supplier);

            Projects.Clear();
            foreach (var project in projects)
                Projects.Add(project);

            Materials.Clear();
            foreach (var material in materials.Where(m => m.IsActive))
                Materials.Add(material);

            ProjectFilterOptions.Clear();
            ProjectFilterOptions.Add(AllProjects);
            foreach (var project in projects)
                ProjectFilterOptions.Add(project.Name);

            SelectedDelivery = Items.FirstOrDefault(d => d.Id == previousId) ?? Items.FirstOrDefault();
            UpdateStatistics();
        }, "Не удалось загрузить поступления.");

        IsBusy = false;

        if (!ok && ErrorMessage is not null)
            _toasts.Error("Поступления", ErrorMessage);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (!CanEdit)
        {
            _toasts.Info("Недостаточно прав", "Текущая роль работает в режиме просмотра.");
            return;
        }

        await RefreshMaterialsAsync();

        if (Suppliers.Count == 0 || Materials.Count == 0)
        {
            _toasts.Error("Недостаточно данных", "Сначала заполните справочники поставщиков и материалов.");
            return;
        }

        var entity = _dialogs.EditDelivery(null, Suppliers, Projects, Materials);
        if (entity is null) return;

        AttachReferences(entity);

        try
        {
            var created = await _inventory.CreateDeliveryAsync(entity);
            AttachReferences(created);
            Items.Insert(0, created);
            SelectedDelivery = created;
            ApplyFilter();
            _toasts.Success("Поступление оформлено",
                $"{created.Number} · {created.ItemsCount} поз. · остатки обновлены");
        }
        catch (Exception ex)
        {
            _toasts.Error("Не удалось сохранить поступление", ex.Message);
        }
    }

    [RelayCommand]
    private async Task EditAsync(Delivery? delivery)
    {
        if (!CanEdit || delivery is null) return;

        await RefreshMaterialsAsync();

        var updated = _dialogs.EditDelivery(delivery, Suppliers, Projects, Materials);
        if (updated is null) return;

        var index = Items.IndexOf(delivery);
        if (index < 0) return;

        AttachReferences(updated);
        Items[index] = updated;
        UpdateStatistics();

        try
        {
            await _inventory.UpdateDeliveryAsync(updated);
            _toasts.Success("Поступление изменено", $"{updated.Number} · остатки пересчитаны");
        }
        catch (Exception ex)
        {
            Items[index] = delivery;
            UpdateStatistics();
            _toasts.Error("Не удалось изменить поступление", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Delivery? delivery)
    {
        if (!CanEdit || delivery is null) return;

        var confirmed = _dialogs.Confirm(
            "Удалить поступление?",
            $"Накладная {delivery.Number} будет удалена, а количество материалов на складе уменьшится на объём документа.",
            "Удалить поступление");

        if (!confirmed) return;

        var index = Items.IndexOf(delivery);
        Items.Remove(delivery);
        UpdateStatistics();

        try
        {
            await _inventory.DeleteDeliveryAsync(delivery.Id);
            _toasts.Success("Поступление удалено", delivery.Number);
        }
        catch (Exception ex)
        {
            if (index >= 0 && index <= Items.Count)
                Items.Insert(index, delivery);

            UpdateStatistics();
            _toasts.Error("Не удалось удалить поступление", ex.Message);
        }
    }

    private void AttachReferences(Delivery delivery)
    {
        delivery.Supplier = Suppliers.FirstOrDefault(s => s.Id == delivery.SupplierId);
        delivery.Project = Projects.FirstOrDefault(p => p.Id == delivery.ProjectId);

        foreach (var item in delivery.Items)
            item.Material = Materials.FirstOrDefault(m => m.Id == item.MaterialId);
    }

    private bool FilterItem(object item)
    {
        if (item is not Delivery delivery) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            var matches = delivery.Number.Contains(term, StringComparison.OrdinalIgnoreCase)
                          || delivery.SupplierName.Contains(term, StringComparison.OrdinalIgnoreCase)
                          || delivery.InvoiceNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
                          || delivery.ProjectName.Contains(term, StringComparison.OrdinalIgnoreCase);
            if (!matches) return false;
        }

        return SelectedProjectFilter == AllProjects || delivery.ProjectName == SelectedProjectFilter;
    }

    private void ApplyFilter()
    {
        ItemsView.Refresh();
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        VisibleCount = ItemsView.Cast<object>().Count();
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(TotalValueDisplay));
        OnPropertyChanged(nameof(TotalPositions));
        OnPropertyChanged(nameof(LastDeliveryDisplay));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsFilteredEmpty));
    }

    /// <summary>
    /// Перечитывает справочник материалов из БД перед открытием диалога: цены должны быть
    /// актуальными, иначе в форму подставится устаревшая цена из кэша страницы.
    /// </summary>
    private async Task RefreshMaterialsAsync()
    {
        try
        {
            var materials = await _materials.GetAllWithSupplierAsync();
            Materials.Clear();
            foreach (var material in materials.Where(m => m.IsActive))
                Materials.Add(material);
        }
        catch
        {
            // Связь недоступна — работаем с уже загруженным справочником.
        }
    }
}
