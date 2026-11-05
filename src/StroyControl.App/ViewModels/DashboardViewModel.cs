using CommunityToolkit.Mvvm.ComponentModel;
using StroyControl.App.Services;
using StroyControl.Core.Dtos;
using StroyControl.Core.Interfaces;

namespace StroyControl.App.ViewModels;

/// <summary>Главная панель: KPI, исполнение смет по объектам, остатки и лента операций.</summary>
public sealed partial class DashboardViewModel : ViewModelBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IToastService _toasts;
    private readonly ISessionService _session;

    public DashboardViewModel(IDashboardService dashboardService, IToastService toasts, ISessionService session)
    {
        _dashboardService = dashboardService;
        _toasts = toasts;
        _session = session;
    }

    public override string Title => "Панель управления";

    public string Greeting
    {
        get
        {
            var hour = DateTime.Now.Hour;
            var hello = hour switch
            {
                < 6 => "Доброй ночи",
                < 12 => "Доброе утро",
                < 18 => "Добрый день",
                _ => "Добрый вечер"
            };
            var name = _session.CurrentUser?.DisplayName.Split(' ').Skip(1).FirstOrDefault() ?? "коллега";
            return $"{hello}, {name}!";
        }
    }

    public string TodayDisplay => DateTime.Now.ToString("dddd, d MMMM yyyy");

    public string DeviationCaption => Data is null
        ? string.Empty
        : $"факт против сметы: {Data.DeviationDirection}";

    [ObservableProperty]
    private DashboardData? data;

    partial void OnDataChanged(DashboardData? value)
    {
        OnPropertyChanged(nameof(DeviationCaption));
        OnPropertyChanged(nameof(HasStockAlerts));
        OnPropertyChanged(nameof(MaterialsSummary));
        OnPropertyChanged(nameof(LowStockCaption));
        OnPropertyChanged(nameof(ActualCaption));
        OnPropertyChanged(nameof(PlannedCaption));
    }

    public bool HasStockAlerts => Data is { StockAlerts.Count: > 0 };

    public string LowStockCaption => Data is null
        ? string.Empty
        : $"{Data.LowStockCount} позиций ниже минимального запаса";

    public string PlannedCaption => "сметная стоимость материалов";

    public string ActualCaption => Data is null
        ? string.Empty
        : $"{Data.DeviationDirection} {Math.Abs(Data.DeviationPercent):N1} % к смете";

    public string MaterialsSummary => Data is null
        ? string.Empty
        : $"{Data.MaterialsCount} позиций · {Data.UsagesCount} списаний · {Data.DeliveriesCount} поступлений";

    public override async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        var ok = await ExecuteAsync(async () => Data = await _dashboardService.LoadAsync(), "Не удалось загрузить панель.");
        IsBusy = false;

        if (!ok && ErrorMessage is not null)
            _toasts.Error("Панель управления", ErrorMessage);
    }
}
