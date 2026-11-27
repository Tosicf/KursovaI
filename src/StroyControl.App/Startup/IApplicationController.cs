namespace StroyControl.App.Startup;

/// <summary>Управление жизненным циклом окон: вход, выход, повторный вход.</summary>
public interface IApplicationController
{
    void ShowLoginAndStart();

    void Logout();

    void OnMainWindowClosed();

    /// <summary>Перестраивает главное окно (например, после смены языка интерфейса).</summary>
    void ReloadShell();
}
