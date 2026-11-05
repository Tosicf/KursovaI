namespace StroyControl.App.Startup;

/// <summary>Управление жизненным циклом окон: вход, выход, повторный вход.</summary>
public interface IApplicationController
{
    void ShowLoginAndStart();

    void Logout();

    void OnMainWindowClosed();
}
