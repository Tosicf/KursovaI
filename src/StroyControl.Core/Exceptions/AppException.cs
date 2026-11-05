namespace StroyControl.Core.Exceptions;

/// <summary>
/// Исключение с сообщением, которое можно показать пользователю без технических подробностей.
/// </summary>
public class AppException : Exception
{
    public AppException(string message) : base(message)
    {
    }

    public AppException(string message, Exception inner) : base(message, inner)
    {
    }
}
