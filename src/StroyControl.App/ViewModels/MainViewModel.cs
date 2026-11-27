using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using StroyControl.App.Localization;
using StroyControl.App.Models;
using StroyControl.App.Services;
using StroyControl.App.Startup;
using StroyControl.Core.Interfaces;

namespace StroyControl.App.ViewModels;

/// <summary>Корневая модель представления главного окна: меню, шапка, текущая страница.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly ISessionService _session;
    private readonly IApplicationController _controller;
    private Task _navigationTask = Task.CompletedTask;
    private bool _initialized;

    public MainViewModel(
        IServiceProvider services,
        ISessionService session,
        IApplicationController controller,
        IToastService toasts,
        PageNavigator navigator,
        AppEnvironment environment)
    {
        _services = services;
        _session = session;
        _controller = controller;
        Toasts = toasts;
        Environment = environment;

        IsDarkTheme = ThemeManager.IsDark;
        BuildNavigation();

        // Переключатель показывает текущий язык; смена языка перестраивает оболочку.
        SelectedLanguage = Languages.First(l => l.Language == LocalizationService.Instance.Language);

        // Страница может попросить открыть другой раздел (например, «Объекты» —
        // анализ отклонений по конкретному объекту). Обработчик один: новое главное
        // окно после повторного входа заменяет прежний.
        navigator.Attach(OnNavigationRequested);
    }

    public IToastService Toasts { get; }

    public AppEnvironment Environment { get; }

    public ObservableCollection<NavigationItem> NavigationItems { get; } = new();

    public string UserName => _session.CurrentUser?.DisplayName ?? L.T("Гость");

    /// <summary>Языки интерфейса для переключателя в шапке.</summary>
    public IReadOnlyList<LanguageOption> Languages { get; } = new List<LanguageOption>
    {
        new(AppLanguage.Ru, LocalizationService.NativeName(AppLanguage.Ru)),
        new(AppLanguage.En, LocalizationService.NativeName(AppLanguage.En)),
        new(AppLanguage.By, LocalizationService.NativeName(AppLanguage.By))
    };

    public string UserRole => _session.CurrentUser?.RoleDisplay ?? string.Empty;

    public string UserInitials => _session.CurrentUser?.Initials ?? "—";

    public string Organization => Environment.Organization;

    public bool IsDemoMode => Environment.IsDemoMode;

    public bool CanManageUsers => _session.CanManageUsers;

    public bool IsUsersPage => CurrentViewModel is UsersViewModel;

    public bool IsSettingsPage => CurrentViewModel is SettingsViewModel;

    public string ThemeTooltip => IsDarkTheme ? L.T("Светлая тема") : L.T("Тёмная тема");

    public string PageTitle => CurrentViewModel?.Title ?? L.T("Панель управления");

    public double SidebarWidth => IsSidebarCollapsed ? 88 : 268;

    [ObservableProperty]
    private ViewModelBase? currentViewModel;

    [ObservableProperty]
    private NavigationItem? selectedNavigationItem;

    [ObservableProperty]
    private bool isSidebarCollapsed;

    /// <summary>Текущая тема: true — тёмная. Переключается кнопкой в шапке и запоминается.</summary>
    [ObservableProperty]
    private bool isDarkTheme;

    /// <summary>Выбранный язык интерфейса: смена перестраивает оболочку приложения.</summary>
    [ObservableProperty]
    private LanguageOption? selectedLanguage;

    partial void OnSelectedLanguageChanged(LanguageOption? value)
    {
        if (value is null || value.Language == LocalizationService.Instance.Language) return;

        LocalizationService.Instance.SetLanguage(value.Language);
        _controller.ReloadShell();
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        SelectedNavigationItem = NavigationItems.FirstOrDefault();
        await _navigationTask;
    }

    partial void OnCurrentViewModelChanged(ViewModelBase? value)
    {
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(IsUsersPage));
        OnPropertyChanged(nameof(IsSettingsPage));
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        ThemeManager.Apply(value ? AppTheme.Dark : AppTheme.Light);
        OnPropertyChanged(nameof(ThemeTooltip));
    }

    partial void OnIsSidebarCollapsedChanged(bool value) => OnPropertyChanged(nameof(SidebarWidth));

    partial void OnSelectedNavigationItemChanged(NavigationItem? value)
    {
        if (value is null) return;
        _navigationTask = NavigateToAsync(value);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (CurrentViewModel is null) return;

        await CurrentViewModel.LoadAsync();
        Toasts.Info(L.T("Данные обновлены"), CurrentViewModel.Title);
    }

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;

    /// <summary>Переключение светлой и тёмной темы (кнопка в верхней панели).</summary>
    [RelayCommand]
    private void ToggleTheme() => IsDarkTheme = !IsDarkTheme;

    /// <summary>Раздел «Сотрудники» открывается из шапки и доступен только администратору.</summary>
    [RelayCommand]
    private async Task OpenUsersAsync()
    {
        if (!CanManageUsers)
        {
            Toasts.Info(L.T("Недостаточно прав"), L.T("Раздел доступен только администратору."));
            return;
        }

        await ShowStandaloneAsync<UsersViewModel>();
    }

    /// <summary>Раздел «О программе» открывается из шапки.</summary>
    [RelayCommand]
    private Task OpenSettingsAsync() => ShowStandaloneAsync<SettingsViewModel>();

    private async Task ShowStandaloneAsync<TPage>() where TPage : ViewModelBase
    {
        try
        {
            SelectedNavigationItem = null;
            var page = _services.GetRequiredService<TPage>();
            CurrentViewModel = page;
            await page.LoadAsync();
        }
        catch (Exception ex)
        {
            Toasts.Error(L.T("Ошибка навигации"), ex.Message);
        }
    }

    [RelayCommand]
    private void Logout() => _controller.Logout();

    private void BuildNavigation()
    {
        NavigationItems.Add(CreateItem<DashboardViewModel>("dashboard", L.T("Панель управления"), "IconDashboard"));
        NavigationItems.Add(CreateItem<MaterialsViewModel>("materials", L.T("Материалы"), "IconMaterials"));
        NavigationItems.Add(CreateItem<SuppliersViewModel>("suppliers", L.T("Поставщики"), "IconSuppliers"));
        NavigationItems.Add(CreateItem<ProjectsViewModel>("projects", L.T("Объекты"), "IconProjects"));
        NavigationItems.Add(CreateItem<EstimatesViewModel>("estimates", L.T("Сметы"), "IconEstimates"));
        NavigationItems.Add(CreateItem<DeliveriesViewModel>("deliveries", L.T("Поступления"), "IconIncoming"));
        NavigationItems.Add(CreateItem<UsagesViewModel>("usages", L.T("Списания"), "IconOutgoing"));
        NavigationItems.Add(CreateItem<DeviationsViewModel>("deviations", L.T("Анализ отклонений"), "IconDeviations"));
        NavigationItems.Add(CreateItem<ArchiveViewModel>("archive", L.T("Архив смет"), "IconArchive"));

        // «Сотрудники» и «О программе» вынесены в верхнюю панель (см. разметку MainWindow).
    }

    private NavigationItem CreateItem<TPage>(string key, string title, string iconKey)
        where TPage : ViewModelBase
    {
        var icon = Application.Current?.TryFindResource(iconKey) as Geometry;
        return new NavigationItem(key, title, icon, () => ShowPageAsync<TPage>());
    }

    private async Task ShowPageAsync<TPage>() where TPage : ViewModelBase
    {
        var page = _services.GetRequiredService<TPage>();
        CurrentViewModel = page;
        await page.LoadAsync();
    }

    /// <summary>Переход в раздел, запрошенный страницей: пункт меню выбирается программно.</summary>
    private void OnNavigationRequested(string pageKey)
    {
        var item = NavigationItems.FirstOrDefault(i => i.Key == pageKey);
        if (item is null) return;

        // Раздел уже открыт — просто перечитываем данные, иначе сеттер не сработает.
        if (ReferenceEquals(SelectedNavigationItem, item))
            _navigationTask = NavigateToAsync(item);
        else
            SelectedNavigationItem = item;
    }

    private async Task NavigateToAsync(NavigationItem item)
    {
        if (item.Activate is null) return;

        try
        {
            await item.Activate();
        }
        catch (Exception ex)
        {
            Toasts.Error(L.T("Ошибка навигации"), ex.Message);
        }
    }
}

/// <summary>Пункт переключателя языка: язык и его название на самом языке.</summary>
public sealed record LanguageOption(AppLanguage Language, string Name);
