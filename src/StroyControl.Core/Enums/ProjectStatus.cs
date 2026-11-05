using System.ComponentModel;

namespace StroyControl.Core.Enums;

/// <summary>Жизненный цикл строительного объекта.</summary>
public enum ProjectStatus
{
    [Description("Проектирование")]
    Planning = 0,

    [Description("В работе")]
    InProgress = 1,

    [Description("Приостановлен")]
    Suspended = 2,

    [Description("Завершён")]
    Completed = 3
}
