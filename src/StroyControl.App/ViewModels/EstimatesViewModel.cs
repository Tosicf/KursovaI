using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.App.Services;
using StroyControl.Core.Entities;
using StroyControl.Core.Interfaces;

namespace StroyControl.App.ViewModels;

/// <summary>
/// Сметы объекта: мастер-деталь. Слева перечень смет, справа — позиции выбранной сметы
/// с плановым количеством и ценой материала.
/// </summary>
public sealed partial class EstimatesViewModel : ViewModelBase
{
    public const string AllProjects = "Все объекты";

    private readonly IEstimateRepository _estimates;
    private readonly IProjectRepository _projects;
    private readonly IMaterialRepository _materials;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly ISessionService _session;

    public EstimatesViewModel(
        IEstimateRepository estimates,
        IProjectRepository projects,
        IMaterialRepository materials,
        IDialogService dialogs,
        IToastService toasts,
        ISessionService session)
    {
        _estimates = estimates;
        _projects = projects;
        _materials = materials;
        _dialogs = dialogs;
        _toasts = toasts;
        _session = session;

        EstimatesView = CollectionViewSource.GetDefaultView(Estimates);
        EstimatesView.Filter = FilterEstimate;

        ProjectFilterOptions.Add(AllProjects);
    }

    public override string Title => "Сметы";

    public ObservableCollection<Estimate> Estimates { get; } = new();

    public ObservableCollection<EstimateItem> Items { get; } = new();

    public ObservableCollection<Project> Projects { get; } = new();

    public ObservableCollection<Material> Materials { get; } = new();

    public ObservableCollection<string> ProjectFilterOptions { get; } = new();

    public ICollectionView EstimatesView { get; }

    public bool CanEdit => _session.CanEdit;

    public bool IsEmpty => Estimates.Count == 0;

    public bool IsFilteredEmpty => Estimates.Count > 0 && VisibleCount == 0;

    public bool HasSelection => SelectedEstimate is not null;

    public bool HasItems => Items.Count > 0;

    public bool IsItemsEmpty => SelectedEstimate is not null && Items.Count == 0;

    public decimal ItemsTotal => Items.Sum(i => i.PlannedCost);

    public string ItemsTotalDisplay => ItemsTotal.ToString("N2") + " BYN";

    public int ItemsCount => Items.Count;

    public string SelectionHint => SelectedEstimate is null
        ? "Выберите смету из списка"
        : $"{SelectedEstimate.Number} · {SelectedEstimate.ProjectName}";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedProjectFilter = AllProjects;

    [ObservableProperty]
    private int visibleCount;

    [ObservableProperty]
    private Estimate? selectedEstimate;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedProjectFilterChanged(string value) => ApplyFilter();

