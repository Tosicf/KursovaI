using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;

namespace StroyControl.Core.Enums;

/// <summary>Человекочитаемые названия значений перечислений (для UI и отчётов).</summary>
public static class EnumExtensions
{
    private static readonly ConcurrentDictionary<Enum, string> Cache = new();

    public static string ToDisplayName(this Enum value)
    {
        return Cache.GetOrAdd(value, static v =>
        {
            var field = v.GetType().GetField(v.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? v.ToString();
        });
    }

    public static IReadOnlyList<EnumOption<TEnum>> ToOptions<TEnum>() where TEnum : struct, Enum
    {
        return System.Enum.GetValues<TEnum>()
            .Select(v => new EnumOption<TEnum>(v, v.ToDisplayName()))
            .ToList();
    }
}
