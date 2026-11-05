using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
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
        AppEnvironment environment)
    {
        _services = services;
        _session = session;
        _controller = controller;
        Toasts = toasts;
        Environment = environment;

        BuildNavigation();
    }

    public IToastService Toasts { get; }

    public AppEnvironment Environment { get; }

    public ObservableCollection<NavigationItem> NavigationItems { get; } = new();

    public string UserName => _session.CurrentUser?.DisplayName ?? "Гость";

    public string UserRole => _session.CurrentUser?.RoleDisplay ?? string.Empty;

    public string UserInitials => _session.CurrentUser?.Initials ?? "—";

    public string Organization => Environment.Organization;

    public bool IsDemoMode => Environment.IsDemoMode;

    public bool CanManageUsers => _session.CanManageUsers;

    public string PageTitle => CurrentViewModel?.Title ?? "Панель управления";

    public double SidebarWidth => IsSidebarCollapsed ? 88 : 268;

    [ObservableProperty]
    private ViewModelBase? currentViewModel;

    [ObservableProperty]
    private NavigationItem? selectedNavigationItem;

    [ObservableProperty]
    private bool isSidebarCollapsed;

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        SelectedNavigationItem = NavigationItems.FirstOrDefault();
        await _navigationTask;
    }

    partial void OnCurrentViewModelChanged(ViewModelBase? value) => OnPropertyChanged(nameof(PageTitle));

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
        Toasts.Info("Данные обновлены", CurrentViewModel.Title);
    }

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;

    [RelayCommand]
    private void Logout() => _controller.Logout();

    private void BuildNavigation()
    {
        NavigationItems.Add(CreateItem<DashboardViewModel>("dashboard", "Панель управления", "IconDashboard"));
        NavigationItems.Add(CreateItem<MaterialsViewModel>("materials", "Материалы", "IconMaterials"));
        NavigationItems.Add(CreateItem<SuppliersViewModel>("suppliers", "Поставщики", "IconSuppliers"));
        NavigationItems.Add(CreateItem<ProjectsViewModel>("projects", "Объекты", "IconProjects"));
        NavigationItems.Add(CreateItem<EstimatesViewModel>("estimates", "Сметы", "IconEstimates"));
        NavigationItems.Add(CreateItem<DeliveriesViewModel>("deliveries", "Поступления", "IconIncoming"));
        NavigationItems.Add(CreateItem<UsagesViewModel>("usages", "Списания", "IconOutgoing"));
        NavigationItems.Add(CreateItem<DeviationsViewModel>("deviations", "Анализ отклонений", "IconDeviations"));

        if (_session.CanManageUsers)
            NavigationItems.Add(CreateItem<UsersViewModel>("users", "Сотрудники", "IconUsers"));

        NavigationItems.Add(CreateItem<SettingsViewModel>("settings", "Настройки", "IconSettings"));
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

    private async Task NavigateToAsync(NavigationItem item)
    {
        if (item.Activate is null) return;

        try
        {
            await item.Activate();
        }
        catch (Exception ex)
        {
            Toasts.Error("Ошибка навигации", ex.Message);
        }
    }
}
