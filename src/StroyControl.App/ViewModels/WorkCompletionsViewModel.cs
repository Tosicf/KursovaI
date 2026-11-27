using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.App.Localization;
using StroyControl.App.Services;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;
using StroyControl.Core.Interfaces;

namespace StroyControl.App.ViewModels;

/// <summary>
/// Журнал актов выполненных работ. Акт фиксирует фактический объём работ по смете
/// и списывает израсходованные материалы со склада; оформлять его можно только по
/// смете в состоянии «В работе».
/// </summary>
public sealed partial class WorkCompletionsViewModel : ViewModelBase
{
    public const string AllProjects = "Все объекты";

    private readonly IWorkCompletionRepository _completions;
    private readonly IWorkCompletionService _service;
    private readonly IProjectRepository _projects;
    private readonly IEstimateRepository _estimates;
    private readonly IMaterialRepository _materials;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly ISessionService _session;

    public WorkCompletionsViewModel(
        IWorkCompletionRepository completions,
        IWorkCompletionService service,
        IProjectRepository projects,
        IEstimateRepository estimates,
        IMaterialRepository materials,
        IDialogService dialogs,
        IToastService toasts,
        ISessionService session)
    {
        _completions = completions;
        _service = service;
        _projects = projects;
        _estimates = estimates;
        _materials = materials;
        _dialogs = dialogs;
        _toasts = toasts;
        _session = session;

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterItem;

        ProjectFilterOptions.Add(AllProjects);

        Period = new PeriodFilterViewModel(ApplyFilter);
    }

    public override string Title => L.T("Акты выполненных работ");

    public ObservableCollection<WorkCompletion> Items { get; } = new();

    public ObservableCollection<Project> Projects { get; } = new();

    public ObservableCollection<Estimate> Estimates { get; } = new();

    public ObservableCollection<Material> Materials { get; } = new();

    public ObservableCollection<string> ProjectFilterOptions { get; } = new();

    public ICollectionView ItemsView { get; }

    public PeriodFilterViewModel Period { get; }

    /// <summary>Акт меняет и работы, и остатки склада — права те же, что на списания.</summary>
    public bool CanEdit => _session.CanEditUsages;

    public bool IsEmpty => Items.Count == 0;

    public bool IsFilteredEmpty => Items.Count > 0 && VisibleCount == 0;

    public int TotalCount => Items.Count;

    public string TotalCostDisplay => Items.Sum(c => c.Total).ToString("N2") + " BYN";

    public string WorksCostDisplay => Items.Sum(c => c.WorksCost).ToString("N2") + " BYN";

    public string MaterialsCostDisplay => Items.Sum(c => c.MaterialsCost).ToString("N2") + " BYN";

    public int ProjectsCovered => Items.Select(c => c.ProjectId).Distinct().Count();

    /// <summary>Смет в работе нет — факт учитывать не по чему, кнопка создания блокируется.</summary>
    public bool HasEstimatesInWork => Estimates.Any(e => e.Status == EstimateStatus.InWork);

    public string NoEstimateInWorkHint => HasEstimatesInWork
        ? string.Empty
        : L.T("Нет смет в работе: переведите утверждённую смету в работу.");

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string selectedProjectFilter = AllProjects;

    [ObservableProperty]
    private int visibleCount;

    [ObservableProperty]
    private WorkCompletion? selectedCompletion;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedProjectFilterChanged(string value) => ApplyFilter();

    public override async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        var ok = await ExecuteAsync(async () =>
        {
            var completions = await _completions.GetAllWithDetailsAsync();
            var projects = await _projects.GetAllWithEstimatesAsync();
            var estimates = await _estimates.GetAllWithItemsAsync();
            var materials = await _materials.GetAllWithSupplierAsync();

            var previousId = SelectedCompletion?.Id;

            Items.Clear();
            foreach (var completion in completions)
                Items.Add(completion);

            Projects.Clear();
            foreach (var project in projects)
                Projects.Add(project);

            Estimates.Clear();
            foreach (var estimate in estimates)
                Estimates.Add(estimate);

            Materials.Clear();
            foreach (var material in materials.Where(m => m.IsActive))
                Materials.Add(material);

            var previousFilter = SelectedProjectFilter;

            ProjectFilterOptions.Clear();
            ProjectFilterOptions.Add(AllProjects);
            foreach (var project in projects)
                ProjectFilterOptions.Add(project.Name);

            SelectedProjectFilter = !string.IsNullOrWhiteSpace(previousFilter) && ProjectFilterOptions.Contains(previousFilter)
                ? previousFilter
                : AllProjects;

            Period.Rebuild(Items.Select(c => (DateTime?)c.Date));
            ItemsView.Refresh();

            SelectedCompletion = Items.FirstOrDefault(c => c.Id == previousId) ?? Items.FirstOrDefault();
            UpdateStatistics();
        }, L.T("Не удалось загрузить акты выполненных работ."));

        IsBusy = false;