    partial void OnSelectedEstimateChanged(Estimate? value)
    {
        Items.Clear();
        if (value is not null)
        {
            foreach (var item in value.Items.OrderBy(i => i.Section).ThenBy(i => i.MaterialName))
                Items.Add(item);
        }

        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsItemsEmpty));
        OnPropertyChanged(nameof(SelectionHint));
        UpdateTotals();
    }

    public override async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        var ok = await ExecuteAsync(async () =>
        {
            var estimates = await _estimates.GetAllWithItemsAsync();
            var projects = await _projects.GetAllWithEstimatesAsync();
            var materials = await _materials.GetAllWithSupplierAsync();

            var previousId = SelectedEstimate?.Id;

            Estimates.Clear();
            foreach (var estimate in estimates)
                Estimates.Add(estimate);

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

            SelectedEstimate = Estimates.FirstOrDefault(e => e.Id == previousId) ?? Estimates.FirstOrDefault();
            UpdateStatistics();
        }, "Не удалось загрузить сметы.");

        IsBusy = false;

        if (!ok && ErrorMessage is not null)
            _toasts.Error("Сметы", ErrorMessage);
    }

    [RelayCommand]
    private async Task AddEstimateAsync()
    {
        if (!CanEdit)
        {
            _toasts.Info("Недостаточно прав", "Текущая роль работает в режиме просмотра.");
            return;
        }

        if (Projects.Count == 0)
        {
            _toasts.Error("Нет объектов", "Сначала создайте строительный объект на странице «Объекты».");
            return;
        }

        var entity = _dialogs.EditEstimate(null, Projects);
        if (entity is null) return;

        Estimates.Insert(0, entity);
        ApplyFilter();
        SelectedEstimate = entity;

        try
        {
            await _estimates.AddAsync(entity);

            entity.Project = Projects.FirstOrDefault(p => p.Id == entity.ProjectId);
            ApplyFilter();

            _toasts.Success("Смета создана", $"{entity.Number} · {entity.Name}");
        }
        catch (Exception ex)
        {
            Estimates.Remove(entity);
            ApplyFilter();
            _toasts.Error("Не удалось создать смету", ex.Message);
        }
    }

    [RelayCommand]
    private async Task EditEstimateAsync(Estimate? estimate)
    {
        if (!CanEdit || estimate is null) return;

        var updated = _dialogs.EditEstimate(estimate, Projects);
        if (updated is null) return;

        var index = Estimates.IndexOf(estimate);
        if (index < 0) return;

        // Позиции и объект нужны только для отображения — репозиторий
        // сохраняет исключительно реквизиты самой сметы.
        updated.Items = estimate.Items;
        Estimates[index] = updated;
        ApplyFilter();

        if (ReferenceEquals(SelectedEstimate, estimate))
            SelectedEstimate = updated;

        try
        {
            await _estimates.UpdateEstimateAsync(updated);

            updated.Project = Projects.FirstOrDefault(p => p.Id == updated.ProjectId);
            ApplyFilter();

            _toasts.Success("Смета обновлена", updated.Number);
        }
        catch (Exception ex)
        {
            Estimates[index] = estimate;
            ApplyFilter();
            _toasts.Error("Не удалось изменить смету", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteEstimateAsync(Estimate? estimate)
    {
        if (!CanEdit || estimate is null) return;

        var confirmed = _dialogs.Confirm(
            "Удалить смету?",
            $"Смета «{estimate.Number} · {estimate.Name}» и все её позиции будут удалены. Действие необратимо.",
            "Удалить смету");

        if (!confirmed) return;

        var index = Estimates.IndexOf(estimate);
        Estimates.Remove(estimate);
        ApplyFilter();

        if (ReferenceEquals(SelectedEstimate, estimate))
            SelectedEstimate = Estimates.FirstOrDefault();

        try
        {
            await _estimates.DeleteAsync(estimate.Id);
            _toasts.Success("Смета удалена", estimate.Number);
        }
        catch (Exception ex)
        {
            if (index >= 0 && index <= Estimates.Count)
                Estimates.Insert(index, estimate);

            ApplyFilter();
            _toasts.Error("Не удалось удалить смету", ex.Message);
        }
    }

    [RelayCommand]
    private async Task AddItemAsync()
    {
        if (!CanEdit || SelectedEstimate is null) return;

        if (Materials.Count == 0)
        {
            _toasts.Error("Нет материалов", "Справочник материалов пуст — добавьте материалы.");
            return;
        }

        var entity = _dialogs.EditEstimateItem(null, SelectedEstimate.Id, Materials);
        if (entity is null) return;

        entity.Material = Materials.FirstOrDefault(m => m.Id == entity.MaterialId);
        Items.Add(entity);
        UpdateTotals();

        try
        {
            var saved = await _estimates.AddItemAsync(entity);
            var index = Items.IndexOf(entity);
            if (index >= 0) Items[index] = saved;
            saved.Material = entity.Material;
            SelectedEstimate?.Items.Add(saved);
            UpdateTotals();
            _toasts.Success("Позиция добавлена", $"{entity.MaterialName} · {entity.PlannedCost:N2} BYN");
        }
        catch (Exception ex)
        {
            Items.Remove(entity);
            UpdateTotals();
            _toasts.Error("Не удалось добавить позицию", ex.Message);
        }
    }

    [RelayCommand]
    private async Task EditItemAsync(EstimateItem? item)
    {
        if (!CanEdit || item is null || SelectedEstimate is null) return;

        var updated = _dialogs.EditEstimateItem(item, SelectedEstimate.Id, Materials);
        if (updated is null) return;

        updated.Material = Materials.FirstOrDefault(m => m.Id == updated.MaterialId);

        var index = Items.IndexOf(item);
        if (index < 0) return;

        Items[index] = updated;
        UpdateTotals();

        try
        {
            await _estimates.UpdateItemAsync(updated);
            _toasts.Success("Позиция обновлена", updated.MaterialName);
        }
        catch (Exception ex)
        {
            Items[index] = item;
            UpdateTotals();
            _toasts.Error("Не удалось изменить позицию", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteItemAsync(EstimateItem? item)
    {
        if (!CanEdit || item is null) return;

        var confirmed = _dialogs.Confirm(
            "Удалить позицию сметы?",
            $"Позиция «{item.MaterialName}» будет удалена из сметы {SelectedEstimate?.Number}.",
            "Удалить позицию");

        if (!confirmed) return;

        var index = Items.IndexOf(item);
        Items.Remove(item);
        UpdateTotals();

        try
        {
            await _estimates.DeleteItemAsync(item.Id);
            _toasts.Success("Позиция удалена", item.MaterialName);
        }
        catch (Exception ex)
        {
            if (index >= 0 && index <= Items.Count)
                Items.Insert(index, item);

            UpdateTotals();
            _toasts.Error("Не удалось удалить позицию", ex.Message);
        }
    }

    private bool FilterEstimate(object item)
    {
        if (item is not Estimate estimate) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            var matches = estimate.Number.Contains(term, StringComparison.OrdinalIgnoreCase)
                          || estimate.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                          || estimate.ProjectName.Contains(term, StringComparison.OrdinalIgnoreCase);
            if (!matches) return false;
        }

        return SelectedProjectFilter == AllProjects || estimate.ProjectName == SelectedProjectFilter;
    }

    private void ApplyFilter()
    {
        EstimatesView.Refresh();
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        VisibleCount = EstimatesView.Cast<object>().Count();
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsFilteredEmpty));
    }

    private void UpdateTotals()
    {
        OnPropertyChanged(nameof(ItemsTotal));
        OnPropertyChanged(nameof(ItemsTotalDisplay));
        OnPropertyChanged(nameof(ItemsCount));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsItemsEmpty));
    }
}
