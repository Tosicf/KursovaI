using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.App.Services;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;
using StroyControl.Core.Interfaces;

namespace StroyControl.App.ViewModels;

/// <summary>Управление учётными записями сотрудников (доступно администратору).</summary>
public sealed partial class UsersViewModel : ViewModelBase
{
    private readonly IUserService _users;
    private readonly IDialogService _dialogs;
    private readonly IToastService _toasts;
    private readonly ISessionService _session;

    public UsersViewModel(IUserService users, IDialogService dialogs, IToastService toasts, ISessionService session)
    {
        _users = users;
        _dialogs = dialogs;
        _toasts = toasts;
        _session = session;

        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.Filter = FilterItem;
    }

    public override string Title => "Сотрудники";

    public ObservableCollection<User> Items { get; } = new();

    public ICollectionView ItemsView { get; }

    public bool CanManage => _session.CanManageUsers;

    public bool IsEmpty => Items.Count == 0;

    public bool IsFilteredEmpty => Items.Count > 0 && VisibleCount == 0;

    public int TotalCount => Items.Count;

    public int ActiveCount => Items.Count(u => u.IsActive);

    public int AdminsCount => Items.Count(u => u.Role == UserRole.Admin);

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private int visibleCount;

    [ObservableProperty]
    private User? selectedUser;

    partial void OnSearchTextChanged(string value)
    {
        ItemsView.Refresh();
        VisibleCount = ItemsView.Cast<object>().Count();
        OnPropertyChanged(nameof(IsFilteredEmpty));
    }

    public override async Task LoadAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        var ok = await ExecuteAsync(async () =>
        {
            var users = await _users.GetAllAsync();
            Items.Clear();
            foreach (var user in users)
                Items.Add(user);

            VisibleCount = ItemsView.Cast<object>().Count();
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(ActiveCount));
            OnPropertyChanged(nameof(AdminsCount));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(IsFilteredEmpty));
        }, "Не удалось загрузить список сотрудников.");

        IsBusy = false;

        if (!ok && ErrorMessage is not null)
            _toasts.Error("Сотрудники", ErrorMessage);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (!CanManage)
        {
            _toasts.Info("Недостаточно прав", "Управление сотрудниками доступно администратору.");
            return;
        }

        var editor = _dialogs.EditUser(null);
        if (editor is null) return;

        try
        {
            var created = await _users.CreateAsync(
                editor.Login,
                editor.DisplayName,
                editor.SelectedRole.Value,
                editor.Password,
                editor.Email,
                editor.Phone);

            Items.Insert(0, created);
            VisibleCount = ItemsView.Cast<object>().Count();
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(ActiveCount));
            OnPropertyChanged(nameof(AdminsCount));
            _toasts.Success("Сотрудник добавлен", $"{created.DisplayName} · {created.RoleDisplay}");
        }
        catch (Exception ex)
        {
            _toasts.Error("Не удалось создать учётную запись", ex.Message);
        }
    }

    [RelayCommand]
    private async Task EditAsync(User? user)
    {
        if (!CanManage || user is null) return;

        var editor = _dialogs.EditUser(user);
        if (editor is null) return;

        var updated = editor.BuildUser();
        var index = Items.IndexOf(user);
        if (index < 0) return;

        Items[index] = updated;
        OnPropertyChanged(nameof(AdminsCount));

        try
        {
            await _users.UpdateAsync(updated);

            if (!string.IsNullOrWhiteSpace(editor.Password))
            {
                await _users.ResetPasswordAsync(updated.Id, editor.Password);
                _toasts.Success("Данные и пароль обновлены", updated.DisplayName);
            }
            else
            {
                _toasts.Success("Данные сотрудника обновлены", updated.DisplayName);
            }
        }
        catch (Exception ex)
        {
            Items[index] = user;
            OnPropertyChanged(nameof(AdminsCount));
            _toasts.Error("Не удалось сохранить изменения", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(User? user)
    {
        if (!CanManage || user is null) return;

        if (user.Id == _session.CurrentUser?.Id && user.IsActive)
        {
            _toasts.Info("Нельзя заблокировать себя", "Выйдите из системы под другой учётной записью.");
            return;
        }

        var targetState = !user.IsActive;

        try
        {
            await _users.SetActiveAsync(user.Id, targetState);
            user.IsActive = targetState;

            var index = Items.IndexOf(user);
            if (index >= 0) Items[index] = user;

            OnPropertyChanged(nameof(ActiveCount));
            _toasts.Success(targetState ? "Учётная запись разблокирована" : "Учётная запись заблокирована", user.DisplayName);
        }
        catch (Exception ex)
        {
            _toasts.Error("Не удалось изменить статус", ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(User? user)
    {
        if (!CanManage || user is null) return;

        if (user.Id == _session.CurrentUser?.Id)
        {
            _toasts.Info("Нельзя удалить себя", "Выйдите из системы под другой учётной записью.");
            return;
        }

        var confirmed = _dialogs.Confirm(
            "Удалить учётную запись?",
            $"{user.DisplayName} ({user.Login}) потеряет доступ к системе. Оформленные списания сохранятся.",
            "Удалить");

        if (!confirmed) return;

        var index = Items.IndexOf(user);
        Items.Remove(user);
        VisibleCount = ItemsView.Cast<object>().Count();
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(ActiveCount));
        OnPropertyChanged(nameof(AdminsCount));

        try
        {
            await _users.DeleteAsync(user.Id);
            _toasts.Success("Сотрудник удалён", user.DisplayName);
        }
        catch (Exception ex)
        {
            if (index >= 0 && index <= Items.Count)
                Items.Insert(index, user);

            VisibleCount = ItemsView.Cast<object>().Count();
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(ActiveCount));
            OnPropertyChanged(nameof(AdminsCount));
            _toasts.Error("Не удалось удалить сотрудника", ex.Message);
        }
    }

    private bool FilterItem(object item)
    {
        if (item is not User user) return false;
        if (string.IsNullOrWhiteSpace(SearchText)) return true;

        var term = SearchText.Trim();
        return user.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
               || user.Login.Contains(term, StringComparison.OrdinalIgnoreCase)
               || user.RoleDisplay.Contains(term, StringComparison.OrdinalIgnoreCase);
    }
}
