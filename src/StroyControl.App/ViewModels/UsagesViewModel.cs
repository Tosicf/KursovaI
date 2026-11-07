using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.App.Services;
using StroyControl.Core.Entities;
using StroyControl.Core.Interfaces;

namespace StroyControl.App.ViewModels;

/// <summary>Фактические списания материалов на объекты (акты расхода).</summary>
public sealed partial class UsagesViewModel : ViewModelBase
{
    public const string AllProjects = "Все объекты";

    private readonly IUsageRepository _usages;
    private readonly IInventoryService _inventory;
    private readonly IProjectRepository _projects;
    private readonly IMaterialRepository _materials;
    private readonly IEstimateRepository _estimates;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly ISessionService _session;

    public UsagesViewModel(
        IUsageRepository usages,
        IInventoryService inventory,
        IProjectRepository projects,
        IMaterialRepository materials,
        IEstimateRepository estimates,
        IDialogService dialogs,
        IToastService toasts,
        ISessionService session)
    {
        _usages = usages;
        _inventory = inventory;
        _projects = projects;
        _materials = materials;
        _estimates = estimates;
        _dialogs = dialogs;
        _toasts = toasts;
        _session = session;

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterItem;

        ProjectFilterOptions.Add(AllProjects);
    }

    public override string Title => "Списания";

    public ObservableCollection<MaterialUsage> Items { get; } = new();

    public ObservableCollection<Project> Projects { get; } = new();

    public ObservableCollection<Material> Materials { get; } = new();

    public ObservableCollection<Estimate> Estimates { get; } = new();

    public ObservableCollection<string> ProjectFilterOptions { get; } = new();

    public ICollectionView ItemsView { get; }

    public bool CanEdit => _session.CanEditUsages;

    public bool IsEmpty => Items.Count == 0;

    public bool IsFilteredEmpty => Items.Count > 0 && VisibleCount == 0;

    public int TotalCount => Items.Count;

    public string TotalCostDisplay => Items.Sum(u => u.Cost).ToString("N2") + " BYN";

    public string TotalQuantityDisplay => Items.Count == 0
        ? "—"
        : Items.Sum(u => u.Quantity).ToString("N2");

    public int ProjectsCovered => Items.Select(u => u.ProjectId).Distinct().Count();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedProjectFilter = AllProjects;

    [ObservableProperty]
    private int visibleCount;

    [ObservableProperty]
    private MaterialUsage? selectedUsage;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedProjectFilterChanged(string value) => ApplyFilter();

    public override async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        var ok = await ExecuteAsync(async () =>
        {
            var usages = await _usages.GetAllWithDetailsAsync();
            var projects = await _projects.GetAllWithEstimatesAsync();
            var materials = await _materials.GetAllWithSupplierAsync();
            var estimates = await _estimates.GetAllWithItemsAsync();

            var previousId = SelectedUsage?.Id;

            Items.Clear();
            foreach (var usage in usages)
                Items.Add(usage);

            Projects.Clear();
            foreach (var project in projects)
                Projects.Add(project);

            Materials.Clear();
            foreach (var material in materials.Where(m => m.IsActive))
                Materials.Add(material);

            Estimates.Clear();
            foreach (var estimate in estimates)
                Estimates.Add(estimate);

            ProjectFilterOptions.Clear();
            ProjectFilterOptions.Add(AllProjects);
            foreach (var project in projects)
                ProjectFilterOptions.Add(project.Name);

            SelectedUsage = Items.FirstOrDefault(u => u.Id == previousId) ?? Items.FirstOrDefault();
            UpdateStatistics();
        }, "Не удалось загрузить списания.");

        IsBusy = false;

        if (!ok && ErrorMessage is not null)
            _toasts.Error("Списания", ErrorMessage);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (!CanEdit)
        {
            _toasts.Info("Недостаточно прав", "Текущая роль работает в режиме просмотра.");
            return;
        }

        if (Projects.Count == 0 || Materials.Count == 0)
        {
            _toasts.Error("Недостаточно данных", "Сначала заполните справочники объектов и материалов.");
            return;
        }

        var entity = _dialogs.EditUsage(null, Projects, Materials, Estimates, _session.CurrentUser?.Id);
        if (entity is null) return;

        AttachReferences(entity);

        try
        {
            var created = await _inventory.CreateUsageAsync(entity);
            AttachReferences(created);
            created.CreatedBy = _session.CurrentUser;
            Items.Insert(0, created);
            SelectedUsage = created;
            ApplyFilter();
            _toasts.Success("Списание оформлено", $"{created.MaterialName} · {created.Quantity:N2} {created.UnitDisplay}");
        }
        catch (Exception ex)
        {
            _toasts.Error("Не удалось оформить списание", ex.Message);
        }
    }

    [RelayCommand]
    private async Task EditAsync(MaterialUsage? usage)
    {
        if (!CanEdit || usage is null) return;

        var updated = _dialogs.EditUsage(usage, Projects, Materials, Estimates, _session.CurrentUser?.Id);
        if (updated is null) return;

        var index = Items.IndexOf(usage);
        if (index < 0) return;

        updated.CreatedBy = usage.CreatedBy;
        AttachReferences(updated);
        Items[index] = updated;
        UpdateStatistics();

        try
        {
            await _inventory.UpdateUsageAsync(updated);
            _toasts.Success("Списание изменено", "Остатки пересчитаны");
        }
        catch (Exception ex)
        {
            Items[index] = usage;
            UpdateStatistics();
            _toasts.Error("Не удалось изменить списание", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(MaterialUsage? usage)
    {
        if (!CanEdit || usage is null) return;

        var confirmed = _dialogs.Confirm(
            "Удалить списание?",
            $"Акт {usage.DocumentNumber} будет удалён. Материал вернётся на склад, отчёт об отклонениях пересчитается.",
            "Удалить списание");

        if (!confirmed) return;

        var index = Items.IndexOf(usage);
        Items.Remove(usage);
        UpdateStatistics();

        try
        {
            await _inventory.DeleteUsageAsync(usage.Id);
            _toasts.Success("Списание удалено", usage.DocumentNumber);
        }
        catch (Exception ex)
        {
            if (index >= 0 && index <= Items.Count)
                Items.Insert(index, usage);

            UpdateStatistics();
            _toasts.Error("Не удалось удалить списание", ex.Message);
        }
    }

    private void AttachReferences(MaterialUsage usage)
    {
        usage.Project = Projects.FirstOrDefault(p => p.Id == usage.ProjectId);
        usage.Material = Materials.FirstOrDefault(m => m.Id == usage.MaterialId);
        usage.Estimate = Estimates.FirstOrDefault(e => e.Id == usage.EstimateId);
    }

    private bool FilterItem(object item)
    {
        if (item is not MaterialUsage usage) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            var matches = usage.DocumentNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
                          || usage.MaterialName.Contains(term, StringComparison.OrdinalIgnoreCase)
                          || usage.ProjectName.Contains(term, StringComparison.OrdinalIgnoreCase)
                          || usage.AuthorName.Contains(term, StringComparison.OrdinalIgnoreCase);
            if (!matches) return false;
        }

        return SelectedProjectFilter == AllProjects || usage.ProjectName == SelectedProjectFilter;
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
        OnPropertyChanged(nameof(TotalCostDisplay));
        OnPropertyChanged(nameof(TotalQuantityDisplay));
        OnPropertyChanged(nameof(ProjectsCovered));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsFilteredEmpty));
    }
}
