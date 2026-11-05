using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using StroyControl.App.Models;

namespace StroyControl.App.Services;

public sealed class ToastService : IToastService
{
    private const int MaxVisible = 3;

    public ObservableCollection<ToastMessage> Toasts { get; } = new();

    public void Success(string title, string message) => Show(new ToastMessage(ToastKind.Success, title, message));

    public void Error(string title, string message) => Show(new ToastMessage(ToastKind.Error, title, message));

    public void Info(string title, string message) => Show(new ToastMessage(ToastKind.Info, title, message));

    public void Show(ToastMessage message)
    {
        var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        void Add()
        {
            Toasts.Insert(0, message);
            while (Toasts.Count > MaxVisible)
                Toasts.RemoveAt(Toasts.Count - 1);
        }

        if (dispatcher.CheckAccess()) Add();
        else dispatcher.Invoke(Add);

        _ = RemoveLaterAsync(message, dispatcher);
    }

    private async Task RemoveLaterAsync(ToastMessage message, Dispatcher dispatcher)
    {
        await Task.Delay(TimeSpan.FromSeconds(message.Kind == ToastKind.Error ? 7 : 4));
        await dispatcher.InvokeAsync(() => Toasts.Remove(message));
    }
}
