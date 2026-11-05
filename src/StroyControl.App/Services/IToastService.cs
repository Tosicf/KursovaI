using System.Collections.ObjectModel;
using StroyControl.App.Models;

namespace StroyControl.App.Services;

/// <summary>Всплывающие уведомления о результате операций.</summary>
public interface IToastService
{
    ObservableCollection<ToastMessage> Toasts { get; }

    void Success(string title, string message);

    void Error(string title, string message);

    void Info(string title, string message);

    void Show(ToastMessage message);
}
