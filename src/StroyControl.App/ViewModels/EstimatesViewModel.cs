using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.App.Localization;
using StroyControl.App.Services;
using StroyControl.Core.Dtos;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;
using StroyControl.Core.Interfaces;
using StroyControl.Core.Services;

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
    private readonly IMinStockService _minStock;
    private readonly IUsageRepository _usages;
    private readonly IWorkCompletionRepository _completions;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly ISessionService _session;

    /// <summary>Объект, выбранный на другой странице: фильтр применится при загрузке.</summary>
    private int? _pendingProjectId;

    /// <summary>
    /// Акты списания, загруженные вместе со сметами: по ним считается фактическая
    /// стоимость каждой сметы для сравнения плана и факта.
    /// </summary>
    private IReadOnlyList<MaterialUsage> _usageCache = Array.Empty<MaterialUsage>();

    /// <summary>Акты выполненных работ: их стоимость работ тоже входит в факт по смете.</summary>
    private IReadOnlyList<WorkCompletion> _completionCache = Array.Empty<WorkCompletion>();

    public EstimatesViewModel(
        IEstimateRepository estimates,
        IProjectRepository projects,
        IMaterialRepository materials,
        IMinStockService minStock,
        IUsageRepository usages,
        IWorkCompletionRepository completions,
        IDialogService dialogs,
        IToastService toasts,
        ISessionService session)
    {
        _estimates = estimates;
        _projects = projects;
        _materials = materials;
        _minStock = minStock;
        _usages = usages;
        _completions = completions;
        _dialogs = dialogs;
        _toasts = toasts;
        _session = session;

        EstimatesView = CollectionViewSource.GetDefaultView(Estimates);
        EstimatesView.Filter = FilterEstimate;

        ProjectFilterOptions.Add(AllProjects);

        Period = new PeriodFilterViewModel(ApplyFilter);
    }

    public override string Title => L.T("Сметы");

    public ObservableCollection<Estimate> Estimates { get; } = new();

    public ObservableCollection<EstimateItem> Items { get; } = new();

    public ObservableCollection<Project> Projects { get; } = new();

    public ObservableCollection<Material> Materials { get; } = new();

    public ObservableCollection<string> ProjectFilterOptions { get; } = new();

    public ICollectionView EstimatesView { get; }

    /// <summary>Период списка: сметы группируются по дате сметы.</summary>
    public PeriodFilterViewModel Period { get; }

    public bool CanEdit => _session.CanEditEstimates;

    /// <summary>Правки разрешены только для выбранного черновика.</summary>
    public bool CanEditSelected => CanEdit && SelectedEstimate?.IsEditable == true;

    /// <summary>Подтвердить можно черновик, в котором есть хотя бы одна позиция.</summary>
    public bool CanConfirmSelected => CanEditSelected && Items.Count > 0;

    /// <summary>В архив отправляется утверждённая смета — в том числе та, что уже в работе.</summary>
    public bool CanArchiveSelected => CanEdit && SelectedEstimate?.Status is EstimateStatus.Approved or EstimateStatus.InWork;

    /// <summary>Выбранная смета зафиксирована — состав и суммы менять нельзя.</summary>
    public bool IsSelectedFixed => SelectedEstimate?.IsFixed == true;

    /// <summary>Подсказка о режиме работы с выбранной сметой.</summary>
    public string StatusHint => SelectedEstimate?.LockDisplay ?? "Выберите смету из списка";

    /// <summary>Смета передана в работу — план и факт можно сравнивать.</summary>
    public bool IsSelectedInWork => SelectedEstimate?.IsInWork == true;

    /// <summary>
    /// Смета не в работе (черновик, на проверке, утверждена без запуска): она ещё не
    /// считается действующей, факт по ней не вводится — в карточке стоит пометка.
    /// </summary>
    public bool IsSelectedNotInWork => SelectedEstimate is { } estimate && estimate.Status != EstimateStatus.InWork;

    /// <summary>Расшифровка факта: сколько пришлось на материалы и на работы.</summary>
    public string ActualBreakdownDisplay => SelectedEstimate is null || !SelectedEstimate.IsInWork
        ? L.T("Факт не собирается: смета не в работе")
        : $"{L.T("материалы")} {MaterialsActualCost:N2} · {L.T("работы")} {WorksActualCost:N2} BYN";

    /// <summary>Утверждённую смету можно перевести в работу.</summary>
    public bool CanStartWorkSelected => CanEdit && SelectedEstimate?.Status == EstimateStatus.Approved;

    /// <summary>
    /// Фактические затраты по выбранной смете: материалы из актов списания плюс работы
    /// из актов выполненных работ. Черновик, смета на проверке и утверждённая без
    /// запуска факт не собирают — для них сумма остаётся нулевой.
    /// </summary>
    [ObservableProperty]
    private decimal actualCost;

    /// <summary>Факт по материалам: акты списания (включая списания из актов работ).</summary>
    [ObservableProperty]
    private decimal materialsActualCost;

    /// <summary>Факт по работам: строки актов выполненных работ.</summary>
    [ObservableProperty]
    private decimal worksActualCost;

    /// <summary>Отклонение факта от плана: положительное значение — перерасход.</summary>
    public decimal DeviationAmount => ActualCost - ItemsTotal;

    public string ActualCostDisplay => ActualCost.ToString("N2") + " BYN";

    public string DeviationDisplay => DeviationAmount == 0m
        ? "0,00 BYN"
        : (DeviationAmount > 0m ? "+" : "−") + Math.Abs(DeviationAmount).ToString("N2") + " BYN";

    public string DeviationPercentDisplay => ItemsTotal == 0m
        ? "—"
        : (DeviationAmount > 0m ? "+" : DeviationAmount < 0m ? "−" : string.Empty)
          + Math.Abs(Math.Round(DeviationAmount / ItemsTotal * 100m, 1)).ToString("0.0") + " %";

    /// <summary>Степень отклонения по тем же порогам, что в анализе отклонений.</summary>
    public DeviationSeverity DeviationLevel => ItemsTotal == 0m
        ? DeviationSeverity.Normal
        : DeviationCalculator.Classify(Math.Abs(Math.Round(DeviationAmount / ItemsTotal * 100m, 1)));

    /// <summary>Пояснение к блоку «план / факт».</summary>
    public string PlanFactHint => SelectedEstimate is null
        ? "Выберите смету из списка"
        : SelectedEstimate.IsInWork
            ? "Смета в работе: план сравнивается с фактом по актам работ и списаниям"
            : SelectedEstimate.IsArchived
                ? "Смета в архиве: сравнение плана и факта доступно только для просмотра"
                : "Переведите смету в работу, чтобы сравнивать план и факт";

    /// <summary>Отчёт содержит финансовые показатели — доступен ролям с правом просмотра финансов.</summary>
    public bool CanViewReport => _session.CanViewFinance;

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
        // Факт по смете — материалы из актов списания и работы из актов работ.
        // Учитываются только сметы в работе: по остальным факт не собирается.
        if (value is null || value.Status != EstimateStatus.InWork)
        {
            MaterialsActualCost = 0m;
            WorksActualCost = 0m;
        }
        else
        {
            MaterialsActualCost = _usageCache.Where(u => u.EstimateId == value.Id).Sum(u => u.Cost);
            WorksActualCost = _completionCache.Where(c => c.EstimateId == value.Id).Sum(c => c.WorksCost);
        }

        ActualCost = MaterialsActualCost + WorksActualCost;

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
        RaiseEstimateStateChanged();
        UpdateTotals();
    }

    /// <summary>
    /// Принимает объект от реестра объектов: список смет будет отфильтрован по нему,
    /// а первая смета объекта — выбрана. Фильтр применяется в <see cref="LoadAsync"/>.
    /// </summary>
    public void FocusProject(int projectId) => _pendingProjectId = projectId;

    public override async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        var ok = await ExecuteAsync(async () =>
        {
            var pendingId = _pendingProjectId;
            _pendingProjectId = null;

            var estimates = await _estimates.GetAllWithItemsAsync();
            var projects = await _projects.GetAllWithEstimatesAsync();
            var materials = await _materials.GetAllWithSupplierAsync();

            // Факт по сметам собирается из актов списания — грузим их до выбора сметы,
            // чтобы блок «план / факт» сразу показывал актуальные суммы.
            _usageCache = await _usages.GetAllWithDetailsAsync();
            _completionCache = await _completions.GetAllWithDetailsAsync();

            var previousId = SelectedEstimate?.Id;

            Estimates.Clear();
            foreach (var estimate in estimates.Where(e => e.Status != EstimateStatus.Archived))
                Estimates.Add(estimate);

            Projects.Clear();
            foreach (var project in projects)
                Projects.Add(project);

            Materials.Clear();
            foreach (var material in materials.Where(m => m.IsActive))
                Materials.Add(material);

            // Очистка списка обнуляет SelectedProjectFilter (привязка ComboBox TwoWay) —
            // запоминаем выбор и возвращаем его, иначе таблица фильтруется в пустоту.
            var previousFilter = SelectedProjectFilter;

            ProjectFilterOptions.Clear();
            ProjectFilterOptions.Add(AllProjects);
            foreach (var project in projects)
                ProjectFilterOptions.Add(project.Name);

            SelectedProjectFilter = !string.IsNullOrWhiteSpace(previousFilter) && ProjectFilterOptions.Contains(previousFilter)
                ? previousFilter
                : AllProjects;

            // Переход из реестра объектов: показываем только сметы выбранного объекта.
            string? focusedProjectName = null;

            if (pendingId is { } projectId)
            {
                var projectName = projects.FirstOrDefault(p => p.Id == projectId)?.Name;
                if (!string.IsNullOrWhiteSpace(projectName) && ProjectFilterOptions.Contains(projectName))
                {
                    SelectedProjectFilter = projectName;
                    focusedProjectName = projectName;

                    // Поиск и ранее выбранный период могли бы спрятать сметы объекта —
                    // при переходе из реестра показываем все сметы этого объекта.
                    SearchText = string.Empty;
                    Period.SelectedYear = PeriodOption.AllYears;
                }
            }

            // Месяцы для фильтра — по датам смет; выбор периода сохраняется.
            Period.Rebuild(Estimates.Select(e => (DateTime?)e.Date));

            SelectedEstimate = Estimates.FirstOrDefault(e => e.Id == previousId) ?? Estimates.FirstOrDefault();

            // При переходе по объекту открываем его первую смету, а не чужую из истории выбора.
            if (focusedProjectName is not null)
            {
                var projectEstimate = Estimates.FirstOrDefault(e => e.ProjectName == focusedProjectName);
                if (projectEstimate is not null) SelectedEstimate = projectEstimate;
            }

            ApplyFilter();
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
        SortByMonth();
        Period.Rebuild(Estimates.Select(e => (DateTime?)e.Date));
        ApplyFilter();
        SelectedEstimate = entity;

        try
        {
            await _estimates.AddAsync(entity);

            entity.Project = Projects.FirstOrDefault(p => p.Id == entity.ProjectId);
            ApplyFilter();

            _toasts.Success("Смета создана", $"{entity.Number} · {entity.Name}");
            await SyncMinStockAsync();
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

        if (!estimate.IsEditable)
        {
            _toasts.Info("Смета зафиксирована", estimate.LockDisplay);
            return;
        }

        var updated = _dialogs.EditEstimate(estimate, Projects);
        if (updated is null) return;

        var index = Estimates.IndexOf(estimate);
        if (index < 0) return;

        // Позиции и объект нужны только для отображения — репозиторий
        // сохраняет исключительно реквизиты самой сметы.
        updated.Items = estimate.Items;
        Estimates[index] = updated;
        SortByMonth();
        Period.Rebuild(Estimates.Select(e => (DateTime?)e.Date));
        ApplyFilter();

        if (ReferenceEquals(SelectedEstimate, estimate))
            SelectedEstimate = updated;

        try
        {
            await _estimates.UpdateEstimateAsync(updated);

            updated.Project = Projects.FirstOrDefault(p => p.Id == updated.ProjectId);
            ApplyFilter();

            _toasts.Success("Смета обновлена", updated.Number);
            await SyncMinStockAsync();
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

        if (!estimate.IsEditable)
        {
            _toasts.Info("Удаление запрещено", "Удалить можно только черновик: подтверждённая смета хранит историю плана.");
            return;
        }

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
            await SyncMinStockAsync();
        }
        catch (Exception ex)
        {
            if (index >= 0 && index <= Estimates.Count)
                Estimates.Insert(index, estimate);

            ApplyFilter();
            _toasts.Error("Не удалось удалить смету", ex.Message);
        }
    }

    /// <summary>Открывает отчёт по выбранной смете (расчёт работ и материалов).</summary>
    [RelayCommand]
    private void ShowReport()
    {
        if (SelectedEstimate is null)
        {
            _toasts.Info("Смета не выбрана", "Выберите смету из списка слева.");
            return;
        }

        if (!CanViewReport)
        {
            _toasts.Info("Недостаточно прав", "Отчёт доступен ролям с доступом к финансовым показателям.");
            return;
        }

        _dialogs.ShowEstimateReport(SelectedEstimate);
    }

    [RelayCommand]
    private async Task AddItemAsync()
    {
        if (SelectedEstimate is null) return;

        if (!CanEditSelected)
        {
            _toasts.Info("Смета зафиксирована", SelectedEstimate.LockDisplay);
            return;
        }

        await RefreshMaterialsAsync();

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
            await SyncMinStockAsync();
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
        if (item is null || SelectedEstimate is null) return;

        if (!CanEditSelected)
        {
            _toasts.Info("Смета зафиксирована", SelectedEstimate.LockDisplay);
            return;
        }

        await RefreshMaterialsAsync();

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
            await SyncMinStockAsync();
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
        if (item is null) return;

        if (!CanEditSelected)
        {
            _toasts.Info("Смета зафиксирована", SelectedEstimate?.LockDisplay ?? "Изменения запрещены");
            return;
        }

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
            await SyncMinStockAsync();
        }
        catch (Exception ex)
        {
            if (index >= 0 && index <= Items.Count)
                Items.Insert(index, item);

            UpdateTotals();
            _toasts.Error("Не удалось удалить позицию", ex.Message);
        }
    }

    /// <summary>
    /// Подтверждение черновика: состав и суммы фиксируются, дальнейшие правки
    /// запрещены и на экране, и в репозитории.
    /// </summary>
    [RelayCommand]
    private async Task ConfirmEstimateAsync()
    {
        if (!CanEdit || SelectedEstimate is null) return;

        if (!SelectedEstimate.IsEditable)
        {
            _toasts.Info("Смета уже зафиксирована", SelectedEstimate.LockDisplay);
            return;
        }

        if (Items.Count == 0)
        {
            _toasts.Error("Пустая смета", "Добавьте хотя бы одну позицию — подтверждать нечего.");
            return;
        }

        var confirmed = _dialogs.Confirm(
            "Подтвердить смету?",
            $"Смета «{SelectedEstimate.Number} · {SelectedEstimate.Name}» будет зафиксирована: "
            + "изменить реквизиты, позиции и суммы станет нельзя, удалить её тоже нельзя. "
            + "Отменить подтверждение невозможно.",
            "Подтвердить смету");

        if (!confirmed) return;

        var number = SelectedEstimate.Number;

        try
        {
            await _estimates.ConfirmAsync(SelectedEstimate.Id);
            _toasts.Success("Смета подтверждена", $"{number}: состав и суммы зафиксированы");
            await LoadAsync();
            await SyncMinStockAsync();
        }
        catch (Exception ex)
        {
            _toasts.Error("Не удалось подтвердить смету", ex.Message);
        }
    }

    /// <summary>
    /// Утверждённая смета передаётся в работу: с этого момента план зафиксирован,
    /// а фактические затраты собираются по актам списания и сравниваются с планом.
    /// </summary>
    [RelayCommand]
    private async Task StartWorkAsync()
    {
        if (!CanEdit || SelectedEstimate is null) return;

        if (SelectedEstimate.Status != EstimateStatus.Approved)
        {
            _toasts.Info("Смета не утверждена",
                "В работу отправляется только утверждённая смета. Сначала нажмите «Подтвердить».");
            return;
        }

        var confirmed = _dialogs.Confirm(
            "Отправить смету в работу?",
            $"Смета «{SelectedEstimate.Number} · {SelectedEstimate.Name}» переходит в состояние «В работе»: "
            + "план остаётся зафиксированным, а фактические затраты будут собираться по актам списания "
            + "и сравниваться с планом. Вернуть смету в черновик нельзя.",
            "В работу");

        if (!confirmed) return;

        var number = SelectedEstimate.Number;

        try
        {
            await _estimates.StartWorkAsync(SelectedEstimate.Id);
            _toasts.Success("Смета в работе", $"{number}: план сравнивается с фактом по актам списания");
            await LoadAsync();
            await SyncMinStockAsync();
        }
        catch (Exception ex)
        {
            _toasts.Error("Не удалось отправить смету в работу", ex.Message);
        }
    }

    /// <summary>Завершённая смета уходит в архив — отдельную вкладку только для чтения.</summary>
    [RelayCommand]
    private async Task ArchiveEstimateAsync()
    {
        if (!CanEdit || SelectedEstimate is null) return;

        if (SelectedEstimate.Status is not (EstimateStatus.Approved or EstimateStatus.InWork))
        {
            _toasts.Info("Сначала подтвердите смету",
                "В архив отправляется только утверждённая смета — так сохраняется история плана.");
            return;
        }

        var confirmed = _dialogs.Confirm(
            "Отправить смету в архив?",
            $"Смета «{SelectedEstimate.Number} · {SelectedEstimate.Name}» завершена и будет перенесена "
            + "во вкладку «Архив». В работе со сметами она больше не участвует; вернуть её нельзя.",
            "В архив");

        if (!confirmed) return;

        var number = SelectedEstimate.Number;

        try
        {
            await _estimates.ArchiveAsync(SelectedEstimate.Id);
            _toasts.Success("Смета в архиве", $"{number}: перенесена во вкладку «Архив»");
            await LoadAsync();
            await SyncMinStockAsync();
        }
        catch (Exception ex)
        {
            _toasts.Error("Не удалось отправить смету в архив", ex.Message);
        }
    }

    /// <summary>
    /// Минимальный запас материалов считается из смет, поэтому после любого изменения
    /// сметы или её позиции он пересчитывается сразу — без отдельной кнопки.
    /// Ошибка пересчёта не отменяет сохранение сметы: показываем сообщение и продолжаем.
    /// </summary>
    private async Task SyncMinStockAsync()
    {
        try
        {
            var updated = await _minStock.RecalculateAsync();
            if (updated > 0)
                _toasts.Info("Минимальный запас пересчитан", $"Обновлено материалов: {updated}");
        }
        catch (Exception ex)
        {
            _toasts.Error("Не удалось пересчитать минимальный запас", ex.Message);
        }
    }

    /// <summary>Пересортировка по месяцам: свежие сметы сверху, внутри месяца — по номеру.</summary>
    private void SortByMonth()
    {
        var ordered = Estimates
            .OrderByDescending(e => MonthPeriod.Start(e.Date))
            .ThenBy(e => e.Number, StringComparer.CurrentCulture)
            .ToList();

        for (var target = 0; target < ordered.Count; target++)
        {
            var current = Estimates.IndexOf(ordered[target]);
            if (current != target) Estimates.Move(current, target);
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

        if (!Period.Matches(estimate.Date)) return false;

        return string.IsNullOrWhiteSpace(SelectedProjectFilter) || SelectedProjectFilter == AllProjects
               || estimate.ProjectName == SelectedProjectFilter;
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

    /// <summary>Состояние выбранной сметы влияет на доступность команд — сообщаем об этом разметке.</summary>
    private void RaiseEstimateStateChanged()
    {
        OnPropertyChanged(nameof(CanEditSelected));
        OnPropertyChanged(nameof(CanConfirmSelected));
        OnPropertyChanged(nameof(CanArchiveSelected));
        OnPropertyChanged(nameof(CanStartWorkSelected));
        OnPropertyChanged(nameof(IsSelectedFixed));
        OnPropertyChanged(nameof(IsSelectedInWork));
        OnPropertyChanged(nameof(IsSelectedNotInWork));
        OnPropertyChanged(nameof(ActualBreakdownDisplay));
        OnPropertyChanged(nameof(StatusHint));
        OnPropertyChanged(nameof(PlanFactHint));
    }

    private void UpdateTotals()
    {
        OnPropertyChanged(nameof(CanConfirmSelected));
        OnPropertyChanged(nameof(ItemsTotal));
        OnPropertyChanged(nameof(ItemsTotalDisplay));
        OnPropertyChanged(nameof(ActualCost));
        OnPropertyChanged(nameof(ActualCostDisplay));
        OnPropertyChanged(nameof(ActualBreakdownDisplay));
        OnPropertyChanged(nameof(DeviationAmount));
        OnPropertyChanged(nameof(DeviationDisplay));
        OnPropertyChanged(nameof(DeviationPercentDisplay));
        OnPropertyChanged(nameof(DeviationLevel));
        OnPropertyChanged(nameof(ItemsCount));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(IsItemsEmpty));
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
