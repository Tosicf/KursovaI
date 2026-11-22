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
/// Вкладка «Архив смет»: завершённые и снятые с учёта сметы. Изменять их нельзя —
/// доступны только просмотр состава, отчёт и выгрузка. Сметы попадают сюда
/// командой «В архив» на странице «Сметы» после подтверждения.
/// </summary>
public sealed partial class ArchiveViewModel : ViewModelBase
{
    private readonly IEstimateRepository _estimates;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly ISessionService _session;

    public ArchiveViewModel(
        IEstimateRepository estimates,
        IDialogService dialogs,
        IToastService toasts,
        ISessionService session)
    {
        _estimates = estimates;
        _dialogs = dialogs;
        _toasts = toasts;
        _session = session;

        EstimatesView = CollectionViewSource.GetDefaultView(Estimates);
        EstimatesView.Filter = Filter;
    }

    public override string Title => "Архив смет";

    public ObservableCollection<Estimate> Estimates { get; } = new();

    public ICollectionView EstimatesView { get; }

    /// <summary>Отчёт содержит финансовые показатели.</summary>
    public bool CanViewReport => _session.CanViewFinance;

    public bool IsEmpty => Estimates.Count == 0;

    public bool IsFilteredEmpty => Estimates.Count > 0 && VisibleCount == 0;

    public bool HasSelection => SelectedEstimate is not null;

    public int VisibleCount => EstimatesView.Cast<object>().Count();

    /// <summary>Суммарная плановая стоимость всех архивных смет.</summary>
    public string TotalDisplay => Estimates.Sum(e => e.PlannedTotal).ToString("N2") + " BYN";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private Estimate? selectedEstimate;

    partial void OnSearchTextChanged(string value)
    {
        EstimatesView.Refresh();
        RaiseTotals();
    }

    partial void OnSelectedEstimateChanged(Estimate? value) => OnPropertyChanged(nameof(HasSelection));

    public override async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        var ok = await ExecuteAsync(async () =>
        {
            var archived = await _estimates.GetArchivedAsync();
            var previousId = SelectedEstimate?.Id;

            Estimates.Clear();
            foreach (var estimate in archived)
                Estimates.Add(estimate);

            EstimatesView.Refresh();
            SelectedEstimate = Estimates.FirstOrDefault(e => e.Id == previousId) ?? Estimates.FirstOrDefault();
            RaiseTotals();
        }, "Не удалось загрузить архив смет.");

        IsBusy = false;

        if (!ok && ErrorMessage is not null)
            _toasts.Error("Архив смет", ErrorMessage);
    }

    /// <summary>Печатный отчёт по архивной смете: только чтение, без изменения данных.</summary>
    [RelayCommand]
    private void ShowReport(Estimate? estimate)
    {
        var target = estimate ?? SelectedEstimate;
        if (target is null)
        {
            _toasts.Info("Смета не выбрана", "Выберите смету в архиве.");
            return;
        }

        if (!CanViewReport)
        {
            _toasts.Info("Недостаточно прав", "Отчёт доступен ролям с доступом к финансовым показателям.");
            return;
        }

        _dialogs.ShowEstimateReport(target);
    }

    private bool Filter(object item)
    {
        if (item is not Estimate estimate) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var term = SearchText.Trim();
        return estimate.Number.Contains(term, StringComparison.OrdinalIgnoreCase)
               || estimate.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
               || estimate.ProjectName.Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private void RaiseTotals()
    {
        OnPropertyChanged(nameof(TotalDisplay));
        OnPropertyChanged(nameof(VisibleCount));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsFilteredEmpty));
        OnPropertyChanged(nameof(HasSelection));
    }
}
