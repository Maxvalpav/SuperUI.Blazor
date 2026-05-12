// SuperUI/Base/Utilities/StyleBuilder.cs
using System.Text;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Fluent inline-style builder.
/// </summary>
/// <remarks>
/// Mutable by design (как <see cref="SgCssBuilder"/>): каждый <c>Add</c> возвращает
/// тот же экземпляр — fluent-цепочка не аллоцирует промежуточные builders.
/// Не thread-safe; предполагается локальное использование внутри одного рендера.
/// Совместим с WASM и Server.
/// </remarks>
public sealed class StyleBuilder
{
    private readonly List<(string Prop, string Value)> _styles = [];
    private string? _cached;

    public StyleBuilder() { }

    public StyleBuilder(string? baseStyle)
    {
        AddUserStyle(baseStyle);
    }

    public StyleBuilder Add(string property, string? value, bool condition = true)
    {
        if (!condition || string.IsNullOrWhiteSpace(property) || string.IsNullOrWhiteSpace(value))
            return this;
        _cached = null;
        _styles.Add((property.Trim(), value!.Trim()));
        return this;
    }

    public StyleBuilder Add(string property, string? value, Func<bool> condition)
        => condition() ? Add(property, value) : this;

    public StyleBuilder AddIf(string property, string? value, Func<bool> condition)
        => condition() ? Add(property, value) : this;

    public StyleBuilder AddVar(string cssVar, string? value, bool condition = true)
        => Add($"--{cssVar}", value, condition);

    /// <summary>
    /// Добавить произвольную style-строку пользователя ("prop: val; prop2: val2").
    /// Безопасно обрабатывает malformed input — пропускает невалидные сегменты.
    /// </summary>
    public StyleBuilder AddUserStyle(string? userStyle)
    {
        if (string.IsNullOrWhiteSpace(userStyle)) return this;

        var span = userStyle.AsSpan();
        var start = 0;
        while (start < span.Length)
        {
            var rest = span[start..];
            var semi = rest.IndexOf(';');
            var segment = semi < 0 ? rest : rest[..semi];
            start += semi < 0 ? rest.Length : semi + 1;

            var colon = segment.IndexOf(':');
            if (colon <= 0) continue;

            var prop = segment[..colon].Trim();
            var val = segment[(colon + 1)..].Trim();
            if (prop.IsEmpty || val.IsEmpty) continue;

            _cached = null;
            _styles.Add((prop.ToString(), val.ToString()));
        }
        return this;
    }

    public string? Build()
    {
        if (_cached is not null) return _cached;
        if (_styles.Count == 0) return _cached = null;

        var capacity = 0;
        for (var i = 0; i < _styles.Count; i++)
            capacity += _styles[i].Prop.Length + _styles[i].Value.Length + 4;

        var sb = new StringBuilder(capacity);
        for (var i = 0; i < _styles.Count; i++)
        {
            if (i > 0) sb.Append("; ");
            sb.Append(_styles[i].Prop).Append(": ").Append(_styles[i].Value);
        }

        return _cached = sb.ToString();
    }

    public static implicit operator string?(StyleBuilder builder) => builder.Build();
    public override string? ToString() => Build();
}
