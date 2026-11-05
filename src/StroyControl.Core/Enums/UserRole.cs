using System.ComponentModel;

namespace StroyControl.Core.Enums;

/// <summary>Роль пользователя в системе (разграничение прав доступа).</summary>
public enum UserRole
{
    [Description("Администратор")]
    Admin = 0,

    [Description("Инженер ПТО")]
    Engineer = 1,

    [Description("Снабженец")]
    SupplyManager = 2,

    [Description("Бухгалтер")]
    Accountant = 3,

    [Description("Прораб")]
    Foreman = 4
}
