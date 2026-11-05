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

    [Description("Архив")]
    Archived = 3
}
