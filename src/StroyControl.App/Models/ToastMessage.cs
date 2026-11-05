namespace StroyControl.App.Models;

public enum ToastKind
{
    Success,
    Error,
    Info
}

/// <summary>Всплывающее уведомление (результат операции, ошибка сети, подсказка).</summary>
public sealed record ToastMessage(ToastKind Kind, string Title, string Message)
{
    public string KindDisplay => Kind switch
    {
        ToastKind.Success => "Успешно",
        ToastKind.Error => "Ошибка",
        _ => "Информация"
    };
}
