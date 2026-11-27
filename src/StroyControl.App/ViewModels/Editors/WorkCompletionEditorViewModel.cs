using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.App.Localization;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;
using StroyControl.Core.Services;

namespace StroyControl.App.ViewModels.Editors;

/// <summary>
/// Акт выполненных работ: шапка документа, строки работ по позициям сметы и материалы,
/// израсходованные на эти работы. Диалог только проверяет данные — остатки склада,
/// транзакцию и правило «смета в работе» проверяет сервис при сохранении.
/// </summary>
public sealed partial class WorkCompletionEditorViewModel : EditorViewModelBase
{
    private readonly WorkCompletion? _source;

    /// <summary>Номера уже сохранённых актов — по ним подбирается свободный номер.</summary>
    private readonly IReadOnlyList<string> _usedNumbers;

    /// <summary>Номер, который форма подставила сама: если его не правили, при смене даты он пересчитывается.</summary>
    private string _autoNumber;

    /// <summary>Пока идёт заполнение формы, смена сметы не должна пересобирать строки.</summary>
    private bool _loading = true;

    /// <param name="estimateId">Смета, открытая в карточке: форма сразу заполняется её позициями.</param>
    /// <param name="estimateItemId">Позиция сметы, по которой вводится факт (из контекстного меню).</param>
    public WorkCompletionEditorViewModel(
        WorkCompletion? source,
        IReadOnlyList<Estimate> estimates,
        IReadOnlyList<Material> materials,
        int? currentUserId,
        int? estimateId = null,
        int? estimateItemId = null,
        IReadOnlyList<string>? usedNumbers = null)
    {
        _source = source;
        _usedNumbers = usedNumbers ?? Array.Empty<string>();
        Materials = materials;
        CurrentUserId = currentUserId;

        // Факт учитывается только по сметам в работе — остальные в выбор не попадают.
        Estimates = estimates.Where(e => e.Status == EstimateStatus.InWork).ToList();

        Works.CollectionChanged += OnWorksChanged;
        MaterialRows.CollectionChanged += OnMaterialRowsChanged;

        SelectedEstimate = Estimates.FirstOrDefault(e => e.Id == (source?.EstimateId ?? estimateId))
                           ?? Estimates.FirstOrDefault();
        Date = source?.Date ?? DateTime.Today;

        // Номер уникален в базе, поэтому берётся первый свободный по году акта, а не шаблон
        // по дате: иначе второй акт за день падал с ошибкой «номер уже используется».
        _autoNumber = source?.DocumentNumber ?? WorkCompletionNumbering.Next(_usedNumbers, Date);
        DocumentNumber = _autoNumber;
        Note = source?.Note;

        FillRows(source);

        // Форму открыли по конкретной позиции сметы — оставляем в акте только её.
        if (source is null && estimateItemId is not null)
            PrefillItem(estimateItemId.Value);

        _loading = false;
    }

    public override string Title => _source is null
        ? L.T("Акт выполненных работ")
        : L.T("Редактирование акта работ");

    public override string SaveText => L.T("Сохранить акт");

    public override double PreferredWidth => 1020;

    public override double ContentMaxHeight => 620;

    public IReadOnlyList<Estimate> Estimates { get; }

    public IReadOnlyList<Material> Materials { get; }

    public int? CurrentUserId { get; }

    public ObservableCollection<WorkCompletionWorkDraft> Works { get; } = new();

    public ObservableCollection<WorkCompletionMaterialDraft> MaterialRows { get; } = new();

    /// <summary>Позиции-работы выбранной сметы — источник строк акта.</summary>
    public IReadOnlyList<EstimateItem> WorkOptions =>
        SelectedEstimate?.Items.Where(i => i.IsWork).OrderBy(i => i.Section).ThenBy(i => i.NameDisplay).ToList()
        ?? new List<EstimateItem>();

    /// <summary>Позиции-материалы выбранной сметы — из них списывается склад.</summary>
    public IReadOnlyList<EstimateItem> MaterialOptions =>
        SelectedEstimate?.Items
            .Where(i => !i.IsWork && i.MaterialId is not null)
            .OrderBy(i => i.Section)
            .ThenBy(i => i.NameDisplay)
            .ToList()
        ?? new List<EstimateItem>();

