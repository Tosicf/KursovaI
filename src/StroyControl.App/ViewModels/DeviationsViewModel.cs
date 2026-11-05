using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.App.Services;
using StroyControl.Core.Dtos;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;
using StroyControl.Core.Interfaces;
using StroyControl.Core.Services;

namespace StroyControl.App.ViewModels;

public sealed record ProjectFilterOption(int? Id, string Name)
{
    public override string ToString() => Name;
}

/// <summary>
/// Анализ отклонений факта от сметы: сводка по объекту, таблица отклонений
/// по количеству/цене/стоимости и экспорт отчёта в CSV.
/// </summary>
public sealed partial class DeviationsViewModel : ViewModelBase
{
    private readonly IDeviationService _deviationService;
    private readonly IProjectRepository _projects;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly ISessionService _session;
    private readonly CsvReportExporter _exporter = new();
    private bool _initialized;

    public DeviationsViewModel(
        IDeviationService deviationService,
        IProjectRepository projects,
        IDialogService dialogs,
        IToastService toasts,
        ISessionService session)
    {
        _deviationService = deviationService;
        _projects = projects;
        _dialogs = dialogs;
        _toasts = toasts;
        _session = session;

        RowsView = CollectionViewSource.GetDefaultView(Rows);
        RowsView.Filter = FilterRow;

        FilterOptions.Add(AllSeverities);
        FilterOptions.Add("Критичные (≥10 %)");
        FilterOptions.Add("Внимание (5–10 %)");
        FilterOptions.Add("В норме (<5 %)");
        FilterOptions.Add("Только перерасход");
        FilterOptions.Add("Только экономия");

        ProjectOptions.Add(new ProjectFilterOption(null, "Все объекты"));
        SelectedProject = ProjectOptions[0];
    }

    public const string AllSeverities = "Все отклонения";

    public override string Title => "Анализ отклонений";

    public ObservableCollection<ProjectFilterOption> ProjectOptions { get; } = new();

    public ObservableCollection<string> FilterOptions { get; } = new();

    public ObservableCollection<DeviationRow> Rows { get; } = new();

    public ICollectionView RowsView { get; }

    public bool CanExport => _session.CanViewFinance;

    public bool IsEmpty => Report is null || Report.Rows.Count == 0;

    public bool IsFilteredEmpty => Report is not null && Report.Rows.Count > 0 && VisibleCount == 0;

    public string ScopeName => Report?.ScopeName ?? "Все объекты";

    public string PlannedTotalDisplay => (Report?.PlannedTotal ?? 0m).ToString("N2") + " BYN";

    public string ActualTotalDisplay => (Report?.ActualTotal ?? 0m).ToString("N2") + " BYN";

    public string DeviationDisplay => Report is null
        ? "—"
        : (Report.CostDeviation >= 0 ? "+" : string.Empty) + Report.CostDeviation.ToString("N2") + " BYN";

    public string DeviationPercentDisplay => Report is null ? "—" : $"{Report.DeviationPercent:+0.0;-0.0;0} %";

    public string DeviationDirection => Report is null
        ? string.Empty
        : Report.CostDeviation switch
        {
            > 0m => "перерасход против сметы",
            < 0m => "экономия против сметы",
            _ => "соответствие смете"
        };

    public int CriticalCount => Report?.CriticalCount ?? 0;

    public int WarningCount => Report?.WarningCount ?? 0;

    public int OverrunCount => Report?.OverrunCount ?? 0;

    public int EconomyCount => Report?.EconomyCount ?? 0;

    public int PositionsCount => Report?.Rows.Count ?? 0;

    public string SeverityDisplay => Report?.Severity.ToDisplayName() ?? "Нет данных";

    public DeviationSeverity Severity => Report?.Severity ?? DeviationSeverity.Normal;

    public string RowsSummary => Report is null
        ? string.Empty
        : $"показано {VisibleCount} из {Report.Rows.Count} позиций";

    [ObservableProperty]
    private DeviationReport? report;

    [ObservableProperty]
    private ProjectFilterOption? selectedProject;

    [ObservableProperty]
    private string selectedFilter = AllSeverities;

