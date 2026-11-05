using System.ComponentModel;

namespace StroyControl.Core.Enums;

/// <summary>Степень критичности отклонения факта от сметы.</summary>
public enum DeviationSeverity
{
    [Description("В норме")]
    Normal = 0,

    [Description("Внимание")]
    Warning = 1,

    [Description("Критично")]
    Critical = 2
}