    [ObservableProperty]
    private Estimate? selectedEstimate;

    [ObservableProperty]
    private DateTime date;

    [ObservableProperty]
    private string documentNumber;

    [ObservableProperty]
    private string? note;

    public bool HasEstimate => SelectedEstimate is not null;

    /// <summary>В смете есть позиции-работы — факт по работам можно ввести.</summary>
    public bool HasWorkOptions => WorkOptions.Count > 0;

    /// <summary>В смете только материалы: работы нужно сначала добавить в саму смету.</summary>
    public bool HasNoWorkOptions => SelectedEstimate is not null && WorkOptions.Count == 0;

    public string NoWorksHint => L.T("В смете нет позиций-работ: факт по работам ввести нельзя. Добавьте работы в смету или выберите другую смету.");

    public string EstimateHint => SelectedEstimate is null
        ? L.T("Смета в работе не выбрана")
        : $"{SelectedEstimate.Number} · {SelectedEstimate.Project?.Name ?? SelectedEstimate.ProjectName} · {L.T("В работе")}";

    public decimal WorksCost => Works.Where(w => w.Quantity > 0m).Sum(w => w.Cost);

    public decimal MaterialsCost => MaterialRows.Where(m => m.Quantity > 0m).Sum(m => m.Cost);

    public decimal Total => WorksCost + MaterialsCost;

    public string TotalDisplay => Total.ToString("N2") + " BYN";

    public string WorksSummary => $"{L.T("Работы")}: {Works.Count} · {WorksCost:N2} BYN";

    public string MaterialsSummary => $"{L.T("Материалы")}: {MaterialRows.Count} · {MaterialsCost:N2} BYN";

    public bool HasOverStock => MaterialRows.Any(m => m.Quantity > 0m && m.IsOverStock);

    public string OverStockWarning => HasOverStock
        ? L.T("Часть материалов списывается сверх остатка: склад уйдёт в минус.")
        : string.Empty;

    public bool IsEmpty => Works.Count == 0 && MaterialRows.Count == 0;

    partial void OnDateChanged(DateTime value)
    {
        // Номер «привязан» к году: пока его не правили вручную, при смене даты он пересчитывается.
        if (_source is not null || DocumentNumber != _autoNumber) return;

        _autoNumber = WorkCompletionNumbering.Next(_usedNumbers, value);
        DocumentNumber = _autoNumber;
    }

    partial void OnSelectedEstimateChanged(Estimate? value)
    {
        // Смена сметы меняет доступные работы и материалы: строки другой сметы
        // сохранять нельзя, поэтому список строк пересобирается.
        if (!_loading && value is not null && value.Id != _source?.EstimateId)
            ResetRows(value);

        OnPropertyChanged(nameof(WorkOptions));
        OnPropertyChanged(nameof(MaterialOptions));
        OnPropertyChanged(nameof(HasEstimate));
        OnPropertyChanged(nameof(EstimateHint));
        OnPropertyChanged(nameof(HasWorkOptions));
        OnPropertyChanged(nameof(HasNoWorkOptions));
        OnPropertyChanged(nameof(CanAddWork));
        OnPropertyChanged(nameof(CanAddMaterial));
    }

    /// <summary>Все позиции-работы сметы уже добавлены в акт — кнопка блокируется.</summary>
    public bool CanAddWork => WorkOptions.Any(item => !UsedWorkIds.Contains(item.Id));

    /// <summary>Все материалы сметы уже добавлены в акт — кнопка блокируется.</summary>
    public bool CanAddMaterial => MaterialOptions.Any(item => !UsedMaterialIds.Contains(item.Id));

    private HashSet<int> UsedWorkIds => Works.Where(w => w.EstimateItem is not null)
        .Select(w => w.EstimateItem!.Id).ToHashSet();

    private HashSet<int> UsedMaterialIds => MaterialRows.Where(m => m.MaterialItem is not null)
        .Select(m => m.MaterialItem!.Id).ToHashSet();

