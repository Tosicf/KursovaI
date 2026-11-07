using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.App.Startup;
using StroyControl.Core.Interfaces;

namespace StroyControl.App.ViewModels;

public sealed record DemoAccount(string Login, string Password, string DisplayName, string Role);

/// <summary>Аутентификация пользователя по учётной записи в общей базе SQL Server.</summary>
public sealed partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly ISessionService _session;

    public LoginViewModel(IAuthService authService, ISessionService session, AppEnvironment environment)
    {
        _authService = authService;
        _session = session;
        Environment = environment;
        Login = "admin";
        Password = "admin123";
    }

    public override string Title => "Вход в систему";

    public AppEnvironment Environment { get; }

    public IReadOnlyList<DemoAccount> DemoAccounts { get; } = new List<DemoAccount>
    {
        new("admin", "admin123", "Иванов А. П.", "Администратор"),
        new("smeta", "smeta123", "Ковалёва Е. В.", "Инженер ПТО"),
        new("snab", "snab123", "Романов Д. О.", "Снабженец"),
        new("prorab", "prorab123", "Дорошевич С. К.", "Прораб"),
        new("buh", "buh123", "Лебедева Н. С.", "Бухгалтер")
    };

    [ObservableProperty]
    private string login;

    [ObservableProperty]
    private string password;

    [ObservableProperty]
    private string? errorText;

    public event EventHandler? SignedIn;

    [RelayCommand]
    private async Task SignInAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        ErrorText = null;

        try
        {
            var result = await _authService.SignInAsync(Login, Password);
            if (!result.Success || result.User is null)
            {
                ErrorText = result.Error ?? "Не удалось выполнить вход.";
                return;
            }

            _session.SignIn(result.User);
            SignedIn?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ErrorText = $"Сервер SQL Server недоступен. Проверьте подключение. ({ex.Message})";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void UseDemoAccount(string? login)
    {
        var account = DemoAccounts.FirstOrDefault(a => a.Login == login);
        if (account is null) return;

        Login = account.Login;
        Password = account.Password;
        ErrorText = null;
    }
}
