using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StroyControl.Core.Exceptions;

namespace StroyControl.App.ViewModels;

/// <summary>
/// Базовая модель представления страницы: состояние загрузки, сообщение об ошибке
/// и безопасное выполнение асинхронных операций.
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    /// <summary>Заголовок страницы в шапке окна.</summary>
    public virtual string Title => string.Empty;

    /// <summary>Загружает данные страницы (вызывается навигацией и кнопкой «Обновить»).</summary>
    public virtual Task LoadAsync() => Task.CompletedTask;

    /// <summary>Команда кнопки «Обновить» на страницах.</summary>
    [RelayCommand]
    private Task Refresh() => LoadAsync();

    /// <summary>
    /// Выполняет операцию, перехватывая прикладные исключения,
    /// чтобы интерфейс не «падал» при сетевой ошибке или нарушении ограничений БД.
    /// </summary>
    protected async Task<bool> ExecuteAsync(Func<Task> action, string fallbackMessage)
    {
        try
        {
            ErrorMessage = null;
            await action();
            return true;
        }
        catch (AppException ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"{fallbackMessage} {ex.Message}";
            return false;
        }
    }
}