    [RelayCommand]
    private void AddWork()
    {
        var used = UsedWorkIds;
        var next = WorkOptions.FirstOrDefault(i => !used.Contains(i.Id));

        if (next is null)
        {
            // Упор: одинаковые позиции в акт не добавляются повторно.
            ErrorMessage = WorkOptions.Count == 0
                ? L.T("В выбранной смете нет позиций-работ: добавьте их в смету.")
                : L.T("Все позиции-работы сметы уже добавлены в акт.");
            return;
        }

        Works.Add(new WorkCompletionWorkDraft(next));
        ErrorMessage = null;
    }

    [RelayCommand]
    private void RemoveWork(WorkCompletionWorkDraft? draft)
    {
        if (draft is not null) Works.Remove(draft);
    }

    [RelayCommand]
    private void AddMaterial()
    {
        var used = UsedMaterialIds;
        var next = MaterialOptions.FirstOrDefault(i => !used.Contains(i.Id));

        if (next is null)
        {
            // Упор: один и тот же материал не попадает в акт двумя строками.
            ErrorMessage = MaterialOptions.Count == 0
                ? L.T("В выбранной смете нет материалов: добавьте их в смету.")
                : L.T("Все материалы сметы уже добавлены в акт.");
            return;
        }

        MaterialRows.Add(new WorkCompletionMaterialDraft(next));
        ErrorMessage = null;
    }

    [RelayCommand]
    private void RemoveMaterial(WorkCompletionMaterialDraft? draft)
    {
        if (draft is not null) MaterialRows.Remove(draft);
    }

    protected override string? Validate()
    {
        if (SelectedEstimate is null)
            return L.T("Выберите смету в работе.");

        if (SelectedEstimate.Status != EstimateStatus.InWork)
            return L.T("Факт учитывается только по смете в работе.");

        if (string.IsNullOrWhiteSpace(DocumentNumber))
            return L.T("Укажите номер акта выполненных работ.");

        // Список номеров мог устареть, но самый частый случай — повтор номера — ловим сразу,
        // не доводя до ошибки уникального индекса при сохранении.
        if (_source is null && _usedNumbers.Any(n =>
                !string.IsNullOrWhiteSpace(n)
                && string.Equals(n.Trim(), DocumentNumber.Trim(), StringComparison.OrdinalIgnoreCase)))
            return L.T("Номер акта уже используется. Измените номер и повторите сохранение.");

        if (Date.Date > MaxDate)
            return L.T("Дата акта не может быть в будущем.");

        if (Works.All(w => w.Quantity <= 0m) && MaterialRows.All(m => m.Quantity <= 0m))
            return L.T("В акте нет ни выполненных работ, ни израсходованных материалов.");

        if (Works.Where(w => w.EstimateItem is not null).GroupBy(w => w.EstimateItem!.Id).Any(g => g.Count() > 1))
            return L.T("Позиция сметы указана в акте дважды: оставьте одну строку.");

        if (MaterialRows.Where(m => m.MaterialItem is not null).GroupBy(m => m.MaterialItem!.Id).Any(g => g.Count() > 1))
            return L.T("Материал указан в акте дважды: оставьте одну строку.");

        if (Works.Any(w => w.Quantity > 0m && w.EstimateItem is null))
            return L.T("В строке работ не выбрана позиция сметы.");

        if (MaterialRows.Any(m => m.Quantity > 0m && m.Material is null))
            return L.T("В строке материалов не выбран материал.");

        return null;
    }

    public WorkCompletion BuildEntity()
    {
        var works = Works
            .Where(w => w.Quantity > 0m && w.EstimateItem is not null)
            .Select(w => new WorkCompletionItem
            {
                EstimateItemId = w.EstimateItem!.Id,
                Quantity = Math.Round(w.Quantity, 3),
                UnitPrice = Math.Round(w.UnitPrice, 2)
            })
            .ToList();

        var materials = MaterialRows
            .Where(m => m.Quantity > 0m && m.Material is not null)
            .Select(m => new MaterialUsage
            {
                MaterialId = m.Material!.Id,
                EstimateItemId = m.MaterialItem?.Id,
                Quantity = Math.Round(m.Quantity, 3),
                UnitPrice = Math.Round(m.UnitPrice, 2)
            })
            .ToList();

        return new WorkCompletion
        {
            Id = _source?.Id ?? 0,
            RowVersion = _source?.RowVersion ?? 0,
            CreatedAt = _source?.CreatedAt ?? DateTime.UtcNow,
            EstimateId = SelectedEstimate?.Id ?? 0,
            ProjectId = SelectedEstimate?.ProjectId ?? _source?.ProjectId ?? 0,
            Date = Date.Date,
            DocumentNumber = DocumentNumber.Trim(),
            Note = string.IsNullOrWhiteSpace(Note) ? null : Note.Trim(),
            CreatedByUserId = _source?.CreatedByUserId ?? CurrentUserId,
            Items = works,
            MaterialUsages = materials
        };
    }

