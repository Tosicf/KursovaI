using System.ComponentModel;

namespace StroyControl.Core.Enums;

/// <summary>Единица измерения строительного материала.</summary>
public enum UnitOfMeasure
{
    [Description("шт")]
    Piece = 0,

    [Description("кг")]
    Kilogram = 1,

    [Description("т")]
    Ton = 2,

    [Description("м³")]
    CubicMeter = 3,

    [Description("м²")]
    SquareMeter = 4,

    [Description("п.м")]
    LinearMeter = 5,

    [Description("мешок")]
    Bag = 6,

    [Description("лист")]
    Sheet = 7,

    [Description("рулон")]
    Roll = 8,

    [Description("л")]
    Liter = 9
}
