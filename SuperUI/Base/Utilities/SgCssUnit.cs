// SuperUI/Base/Utilities/SgCssUnit.cs
// Парсер и нормализатор CSS-значений (длин, размеров, углов, времени, цветов).
// Устраняет 50+ копипаст реализаций вроде FixUnit(value) в SgStack/SgSpace/SgDockWindow.

using System.Globalization;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Helper for parsing and normalizing CSS values: lengths, sizes, durations, angles, percentages.
/// </summary>
/// <remarks>
/// <para>Used by SgStack/SgSpace/SgResizable/SgDockWindow and any component that
/// accepts user-supplied CSS values as strings.</para>
/// <para>Round-trip-safe: <c>"16" → "16px" → 16</c> produces a lossless value when
/// the unit is implicit; values with explicit units pass through unchanged.</para>
/// </remarks>
public static class SgCssUnit
{
    /// <summary>
    /// CSS length units. A value that arrives as a bare number is interpreted
    /// as <see cref="LengthUnit.Px"/> by convention (matches browser default for layout values).
    /// </summary>
    public static string EnsureUnit(string? value, string fallbackUnit = "px")
    {
        if (string.IsNullOrWhiteSpace(value)) return "0";
        var v = value.Trim();
        if (v.Length == 0) return "0";
        if (IsAlreadyUnit(v)) return v;
        if (double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
        {
            return v + fallbackUnit;
        }
        return v;
    }

    /// <summary>Appends <paramref name="unit"/> to <paramref name="value"/> if the value is a bare number.</summary>
    public static string? EnsureUnit(string? value, Func<string> unitFactory)
    {
        ArgumentNullException.ThrowIfNull(unitFactory);
        if (string.IsNullOrWhiteSpace(value)) return null;
        var v = value.Trim();
        return IsAlreadyUnit(v) ? v : v + unitFactory();
    }

    /// <summary>
    /// Parses a CSS value like <c>"16px"</c>, <c>"1.5rem"</c>, <c>"50%"</c> and returns
    /// the numeric part as a double, or <paramref name="fallback"/> if parsing fails.
    /// </summary>
    public static double ParsePixels(string? value, double fallback = 0)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        var span = value.AsSpan().Trim();
        var i = 0;
        while (i < span.Length && (char.IsDigit(span[i]) || span[i] is '.' or ',' or '-' or '+' or 'e' or 'E'))
            i++;
        if (i == 0) return fallback;
        var numSpan = span[..i];
        if (double.TryParse(numSpan, NumberStyles.Any, CultureInfo.InvariantCulture, out var n))
        {
            return n;
        }
        return fallback;
    }

    /// <summary>
    /// Multiplies a CSS length by a scale factor. Only applies the scale to
    /// <c>px</c> / unitless numeric values; passes through <c>rem</c>, <c>em</c>, <c>%</c>,
    /// <c>var(...)</c>, <c>calc(...)</c> unchanged.
    /// </summary>
    /// <param name="value">Source CSS value (e.g. <c>"16px"</c>, <c>"1rem"</c>).</param>
    /// <param name="scale">Multiplier (e.g. <c>0.5</c> for compact density).</param>
    /// <param name="invariant">Culture for the result number.</param>
    public static string Scale(string? value, double scale, CultureInfo? invariant = null)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        if (Math.Abs(scale - 1.0) < 0.0001) return value;
        var v = value.Trim();

        // Pass-through patterns.
        if (v.StartsWith("var(", StringComparison.Ordinal) ||
            v.StartsWith("calc(", StringComparison.Ordinal) ||
            v.StartsWith("clamp(", StringComparison.Ordinal) ||
            v.StartsWith("min(",  StringComparison.Ordinal) ||
            v.StartsWith("max(",  StringComparison.Ordinal))
        {
            return v;
        }

        var unit = DetectUnit(v);
        if (unit is "rem" or "em" or "%" or "vh" or "vw" or "vmin" or "vmax" or "ch" or "ex")
        {
            return v;
        }

        var ci = invariant ?? CultureInfo.InvariantCulture;
        var numPart = v[..^unit.Length];
        if (double.TryParse(numPart, NumberStyles.Any, ci, out var n))
        {
            var scaled = n * scale;
            return scaled.ToString("0.##", ci) + unit;
        }
        return v;
    }

    /// <summary>Detects the unit suffix of a CSS value (returns empty string if bare number).</summary>
    public static string DetectUnit(string value)
    {
        var span = value.AsSpan();
        var i = span.Length;
        while (i > 0 && IsUnitChar(span[i - 1])) i--;
        return span[i..].ToString();
    }

    /// <summary>True if <paramref name="value"/> already carries a CSS unit.</summary>
    public static bool IsAlreadyUnit(string value)
    {
        var span = value.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            if (char.IsLetter(span[i]) || span[i] == '%') return true;
        }
        return false;
    }

    private static bool IsUnitChar(char c) =>
        char.IsLetter(c) || c == '%';
}
