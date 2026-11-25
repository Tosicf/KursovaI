using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.App.Services;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;
using StroyControl.Core.Interfaces;
using StroyControl.Core.Services;

namespace StroyControl.App.ViewModels;

/// <summary>Строительные объекты организации.</summary>
public sealed partial class ProjectsViewModel : ViewModelBase
{
    public const string AllStatuses = "Все статусы";

    private readonly IProjectRepository _projects;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly ISessionService _session;
    private readonly DeviationsViewModel _deviations;
    private readonly EstimatesViewModel _estimates;
    private readonly PageNavigator _navigator;

    public ProjectsViewModel(
        IProjectRepository projects,
        IDialogService dialogs,
        IToastService toasts,
        ISessionService session,
        DeviationsViewModel deviations,
        EstimatesViewModel estimates,
        PageNavigator navigator)
    {
        _projects = projects;
        _dialogs = dialogs;
        _toasts = toasts;
        _session = session;
        _deviations = deviations;
        _estimates = estimates;
        _navigator = navigator;

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterItem;

        StatusOptions.Add(AllStatuses);
        foreach (var option in EnumExtensions.ToOptions<ProjectStatus>())
            StatusOptions.Add(option.Display);

        Period = new PeriodFilterViewModel(ApplyFilters);
    }

    public override string Title => "Объекты";

    public ObservableCollection<Project> Items { get; } = new();

    public ObservableCollection<string> StatusOptions { get; } = new();

    public ICollectionView ItemsView { get; }

    /// <summary>
    /// Период списка: объекты группируются по месяцу начала работ, но в выбранный
    /// месяц попадают и те, по которым в этом месяце были документы.
    /// </summary>
    public PeriodFilterViewModel Period { get; }

    public bool CanEdit => _session.CanEditProjects;

    public bool IsEmpty => Items.Count == 0;

    public bool IsFilteredEmpty => Items.Count > 0 && VisibleCount == 0;

    public int TotalCount => Items.Count;

    public int ActiveCount => Items.Count(p => p.Status == ProjectStatus.InProgress);

    public string ContractSumDisplay => Items.Sum(p => p.ContractSum).ToString("N0") + " BYN";

    public string EstimatesCount => Items.Sum(p => p.Estimates.Count).ToString();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedStatus = AllStatuses;

    [ObservableProperty]
    private int visibleCount;

    [ObservableProperty]
    private Project? selectedProject;

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    partial void OnSelectedStatusChanged(string value) => ApplyFilters();

    public override async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        var ok = await ExecuteAsync(async () =>
        {
            var projects = await _projects.GetAllWithEstimatesAsync();
            Items.Clear();
            foreach (var project in projects)
                Items.Add(project);

            // Месяцы для фильтра берутся из дат начала работ; выбор периода сохраняется.
            Period.Rebuild(Items.Select(p => (DateTime?)p.StartDate));
            ApplyFilters();
        }, "Не удалось загрузить список объектов.");

        IsBusy = false;

        if (!ok && ErrorMessage is not null)
            _toasts.Error("Объекты", ErrorMessage);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (!CanEdit)
        {
            _toasts.Info("Недостаточно прав", "Текущая роль работает в режиме просмотра.");
            return;
        }

        var entity = _dialogs.EditProject(null);
        if (entity is null) return;

        Items.Insert(0, entity);
        SortByMonth();
        ApplyFilters();

