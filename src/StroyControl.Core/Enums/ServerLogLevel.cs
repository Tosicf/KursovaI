namespace StroyControl.Core.Enums;

/// <summary>Уровень записи журнала серверной части.</summary>
public enum ServerLogLevel
{
    /// <summary>Технические подробности (например, выбор провайдера базы).</summary>
    Debug = 0,

    /// <summary>Обычная операция: подключение, запрос, сохранение.</summary>
    Info = 1,

    /// <summary>Операция выполнена с замечанием или отклонена бизнес-правилом.</summary>
    Warning = 2,

    /// <summary>Ошибка выполнения на серверной стороне.</summary>
    Error = 3
}
