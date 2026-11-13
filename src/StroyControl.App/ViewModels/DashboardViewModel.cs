using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

            var delivery = _dialogs.EditDelivery(null, suppliers, projects, materials, deficit);
            if (delivery is null) return;

            var saved = await _inventory.CreateDeliveryAsync(delivery);

            _toasts.Success("Поступление оформлено", $"{saved.Number} · {saved.ItemsCount} поз. · {saved.Total:N2} BYN");
            await LoadAsync();

            // Товарный чек по сохранённой накладной: печать, PDF или закрыть.
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
        var ok = await ExecuteAsync(async () => Data = await _dashboardService.LoadAsync(), "Не удалось загрузить панель.");
        IsBusy = false;

        if (!ok && ErrorMessage is not null)
            _toasts.Error("Панель управления", ErrorMessage);
    }
}
