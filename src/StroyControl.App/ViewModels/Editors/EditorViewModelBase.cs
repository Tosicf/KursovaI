using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StroyControl.App.ViewModels.Editors;

/// <summary>
/// Базовая модель диалога редактирования. Диалог только проверяет введённые данные
/// и возвращает результат; сохранение в БД выполняет страница — так проще
/// реализовать оптимистичное обновление списка с откатом при ошибке.
/// </summary>
public abstract partial class EditorViewModelBase : ObservableObject
{
    [ObservableProperty]
    private string? errorMessage;

    public event EventHandler<bool>? CloseRequested;

    public abstract string Title { get; }

    public virtual string SaveText => "Сохранить";

    public virtual double PreferredWidth => 620;

    public virtual double ContentMaxHeight => 540;

    /// <summary>Проверка введённых данных. null — данные корректны.</summary>
    protected abstract string? Validate();

    [RelayCommand]
    private void Save()
    {
        var error = Validate();
        if (error is not null)
        {
            ErrorMessage = error;
            return;
        }

        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
