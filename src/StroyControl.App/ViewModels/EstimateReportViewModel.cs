using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.Core.Dtos;
using StroyControl.Core.Entities;

namespace StroyControl.App.ViewModels;

/// <summary>
/// Отчёт по смете в форме «Расчёт количества и стоимости работ и материалов»:
/// наценка, прибыль и зарплата рабочих настраиваются прямо в окне отчёта.
/// </summary>
public sealed partial class EstimateReportViewModel : ObservableObject
{
    private readonly Estimate _estimate;

    public EstimateReportViewModel(Estimate estimate)
    {
        _estimate = estimate;
        markupPercent = estimate.MarkupPercent;
        profitPercent = estimate.ProfitPercent;
        laborSharePercent = estimate.LaborSharePercent;
        area = estimate.Project?.TotalArea ?? 0m;

        Report = BuildReport();
    }

    public EstimateReport Report { get; private set; }

    public string Title => $"Отчёт по смете {_estimate.Number}";

    public string EstimateCaption => $"{_estimate.Number} · {_estimate.Name}";

    public string ProjectCaption => string.IsNullOrWhiteSpace(_estimate.Project?.AddressDisplay)
        ? _estimate.ProjectName
        : $"{_estimate.ProjectName} · {_estimate.Project.AddressDisplay}";

    public string CustomerCaption => string.IsNullOrWhiteSpace(_estimate.Project?.Customer)
        ? "заказчик не указан"
        : _estimate.Project!.Customer;

    public string GeneratedCaption => Report.GeneratedAt.ToString("dd.MM.yyyy HH:mm");

    [ObservableProperty]
    private decimal markupPercent;

    [ObservableProperty]
    private decimal profitPercent;

    [ObservableProperty]
    private decimal laborSharePercent;

    [ObservableProperty]
    private decimal area;

    partial void OnMarkupPercentChanged(decimal value) => Rebuild();

    partial void OnProfitPercentChanged(decimal value) => Rebuild();

    partial void OnLaborSharePercentChanged(decimal value) => Rebuild();

    partial void OnAreaChanged(decimal value) => Rebuild();

    /// <summary>Событие пересборки документа (окно перерисовывает FlowDocument).</summary>
    public event EventHandler? ReportChanged;

    /// <summary>Восстанавливает коэффициенты, сохранённые в смете.</summary>
    [RelayCommand]
    private void ResetCoefficients()
    {
        MarkupPercent = _estimate.MarkupPercent;
        ProfitPercent = _estimate.ProfitPercent;
        LaborSharePercent = _estimate.LaborSharePercent;
        Area = _estimate.Project?.TotalArea ?? 0m;
    }

    private void Rebuild()
    {
        Report = BuildReport();
        OnPropertyChanged(nameof(Report));
        OnPropertyChanged(nameof(GeneratedCaption));
        ReportChanged?.Invoke(this, EventArgs.Empty);
    }

    private EstimateReport BuildReport() =>
        EstimateReportBuilder.Build(_estimate, Area, MarkupPercent, ProfitPercent, LaborSharePercent);
}