    private void FillRows(WorkCompletion? source)
    {
        Works.Clear();
        MaterialRows.Clear();

        if (source is null)
        {
            if (WorkOptions.Count > 0) Works.Add(new WorkCompletionWorkDraft(WorkOptions[0]));
            if (MaterialOptions.Count > 0) MaterialRows.Add(new WorkCompletionMaterialDraft(MaterialOptions[0]));
            RaiseTotals();
            return;
        }

        foreach (var item in source.Items)
        {
            var estimateItem = SelectedEstimate?.Items.FirstOrDefault(i => i.Id == item.EstimateItemId);
            Works.Add(new WorkCompletionWorkDraft(estimateItem, item.Quantity, item.UnitPrice));
        }

        foreach (var usage in source.MaterialUsages)
        {
            var materialItem = SelectedEstimate?.Items.FirstOrDefault(i => i.Id == usage.EstimateItemId);
            var material = Materials.FirstOrDefault(m => m.Id == usage.MaterialId) ?? materialItem?.Material;
            MaterialRows.Add(new WorkCompletionMaterialDraft(
                materialItem,
                material,
                usage.Quantity,
                usage.UnitPrice,
                alreadyUsed: usage.Quantity));
        }

        RaiseTotals();
    }

    /// <summary>
    /// Готовит акт по одной позиции сметы: работа попадает в таблицу работ, материал —
    /// в таблицу материалов. Так прораб вводит факт прямо из строки сметы.
    /// </summary>
    private void PrefillItem(int estimateItemId)
    {
        var item = SelectedEstimate?.Items.FirstOrDefault(i => i.Id == estimateItemId);
        if (item is null) return;

        Works.Clear();
        MaterialRows.Clear();

        if (item.IsWork)
            Works.Add(new WorkCompletionWorkDraft(item));
        else if (item.MaterialId is not null)
            MaterialRows.Add(new WorkCompletionMaterialDraft(item));

        RaiseTotals();
    }

    private void ResetRows(Estimate estimate)
    {
        Works.Clear();
        MaterialRows.Clear();

        var firstWork = estimate.Items.FirstOrDefault(i => i.IsWork);
        if (firstWork is not null) Works.Add(new WorkCompletionWorkDraft(firstWork));

        var firstMaterial = estimate.Items.FirstOrDefault(i => !i.IsWork && i.MaterialId is not null);
        if (firstMaterial is not null) MaterialRows.Add(new WorkCompletionMaterialDraft(firstMaterial));

        RaiseTotals();
    }

    private void OnWorksChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (WorkCompletionWorkDraft draft in e.NewItems)
                draft.PropertyChanged += OnDraftChanged;

        if (e.OldItems is not null)
            foreach (WorkCompletionWorkDraft draft in e.OldItems)
                draft.PropertyChanged -= OnDraftChanged;

        RaiseTotals();
    }

    private void OnMaterialRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (WorkCompletionMaterialDraft draft in e.NewItems)
                draft.PropertyChanged += OnDraftChanged;

        if (e.OldItems is not null)
            foreach (WorkCompletionMaterialDraft draft in e.OldItems)
                draft.PropertyChanged -= OnDraftChanged;

        RaiseTotals();
    }

    private void OnDraftChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) => RaiseTotals();

    private void RaiseTotals()
    {
        OnPropertyChanged(nameof(WorksCost));
        OnPropertyChanged(nameof(MaterialsCost));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalDisplay));
        OnPropertyChanged(nameof(WorksSummary));
        OnPropertyChanged(nameof(MaterialsSummary));
        OnPropertyChanged(nameof(HasOverStock));
        OnPropertyChanged(nameof(OverStockWarning));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(CanAddWork));
        OnPropertyChanged(nameof(CanAddMaterial));
    }
}
