using CommunityToolkit.Mvvm.ComponentModel;
using StroyControl.Core.Entities;
using StroyControl.Core.Enums;

namespace StroyControl.App.ViewModels.Editors;

public sealed partial class UserEditorViewModel : EditorViewModelBase
{
    private readonly User? _source;

    public UserEditorViewModel(User? source)
    {
        _source = source;
        IsNew = source is null;
        Login = source?.Login ?? string.Empty;
        DisplayName = source?.DisplayName ?? string.Empty;
        Email = source?.Email ?? string.Empty;
        Phone = source?.Phone ?? string.Empty;
        IsActive = source?.IsActive ?? true;
        SelectedRole = Roles.FirstOrDefault(r => r.Value == (source?.Role ?? UserRole.Foreman)) ?? Roles[0];
    }

    public override string Title => IsNew ? "Новый сотрудник" : $"Учётная запись · {_source?.DisplayName}";

    public bool IsNew { get; }

    public IReadOnlyList<EnumOption<UserRole>> Roles { get; } = EnumExtensions.ToOptions<UserRole>();

    [ObservableProperty]
    private string login;

    [ObservableProperty]
    private string displayName;

    [ObservableProperty]
    private string email;

    [ObservableProperty]
    private string phone;

    [ObservableProperty]
    private EnumOption<UserRole> selectedRole;

    [ObservableProperty]
    private bool isActive;

    [ObservableProperty]
    private string password = string.Empty;

    public string PasswordCaption => IsNew
        ? "Пароль (минимум 6 символов)"
        : "Новый пароль (оставьте пустым, чтобы не менять)";

    protected override string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Login) || Login.Trim().Length < 3)
            return "Логин должен содержать не менее 3 символов.";
        if (string.IsNullOrWhiteSpace(DisplayName))
            return "Укажите ФИО сотрудника.";
        if (IsNew && (Password ?? string.Empty).Length < 6)
            return "Для нового сотрудника задайте пароль не короче 6 символов.";
        if (!string.IsNullOrEmpty(Password) && Password.Length < 6)
            return "Пароль должен содержать не менее 6 символов.";
        return null;
    }

    public User BuildUser() => new()
    {
        Id = _source?.Id ?? 0,
        RowVersion = _source?.RowVersion ?? 0,
        CreatedAt = _source?.CreatedAt ?? DateTime.UtcNow,
        Login = Login.Trim(),
        DisplayName = DisplayName.Trim(),
        Role = SelectedRole.Value,
        Email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim(),
        Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
        IsActive = IsActive,
        PasswordHash = _source?.PasswordHash ?? string.Empty,
        PasswordSalt = _source?.PasswordSalt ?? string.Empty,
        LastLoginAt = _source?.LastLoginAt
    };
}
