// SuperUI/Base/Utilities/SgEnumExtensions.cs

using System.ComponentModel;
using System.Reflection;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Расширения для работы с enum в компонентах UI.
/// </summary>
public static class SgEnumExtensions
{
    /// <summary>
    /// Получить Description из [Description("...")] атрибута или ToString().
    /// </summary>
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attr = field?.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? value.ToString();
    }

    /// <summary>
    /// Получить все значения enum как список (value, label).
    /// </summary>
    public static IReadOnlyList<(TEnum Value, string Label)> GetOptions<TEnum>()
        where TEnum : struct, Enum
    {
        return Enum.GetValues<TEnum>()
            .Select(v => (v, ((Enum)(object)v).GetDescription()))
            .ToList();
    }

    /// <summary>
    /// Проверить наличие [Obsolete] атрибута (для скрытия в UI).
    /// </summary>
    public static bool IsObsolete(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        return field?.GetCustomAttribute<ObsoleteAttribute>() != null;
    }
}
