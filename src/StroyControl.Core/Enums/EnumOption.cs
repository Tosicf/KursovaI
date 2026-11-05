namespace StroyControl.Core.Enums;

/// <summary>Пара «значение перечисления — подпись» для выпадающих списков.</summary>
public sealed record EnumOption<TEnum>(TEnum Value, string Display) where TEnum : struct, Enum
{
    public override string ToString() => Display;
}
