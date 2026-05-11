using System.Globalization;
using System.Text;

namespace SuperUI.Core;

/// <summary>
/// Fluent builder for composing inline <c>style</c> strings. Mirrors <see cref="CssBuilder"/>
/// but for <c>property: value;</c> pairs.
/// </summary>
/// <example>
/// <code>
/// var style = StyleBuilder.Empty()
///     .AddStyle("width", $"{Width}px", Width &gt; 0)
///     .AddStyle("color", Color)
///     .AddStyle(Style)
///     .Build();
/// </code>
/// </example>
public ref struct StyleBuilder
{
    private StringBuilder? _sb;

    private StyleBuilder(string? initial)
    {
        _sb = null;
        if (!string.IsNullOrWhiteSpace(initial)) AppendRaw(initial);
    }

    /// <summary>Creates an empty builder.</summary>
    public static StyleBuilder Empty() => new(null);

    /// <summary>Creates a builder pre-populated with raw inline style fragments.</summary>
    public static StyleBuilder Default(string? initial) => new(initial);

    /// <summary>Adds a <paramref name="property"/>:<paramref name="value"/> pair.</summary>
    public StyleBuilder AddStyle(string property, string? value)
    {
        if (!string.IsNullOrWhiteSpace(property) && !string.IsNullOrWhiteSpace(value))
        {
            AppendPair(property, value!);
        }
        return this;
    }

    /// <summary>Adds a <paramref name="property"/>:<paramref name="value"/> pair when <paramref name="condition"/> is true.</summary>
    public StyleBuilder AddStyle(string property, string? value, bool condition)
        => condition ? AddStyle(property, value) : this;

    /// <summary>Adds a numeric pixel value (e.g. <c>AddStylePx("width", 240)</c>).</summary>
    public StyleBuilder AddStylePx(string property, double value, bool condition = true)
        => condition
            ? AddStyle(property, value.ToString("0.###", CultureInfo.InvariantCulture) + "px")
            : this;

    /// <summary>Adds a raw style fragment such as <c>"transform: translateX(10px)"</c>.</summary>
    public StyleBuilder AddStyle(string? raw)
    {
        if (!string.IsNullOrWhiteSpace(raw)) AppendRaw(raw!);
        return this;
    }

    /// <summary>
    /// Picks up a <c>style</c> entry from a parameter splat dictionary so unmatched
    /// user-provided inline styles are merged in.
    /// </summary>
    public StyleBuilder AddStyleFromAttributes(IReadOnlyDictionary<string, object>? attributes)
    {
        if (attributes is not null && attributes.TryGetValue("style", out var raw) && raw is string s)
        {
            AddStyle(s);
        }
        return this;
    }

    /// <summary>Returns the composed inline style. Empty when no fragments were added.</summary>
    public readonly string Build() => _sb?.ToString() ?? string.Empty;

    /// <inheritdoc cref="Build"/>
    public override readonly string ToString() => Build();

    private void AppendPair(string property, string value)
    {
        _sb ??= new StringBuilder(64);
        if (_sb.Length > 0 && _sb[_sb.Length - 1] != ';') _sb.Append("; ");
        else if (_sb.Length > 0) _sb.Append(' ');
        _sb.Append(property.Trim()).Append(": ").Append(value.Trim()).Append(';');
    }

    private void AppendRaw(string value)
    {
        _sb ??= new StringBuilder(64);
        var trimmed = value.Trim().TrimEnd(';');
        if (trimmed.Length == 0) return;
        if (_sb.Length > 0 && _sb[_sb.Length - 1] != ';') _sb.Append("; ");
        else if (_sb.Length > 0) _sb.Append(' ');
        _sb.Append(trimmed).Append(';');
    }
}
