using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.App.Services;
using StroyControl.App.Startup;
using StroyControl.Core.Enums;
using StroyControl.Core.Interfaces;
using RoleKind = StroyControl.Core.Enums.UserRole;

namespace StroyControl.App.ViewModels;

/// <summary>Настройки клиента: сетевое подключение к MySQL, текущая учётная запись и права.</summary>
public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly IDatabaseHealthService _healthService;
    private readonly IToastService _toasts;

    public SettingsViewModel(
        IDatabaseHealthService healthService,
        ISessionService session,
        AppEnvironment environment,
        IToastService toasts)
    {
        _healthService = healthService;
        _toasts = toasts;
        Environment = environment;
        Session = session;
    }

    public override string Title => "Настройки";

    public AppEnvironment Environment { get; }

    public ISessionService Session { get; }

    public string ServerDisplay => Environment.ServerDisplay;

    public string DatabaseDisplay => Environment.DatabaseDisplay;

    public string ProviderDisplay => Environment.ProviderDisplay;

    public string Organization => Environment.Organization;

    public string Version => Environment.Version;

    public string UserName => Session.CurrentUser?.DisplayName ?? "—";

    public string UserLogin => Session.CurrentUser?.Login ?? "—";

    public string UserRole => Session.CurrentUser?.RoleDisplay ?? "—";

    public string UserLastLogin => Session.CurrentUser?.LastLoginDisplay ?? "—";

    public string UserInitials => Session.CurrentUser?.Initials ?? "—";

    public string RoleDescription => Session.CurrentUser?.Role switch
    {
        RoleKind.Admin => "Полный доступ: справочники, документы, сотрудники и настройки.",
        RoleKind.Engineer => "Сметы, анализ отклонений, справочники и документы.",
        RoleKind.SupplyManager => "Материалы, поставщики и поступления.",
        RoleKind.Foreman => "Списания материалов и контроль остатков по объекту.",
        RoleKind.Accountant => "Просмотр данных и отчётов без изменения документов.",
        _ => "Роль не определена."
    };
    public IReadOnlyList<string> Capabilities
    {
        get
        {
            var list = new List<string> { "Просмотр справочников, смет и документов движения" };

            if (Session.CanEdit)
                list.Add("Создание и редактирование справочников и документов");
            if (Session.CanViewFinance)
                list.Add("Просмотр финансовых показателей и экспорт отчётов");
            if (Session.CanManageUsers)
                list.Add("Управление учётными записями сотрудников");

            return list;
        }
    }

    [ObservableProperty]
    private DatabaseHealth? health;

    [ObservableProperty]
    private bool isChecking;

    public bool IsOnline => Health?.IsAvailable == true;

    public string HealthTitle => Health switch
    {
        null => "Проверка не выполнялась",
        { IsAvailable: true } => "Подключение активно",
        _ => "Сервер недоступен"
    };

    public string HealthMessage => Health?.Message ?? "Нажмите «Проверить подключение», чтобы проверить доступность сервера MySQL.";

    public string UsersCountDisplay => Health is null ? "—" : Health.UsersCount.ToString();

    public string MaterialsCountDisplay => Health is null ? "—" : Health.MaterialsCount.ToString();

    partial void OnHealthChanged(DatabaseHealth? value)
    {
        OnPropertyChanged(nameof(IsOnline));
        OnPropertyChanged(nameof(HealthTitle));
        OnPropertyChanged(nameof(HealthMessage));
        OnPropertyChanged(nameof(UsersCountDisplay));
        OnPropertyChanged(nameof(MaterialsCountDisplay));
    }

    public override async Task LoadAsync()
    {
        if (IsBusy) return;
        await CheckConnectionAsync();
    }

    [RelayCommand]
    private async Task CheckConnectionAsync()
    {
        if (IsChecking) return;

        IsChecking = true;
        var ok = await ExecuteAsync(async () =>
        {
            Health = await _healthService.CheckAsync();
        }, "Не удалось проверить подключение.");

        IsChecking = false;

        if (!ok && ErrorMessage is not null)
            _toasts.Error("Подключение", ErrorMessage);
        else if (Health is not null)
        {
            if (Health.IsAvailable) _toasts.Success("Соединение с MySQL", Health.Message);
            else _toasts.Error("Соединение с MySQL", Health.Message);
        }
    }
}
