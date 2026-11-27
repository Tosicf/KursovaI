using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.App.Localization;
using StroyControl.App.Services;
using StroyControl.Core.Dtos;
using StroyControl.Core.Interfaces;
using StroyControl.Core.Services;

namespace StroyControl.App.ViewModels;

/// <summary>Главная панель: KPI, исполнение смет по объектам, остатки и лента операций.</summary>
public sealed partial class DashboardViewModel : ViewModelBase
{
    private readonly IDashboardService _dashboardService;
    private readonly IToastService _toasts;
    private readonly ISessionService _session;
    private readonly IMaterialRepository _materials;
    private readonly ISupplierRepository _suppliers;
    private readonly IProjectRepository _projects;
    private readonly IDeliveryRepository _deliveries;
    private readonly IInventoryService _inventory;
    private readonly IDialogService _dialogs;

    public DashboardViewModel(
        IDashboardService dashboardService,
        IToastService toasts,
        ISessionService session,
        IMaterialRepository materials,
        ISupplierRepository suppliers,
        IProjectRepository projects,
        IDeliveryRepository deliveries,
        IInventoryService inventory,
        IDialogService dialogs)
    {
        _dashboardService = dashboardService;
        _toasts = toasts;
        _session = session;
        _materials = materials;
        _suppliers = suppliers;
        _projects = projects;
        _deliveries = deliveries;
        _inventory = inventory;
        _dialogs = dialogs;

        Period = new PeriodFilterViewModel(ReloadForPeriod);
    }

    public override string Title => L.T("Панель управления");

    /// <summary>
    /// Период панели. Главная страница всегда показывает один месяц (или все месяцы),
    /// поэтому при росте числа объектов она не разрастается.
    /// </summary>
    public PeriodFilterViewModel Period { get; }

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

    /// <summary>Подпись периода для заголовков блоков панели.</summary>
    public string PeriodCaption => Period.FilterCaption;

    /// <summary>Подпись накопленных показателей: бюджет объектов считается на конец месяца.</summary>
    public string BalanceCaption => Period.BalanceCaption;

    /// <summary>Сколько объектов показано и сколько всего попало в выбранный месяц.</summary>
    public string BudgetCaption => Data is null
        ? string.Empty
        : Data.BudgetRowsTotal > Data.ProjectBudgets.Count
            ? $"{BalanceCaption}: показаны {Data.ProjectBudgets.Count} из {Data.BudgetRowsTotal} объектов, остальные — в разделе «Объекты»"
            : $"{BalanceCaption}: {Data.BudgetRowsTotal} объектов";

    [ObservableProperty]
    private DashboardData? data;

    partial void OnDataChanged(DashboardData? value)
    {
        OnPropertyChanged(nameof(DeviationCaption));
        OnPropertyChanged(nameof(PlannedCaption));
        OnPropertyChanged(nameof(ActualCaption));
        OnPropertyChanged(nameof(HasStockAlerts));
        OnPropertyChanged(nameof(MaterialsSummary));
        OnPropertyChanged(nameof(LowStockCaption));
        OnPropertyChanged(nameof(BudgetCaption));
        OnPropertyChanged(nameof(PeriodCaption));
        OnPropertyChanged(nameof(BalanceCaption));
    }

    public bool HasStockAlerts => Data is { StockAlerts.Count: > 0 };

    public string LowStockCaption => Data is null
        ? string.Empty
        : $"{Data.LowStockCount} позиций ниже минимального запаса";

    public string PlannedCaption => $"{BalanceCaption}: план по сметам";

    public string ActualCaption => Data is null
        ? string.Empty
        : $"{BalanceCaption}: {Data.DeviationDirection} {Math.Abs(Data.DeviationPercent):N1} % к смете";

    public string MaterialsSummary => Data is null
        ? string.Empty
        : $"{Data.MaterialsCount} позиций · {Data.UsagesCount} списаний · {Data.DeliveriesCount} поступлений";

    /// <summary>Роль может оформлять поступления (Снабженец, Прораб, Администратор, Инженер).</summary>
    public bool CanPurchase => _session.CanEditDeliveries;

    /// <summary>
    /// Закупка по дефициту: открывает накладную с уже заполненными строками —
    /// материалы ниже минимума и количество до двух минимумов.
    /// </summary>
    [RelayCommand]
    private async Task PurchaseDeficitAsync()
    {
        if (!CanPurchase)
        {
            _toasts.Info("Недостаточно прав", "Оформлять поступления может снабженец, прораб или администратор.");
            return;
        }

        try
        {
            var materials = await _materials.GetAllWithSupplierAsync();
            var deficit = PurchasePlanner.Deficit(materials);

            if (deficit.Count == 0)
            {
                _toasts.Info("Дефицита нет", "Все материалы обеспечены запасом не ниже минимума.");
                return;
            }

            var suppliers = await _suppliers.GetAllAsync();
            var projects = await _projects.GetAllAsync();

            var numbers = await _deliveries.GetNumbersAsync(DateTime.Today);
            var number = DeliveryNumbers.Next(numbers, DateTime.Today);

            var delivery = _dialogs.EditDelivery(null, suppliers, projects, materials, deficit, number);
            if (delivery is null) return;

            var saved = await _inventory.CreateDeliveryAsync(delivery);

            _toasts.Success("Поступление оформлено", $"{saved.Number} · {saved.ItemsCount} поз. · {saved.Total:N2} BYN");
            await LoadAsync();

            // Счёт-фактура по сохранённой накладной: печать, PDF или закрыть.
            var full = await _deliveries.GetWithItemsAsync(saved.Id) ?? saved;
            _dialogs.ShowPurchaseReceipt(full);
        }
        catch (Exception ex)
        {
            _toasts.Error("Не удалось оформить поступление", ex.Message);
        }
    }

    public override async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        var ok = await ExecuteAsync(
            async () => Data = await _dashboardService.LoadAsync(Period.Filter),
            "Не удалось загрузить панель.");
        IsBusy = false;

        // Список месяцев строится по всем документам, поэтому выбор периода не теряется.
        if (Data is not null)
            Period.Rebuild(Data.PeriodMonths.Select(m => (DateTime?)m));

        if (!ok && ErrorMessage is not null)
            _toasts.Error("Панель управления", ErrorMessage);
    }

    /// <summary>Смена месяца в фильтре перезагружает показатели панели.</summary>
    private void ReloadForPeriod()
    {
        OnPropertyChanged(nameof(PeriodCaption));
        OnPropertyChanged(nameof(BalanceCaption));
        OnPropertyChanged(nameof(BudgetCaption));

        if (IsBusy) return;
        _ = LoadAsync();
    }
}
