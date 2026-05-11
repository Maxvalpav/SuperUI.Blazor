using System.Text;

namespace SuperUI.Core;

/// <summary>
/// Fluent builder for composing CSS class strings with conditional fragments.
/// Use over manual concatenation in markup to avoid stray spaces, double classes, and null literals.
/// </summary>
/// <example>
/// <code>
/// var css = CssBuilder.Default("sgc-btn")
///     .AddClass($"sgc-btn-{Variant.ToString().ToLower()}")
///     .AddClass("is-loading", IsLoading)
///     .AddClass("is-disabled", Disabled)
///     .AddClass(Class)
///     .Build();
/// </code>
/// </example>
public ref struct CssBuilder
{
    private StringBuilder? _sb;

    private CssBuilder(string? initial)
    {
        _sb = null;
        if (!string.IsNullOrWhiteSpace(initial)) Append(initial);
    }

    /// <summary>Creates an empty builder.</summary>
    public static CssBuilder Empty() => new(null);

    /// <summary>Creates a builder pre-populated with the given class.</summary>
    public static CssBuilder Default(string? initial) => new(initial);

    /// <summary>Adds a class fragment if non-empty.</summary>
    public CssBuilder AddClass(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) Append(value);
        return this;
    }

    /// <summary>Adds a class fragment if <paramref name="condition"/> is true.</summary>
    public CssBuilder AddClass(string? value, bool condition)
        => condition ? AddClass(value) : this;

    /// <summary>Adds a class fragment if the predicate returns true (lazily evaluated).</summary>
    public CssBuilder AddClass(string? value, Func<bool> when)
        => when is not null && when() ? AddClass(value) : this;

    /// <summary>Adds a class produced by the factory if <paramref name="condition"/> is true.</summary>
    public CssBuilder AddClass(Func<string?> valueFactory, bool condition)
        => condition && valueFactory is not null ? AddClass(valueFactory()) : this;

    /// <summary>
    /// Picks up a <c>class</c> entry from a parameter splat dictionary (Blazor's
    /// <c>AdditionalAttributes</c>) so unmatched user classes are merged in.
    /// </summary>
    public CssBuilder AddClassFromAttributes(IReadOnlyDictionary<string, object>? attributes)
    {
        if (attributes is not null && attributes.TryGetValue("class", out var raw) && raw is string s)
        {
            AddClass(s);
        }
        return this;
    }

    /// <summary>Returns the composed class string. Empty when no fragments were added.</summary>
    public readonly string Build() => _sb?.ToString() ?? string.Empty;

    /// <inheritdoc cref="Build"/>
    public override readonly string ToString() => Build();

    private void Append(string value)
    {
        _sb ??= new StringBuilder(64);
        if (_sb.Length > 0) _sb.Append(' ');
        _sb.Append(value.Trim());
    }
}