        try
        {
            await _projects.AddAsync(entity);
            Period.Rebuild(Items.Select(p => (DateTime?)p.StartDate));
            _toasts.Success("Объект добавлен", entity.Name);
        }
        catch (Exception ex)
        {
            Items.Remove(entity);
            UpdateStatistics();
            _toasts.Error("Не удалось сохранить объект", ex.Message);
        }
    }

    /// <summary>
    /// Открывает анализ отклонений с уже выбранным объектом: отчёт строится по нему
    /// сразу, без повторного выбора в выпадающем списке на другой странице.
    /// </summary>
    [RelayCommand]
    private void OpenDeviations(Project? project)
    {
        project ??= SelectedProject;
        if (project is null) return;

        _deviations.FocusProject(project.Id);
        _navigator.Open("deviations");
    }

    /// <summary>Открывает раздел «Сметы», отфильтрованный по выбранному объекту.</summary>
    [RelayCommand]
    private void OpenEstimates(Project? project)
    {
        project ??= SelectedProject;
        if (project is null) return;

        _estimates.FocusProject(project.Id);
        _navigator.Open("estimates");
    }

    [RelayCommand]
    private async Task EditAsync(Project? project)
    {
        if (!CanEdit || project is null) return;

        var updated = _dialogs.EditProject(project);
        if (updated is null) return;

        var index = Items.IndexOf(project);
        if (index < 0) return;

        Items[index] = updated;
        UpdateStatistics();

        try
        {
            await _projects.UpdateAsync(updated);

            updated.Estimates = project.Estimates;
            Items[index] = updated;

            SortByMonth();
            Period.Rebuild(Items.Select(p => (DateTime?)p.StartDate));
            ApplyFilters();
            _toasts.Success("Объект обновлён", updated.Name);
        }
        catch (Exception ex)
        {
            Items[index] = project;
            UpdateStatistics();
            _toasts.Error("Не удалось изменить объект", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Project? project)
    {
        if (!CanEdit || project is null) return;

        var confirmed = _dialogs.Confirm(
            "Удалить объект?",
            $"Объект «{project.Name}» будет удалён вместе со сметами. Если по объекту есть списания материалов, удаление будет заблокировано.",
            "Удалить");

        if (!confirmed) return;

        var index = Items.IndexOf(project);
        Items.Remove(project);
        UpdateStatistics();

        try
        {
            await _projects.DeleteAsync(project.Id);
            _toasts.Success("Объект удалён", project.Name);
        }
        catch (Exception ex)
        {
            if (index >= 0 && index <= Items.Count)
                Items.Insert(index, project);

            UpdateStatistics();
            _toasts.Error("Не удалось удалить объект", ex.Message);
        }
    }

    private bool FilterItem(object item)
    {
        if (item is not Project project) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            var matches = project.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                          || project.Code.Contains(term, StringComparison.OrdinalIgnoreCase)
                          || project.Address.Contains(term, StringComparison.OrdinalIgnoreCase)
                          || project.Customer.Contains(term, StringComparison.OrdinalIgnoreCase);
            if (!matches) return false;
        }

        // Объект показывается в периоде не только по месяцу начала работ: если по нему
        // в этом месяце составлялись сметы, он тоже попадает в список — так
        // объект-долгострой виден во всех месяцах, когда на нём велись работы.
        if (!Period.Matches(project.StartDate) && !project.Estimates.Any(e => Period.Matches(e.Date)))
            return false;

        return SelectedStatus == AllStatuses || project.StatusDisplay == SelectedStatus;
    }

    /// <summary>Пересортировка по месяцам: свежие месяцы сверху, внутри месяца — по названию.</summary>
    private void SortByMonth()
    {
        var ordered = Items
            .OrderByDescending(p => MonthPeriod.Start(p.StartDate))
            .ThenBy(p => p.Name, StringComparer.CurrentCulture)
            .ToList();

        for (var target = 0; target < ordered.Count; target++)
        {
            var current = Items.IndexOf(ordered[target]);
            if (current != target) Items.Move(current, target);
        }
    }

    private void ApplyFilters()
    {
        ItemsView.Refresh();
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        VisibleCount = ItemsView.Cast<object>().Count();
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ActiveCount));
        OnPropertyChanged(nameof(ContractSumDisplay));
        OnPropertyChanged(nameof(EstimatesCount));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsFilteredEmpty));
    }
}
