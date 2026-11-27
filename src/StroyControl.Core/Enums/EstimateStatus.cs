using System.ComponentModel;

namespace StroyControl.Core.Enums;

/// <summary>Состояние сметы.</summary>
public enum EstimateStatus
{
    [Description("Черновик")]
    Draft = 0,

    [Description("На проверке")]
    OnReview = 1,

    [Description("Утверждена")]
    Approved = 2,

    /// <summary>
    /// Утверждённая смета передана в производство: план зафиксирован, по актам
    /// списания собирается факт, и план сравнивается с фактом по затратам.
    /// </summary>
    [Description("В работе")]
    InWork = 3,

    [Description("Архив")]
    Archived = 4
}