        if (!ok && ErrorMessage is not null)
            _toasts.Error(Title, ErrorMessage);
    }

    /// <param name="template">
    /// Акт, из контекстного меню которого открывается форма: берётся его смета,
    /// чтобы быстро оформить следующий документ по той же смете.
    /// </param>
    [RelayCommand]
    private async Task AddAsync(WorkCompletion? template)
    {
        if (!CanEdit)
        {
            _toasts.Info(L.T("Недостаточно прав"), L.T("Текущая роль работает в режиме просмотра."));
            return;
        }

        if (!HasEstimatesInWork)
        {
            _toasts.Info(Title, NoEstimateInWorkHint);
            return;
        }

        var entity = _dialogs.EditWorkCompletion(
            null, Estimates, Materials, _session.CurrentUser?.Id, template?.EstimateId,
            usedNumbers: await SafeNumbersAsync());
        if (entity is null) return;

        AttachReferences(entity);

        try
        {
            var created = await _service.CreateAsync(entity);
            await LoadAsync();
            SelectedCompletion = Items.FirstOrDefault(c => c.Id == created.Id) ?? Items.FirstOrDefault();

            // Акт может быть оформлен задним числом: если выбранный период его не
            // покрывает, документ «пропадал» бы из списка сразу после сохранения.
            ShowDate(created.Date);
            _toasts.Success(L.T("Акт выполненных работ сохранён"),
                $"{created.DocumentNumber} · {created.Total:N2} BYN");

            // Материалы уже списаны, акт можно печатать сразу.
            _dialogs.ShowWorkCompletionAct(SelectedCompletion ?? created);
        }
        catch (Exception ex)
        {
            _toasts.Error(L.T("Не удалось сохранить акт"), ex.Message);
        }
    }

    [RelayCommand]
    private async Task EditAsync(WorkCompletion? completion)
    {
        var target = completion ?? SelectedCompletion;
        if (!CanEdit || target is null) return;

        var updated = _dialogs.EditWorkCompletion(
            target, Estimates, Materials, _session.CurrentUser?.Id,
            usedNumbers: await SafeNumbersAsync());
        if (updated is null) return;

        try
        {
            await _service.UpdateAsync(updated);
            await LoadAsync();
            SelectedCompletion = Items.FirstOrDefault(c => c.Id == target.Id) ?? Items.FirstOrDefault();
            ShowDate(updated.Date);
            _toasts.Success(L.T("Акт изменён"), L.T("Остатки склада и факт пересчитаны"));
        }
        catch (Exception ex)
        {
            _toasts.Error(L.T("Не удалось изменить акт"), ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(WorkCompletion? completion)
    {
        var target = completion ?? SelectedCompletion;
        if (!CanEdit || target is null) return;

        var confirmed = _dialogs.Confirm(
            L.T("Удалить акт выполненных работ?"),
            L.T("Акт {0} будет удалён. Материалы вернутся на склад, факт по смете пересчитается.")
                .Replace("{0}", target.DocumentNumber),
            L.T("Удалить акт"));

        if (!confirmed) return;

        var index = Items.IndexOf(target);
        Items.Remove(target);
        UpdateStatistics();

        try
        {
            await _service.DeleteAsync(target.Id);
            _toasts.Success(L.T("Акт удалён"), target.DocumentNumber);
        }
        catch (Exception ex)
        {
            if (index >= 0 && index <= Items.Count)
                Items.Insert(index, target);

            UpdateStatistics();
            _toasts.Error(L.T("Не удалось удалить акт"), ex.Message);
        }
    }

    /// <summary>Печать акта: работы, материалы и итоги собираются из сохранённого документа.</summary>
    [RelayCommand]
    private void PrintAct(WorkCompletion? completion)
    {
        var target = completion ?? SelectedCompletion;
        if (target is null)
        {
            _toasts.Info(Title, L.T("Выберите акт в списке."));
            return;
        }

        try
        {
            _dialogs.ShowWorkCompletionAct(target);
        }
        catch (Exception ex)
        {
            _toasts.Error(L.T("Не удалось открыть акт"), ex.Message);
        }
    }

    /// <summary>
    /// Показывает документ в списке: если текущий период не содержит дату акта,
    /// период сбрасывается на «всё время», иначе новый акт остался бы невидимым.
    /// </summary>
    private void ShowDate(DateTime date)
    {
        if (Period.Matches(date)) return;

        Period.Reset();
        ApplyFilter();
    }

    /// <summary>
    /// Номера сохранённых актов для подбора свободного номера. Если запрос не удался,
    /// форма всё равно откроется — номер можно ввести вручную.
    /// </summary>
    private async Task<IReadOnlyList<string>?> SafeNumbersAsync()
    {
        try
        {
            return await _service.GetNumbersAsync();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void AttachReferences(WorkCompletion completion)
    {
        completion.Estimate = Estimates.FirstOrDefault(e => e.Id == completion.EstimateId);
        completion.Project = Projects.FirstOrDefault(p => p.Id == completion.ProjectId);
        completion.CreatedBy = _session.CurrentUser;
    }

    private bool FilterItem(object item)
    {
        if (item is not WorkCompletion completion) return false;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            var matches = completion.DocumentNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
                          || completion.EstimateNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
                          || completion.ProjectName.Contains(term, StringComparison.OrdinalIgnoreCase)
                          || completion.AuthorName.Contains(term, StringComparison.OrdinalIgnoreCase);
            if (!matches) return false;
        }

        if (!Period.Matches(completion.Date)) return false;

        return string.IsNullOrWhiteSpace(SelectedProjectFilter) || SelectedProjectFilter == AllProjects
               || completion.ProjectName == SelectedProjectFilter;
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
        OnPropertyChanged(nameof(WorksCostDisplay));
        OnPropertyChanged(nameof(MaterialsCostDisplay));
        OnPropertyChanged(nameof(ProjectsCovered));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsFilteredEmpty));
        OnPropertyChanged(nameof(HasEstimatesInWork));
        OnPropertyChanged(nameof(NoEstimateInWorkHint));
    }
}
