using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.App.Localization;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;

namespace StroyControl.App.ViewModels.Editors;

/// <summary>
/// Акт выполненных работ: шапка документа, строки работ по позициям сметы и материалы,
/// израсходованные на эти работы. Диалог только проверяет данные — остатки склада,
/// транзакцию и правило «смета в работе» проверяет сервис при сохранении.
/// </summary>
public sealed partial class WorkCompletionEditorViewModel : EditorViewModelBase
{
    private readonly WorkCompletion? _source;

    /// <summary>Пока идёт заполнение формы, смена сметы не должна пересобирать строки.</summary>
    private bool _loading = true;

    public WorkCompletionEditorViewModel(
        WorkCompletion? source,
        IReadOnlyList<Estimate> estimates,
        IReadOnlyList<Material> materials,
        int? currentUserId)
    {
        _source = source;
        Materials = materials;
        CurrentUserId = currentUserId;

        // Факт учитывается только по сметам в работе — остальные в выбор не попадают.
        Estimates = estimates.Where(e => e.Status == EstimateStatus.InWork).ToList();

        Works.CollectionChanged += OnWorksChanged;
        MaterialRows.CollectionChanged += OnMaterialRowsChanged;

        SelectedEstimate = Estimates.FirstOrDefault(e => e.Id == source?.EstimateId) ?? Estimates.FirstOrDefault();
        Date = source?.Date ?? DateTime.Today;
        DocumentNumber = source?.DocumentNumber ?? $"АВР-{DateTime.Now:yy}-{DateTime.Now:MMdd}";
        Note = source?.Note;

        FillRows(source);
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
    }

    [RelayCommand]
    private void AddWork()
    {
        var used = Works.Select(w => w.EstimateItem?.Id).ToHashSet();
        var next = WorkOptions.FirstOrDefault(i => !used.Contains(i.Id)) ?? WorkOptions.FirstOrDefault();

        if (next is null)
        {
            ErrorMessage = L.T("В выбранной смете нет позиций-работ: добавьте их в смету.");
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
        var used = MaterialRows.Select(m => m.MaterialItem?.Id).ToHashSet();
        var next = MaterialOptions.FirstOrDefault(i => !used.Contains(i.Id)) ?? MaterialOptions.FirstOrDefault();

        if (next is null)
        {
            ErrorMessage = L.T("В выбранной смете нет материалов: добавьте их в смету.");
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

        if (Date.Date > MaxDate)
            return L.T("Дата акта не может быть в будущем.");

        if (Works.All(w => w.Quantity <= 0m) && MaterialRows.All(m => m.Quantity <= 0m))
            return L.T("В акте нет ни выполненных работ, ни израсходованных материалов.");

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
    }
}