    [ObservableProperty]
    private int visibleCount;

    [ObservableProperty]
    private DeviationRow? selectedRow;

    partial void OnSelectedProjectChanged(ProjectFilterOption? value)
    {
        if (value is null || !_initialized) return;
        _ = ReloadReportAsync();
    }

    partial void OnSelectedFilterChanged(string value) => ApplyFilter();

    partial void OnReportChanged(DeviationReport? value)
    {
        Rows.Clear();
        if (value is not null)
        {
            foreach (var row in value.Rows)
                Rows.Add(row);
        }

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(ScopeName));
        OnPropertyChanged(nameof(PlannedTotalDisplay));
        OnPropertyChanged(nameof(ActualTotalDisplay));
        OnPropertyChanged(nameof(DeviationDisplay));
        OnPropertyChanged(nameof(DeviationPercentDisplay));
        OnPropertyChanged(nameof(DeviationDirection));
        OnPropertyChanged(nameof(CriticalCount));
        OnPropertyChanged(nameof(WarningCount));
        OnPropertyChanged(nameof(OverrunCount));
        OnPropertyChanged(nameof(EconomyCount));
        OnPropertyChanged(nameof(PositionsCount));
        OnPropertyChanged(nameof(Severity));
        OnPropertyChanged(nameof(SeverityDisplay));

        ApplyFilter();
    }

    public override async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        var ok = await ExecuteAsync(async () =>
        {
            if (ProjectOptions.Count <= 1)
            {
                var projects = await _projects.GetAllWithEstimatesAsync();
                var selectedId = SelectedProject?.Id;

                ProjectOptions.Clear();
                ProjectOptions.Add(new ProjectFilterOption(null, "Все объекты"));
                foreach (var project in projects)
                    ProjectOptions.Add(new ProjectFilterOption(project.Id, project.Name));

                SelectedProject = ProjectOptions.FirstOrDefault(p => p.Id == selectedId) ?? ProjectOptions[0];
            }

            Report = await _deviationService.BuildReportAsync(SelectedProject?.Id);
            _initialized = true;
        }, "Не удалось построить отчёт об отклонениях.");

        IsBusy = false;

        if (!ok && ErrorMessage is not null)
            _toasts.Error("Анализ отклонений", ErrorMessage);
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (Report is null) return;

        var path = _dialogs.SaveFile(
            "Сохранить отчёт об отклонениях",
            _exporter.DefaultFileName(Report),
            "CSV-файл (*.csv)|*.csv|Все файлы (*.*)|*.*");

        if (path is null) return;

        var ok = await ExecuteAsync(
            () => _exporter.ExportDeviationReportAsync(Report, path),
            "Не удалось сохранить отчёт.");

        if (ok)
            _toasts.Success("Отчёт сохранён", Path.GetFileName(path));
        else if (ErrorMessage is not null)
            _toasts.Error("Экспорт", ErrorMessage);
    }

    private async Task ReloadReportAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        var ok = await ExecuteAsync(
            async () => Report = await _deviationService.BuildReportAsync(SelectedProject?.Id),
            "Не удалось обновить отчёт.");
        IsBusy = false;

        if (!ok && ErrorMessage is not null)
            _toasts.Error("Анализ отклонений", ErrorMessage);
    }

    private bool FilterRow(object item)
    {
        if (item is not DeviationRow row) return false;

        return SelectedFilter switch
        {
            "Критичные (≥10 %)" => row.Severity == DeviationSeverity.Critical,
            "Внимание (5–10 %)" => row.Severity == DeviationSeverity.Warning,
            "В норме (<5 %)" => row.Severity == DeviationSeverity.Normal,
            "Только перерасход" => row.CostDeviation > 0m,
            "Только экономия" => row.CostDeviation < 0m,
            _ => true
        };
    }

    private void ApplyFilter()
    {
        RowsView.Refresh();
        VisibleCount = RowsView.Cast<object>().Count();
        OnPropertyChanged(nameof(VisibleCount));
        OnPropertyChanged(nameof(IsFilteredEmpty));
        OnPropertyChanged(nameof(RowsSummary));
    }
}
