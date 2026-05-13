// SuperUI/Base/AriaBuilder.cs
// ИСПРАВЛЕНО v3:
// ✅ FIX: Live() валидирует допустимые значения ("off", "polite", "assertive")
// ✅ FIX: HasPopup() валидирует допустимые значения WAI-ARIA
// ✅ FIX: Current() валидирует допустимые значения
// ✅ NEW: aria-keyshortcuts, aria-roledescription, aria-flowto
// ✅ NEW: Merge() — объединение двух builder'ов
// ✅ AOT: нет рефлексии

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SuperUI.Base;

public sealed class AriaBuilder
{
    private readonly Dictionary<string, object?> _attributes = new(StringComparer.OrdinalIgnoreCase);

    // ── Стандартные ARIA атрибуты ──────────────────────────────────────────

    public AriaBuilder Label(string? label, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(label))
            _attributes["aria-label"] = label;
        return this;
    }

    public AriaBuilder LabelledBy(string? id, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(id))
            _attributes["aria-labelledby"] = id;
        return this;
    }

    public AriaBuilder DescribedBy(string? id, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(id))
            _attributes["aria-describedby"] = id;
        return this;
    }

    public AriaBuilder Expanded(bool expanded)
    {
        _attributes["aria-expanded"] = expanded ? "true" : "false";
        return this;
    }

    public AriaBuilder Hidden(bool hidden = true)
    {
        _attributes["aria-hidden"] = hidden ? "true" : "false";
        return this;
    }

    public AriaBuilder Disabled(bool disabled = true)
    {
        _attributes["aria-disabled"] = disabled ? "true" : "false";
        return this;
    }

    public AriaBuilder Selected(bool selected = true)
    {
        _attributes["aria-selected"] = selected ? "true" : "false";
        return this;
    }

    public AriaBuilder Checked(bool? isChecked)
    {
        _attributes["aria-checked"] = isChecked switch
        {
            true  => "true",
            false => "false",
            null  => "mixed"
        };
        return this;
    }

    /// <summary>
    /// ✅ FIX: Валидация допустимых значений aria-haspopup.
    /// WAI-ARIA 1.2: false | true | menu | listbox | tree | grid | dialog
    /// </summary>
    public AriaBuilder HasPopup(string? popupType = "true")
    {
        var validValues = new[] { "false", "true", "menu", "listbox", "tree", "grid", "dialog" };
        var value = popupType ?? "true";
        if (Array.IndexOf(validValues, value) < 0)
            throw new ArgumentException($"aria-haspopup value '{value}' is invalid. Valid: {string.Join(", ", validValues)}",
                nameof(popupType));
        _attributes["aria-haspopup"] = value;
        return this;
    }

    public AriaBuilder Controls(string? id, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(id))
            _attributes["aria-controls"] = id;
        return this;
    }

    /// <summary>
    /// ✅ FIX: Валидация допустимых значений aria-live.
    /// WAI-ARIA: "off" | "polite" | "assertive"
    /// </summary>
    public AriaBuilder Live(string? politeness = "polite")
    {
        var value = politeness ?? "off";
        if (value is not ("off" or "polite" or "assertive"))
            throw new ArgumentException($"aria-live value '{value}' is invalid. Valid: off, polite, assertive",
                nameof(politeness));
        _attributes["aria-live"] = value;
        return this;
    }

    public AriaBuilder Atomic(bool atomic = true)
    {
        _attributes["aria-atomic"] = atomic ? "true" : "false";
        return this;
    }

    public AriaBuilder Busy(bool busy = true)
    {
        _attributes["aria-busy"] = busy ? "true" : "false";
        return this;
    }

    /// <summary>
    /// ✅ FIX: Валидация допустимых значений aria-current.
    /// WAI-ARIA: false | true | page | step | location | date | time
    /// </summary>
    public AriaBuilder Current(string? value = "true")
    {
        var validValues = new[] { "false", "true", "page", "step", "location", "date", "time" };
        var val = value ?? "true";
        if (Array.IndexOf(validValues, val) < 0)
            throw new ArgumentException($"aria-current value '{val}' is invalid. Valid: {string.Join(", ", validValues)}",
                nameof(value));
        _attributes["aria-current"] = val;
        return this;
    }

    public AriaBuilder Required(bool required = true)
    {
        _attributes["aria-required"] = required ? "true" : "false";
        return this;
    }

    public AriaBuilder Invalid(bool invalid = true)
    {
        _attributes["aria-invalid"] = invalid ? "true" : "false";
        return this;
    }

    public AriaBuilder ErrorMessage(string? id, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(id))
            _attributes["aria-errormessage"] = id;
        return this;
    }

    // ── Новые атрибуты WAI-ARIA 1.2 ───────────────────────────────────────

    /// <summary>aria-keyshortcuts — описывает горячие клавиши элемента.</summary>
    public AriaBuilder KeyShortcuts(string? shortcuts, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(shortcuts))
            _attributes["aria-keyshortcuts"] = shortcuts;
        return this;
    }

    /// <summary>aria-roledescription — пользовательское описание роли.</summary>
    public AriaBuilder RoleDescription(string? description, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(description))
            _attributes["aria-roledescription"] = description;
        return this;
    }

    /// <summary>aria-flowto — порядок навигации (альтернативный чтению).</summary>
    public AriaBuilder FlowTo(string? id, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(id))
            _attributes["aria-flowto"] = id;
        return this;
    }

    /// <summary>aria-posinset / aria-setsize — позиция в наборе.</summary>
    public AriaBuilder PosInSet(int position, int setSize)
    {
        _attributes["aria-posinset"] = position.ToString();
        _attributes["aria-setsize"]  = setSize.ToString();
        return this;
    }

    /// <summary>aria-level — уровень вложенности (для headings, trees).</summary>
    public AriaBuilder Level(int level)
    {
        if (level < 1 || level > 6)
            throw new ArgumentOutOfRangeException(nameof(level), "aria-level must be between 1 and 6.");
        _attributes["aria-level"] = level.ToString();
        return this;
    }

    // ── Role shortcuts ─────────────────────────────────────────────────────

    public AriaBuilder Role(string? role, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(role))
            _attributes["role"] = role;
        return this;
    }

    public AriaBuilder Alert()      => Role("alert");
    public AriaBuilder Status()     => Role("status");
    public AriaBuilder Dialog()     => Role("dialog");
    public AriaBuilder ButtonRole() => Role("button");
    public AriaBuilder Region()     => Role("region");
    public AriaBuilder Navigation() => Role("navigation");
    public AriaBuilder Main()       => Role("main");
    public AriaBuilder Complementary() => Role("complementary");

    // ── Произвольный атрибут ───────────────────────────────────────────────

    public AriaBuilder Add(string attribute, string? value, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(value))
        {
            var key = attribute.StartsWith("aria-", StringComparison.OrdinalIgnoreCase)
                ? attribute : $"aria-{attribute}";
            _attributes[key] = value;
        }
        return this;
    }

    // ── Merge ──────────────────────────────────────────────────────────────

    /// <summary>Объединить с другим builder'ом. Значения other перезаписывают текущие.</summary>
    public AriaBuilder Merge(AriaBuilder other)
    {
        foreach (var (key, value) in other._attributes)
            _attributes[key] = value;
        return this;
    }

    // ── Build ──────────────────────────────────────────────────────────────

    public IReadOnlyDictionary<string, object?> Build() =>
        new Dictionary<string, object?>(_attributes);

    public string? Get(string attribute) =>
        _attributes.TryGetValue(attribute, out var v) ? v?.ToString() : null;

    public static AriaBuilder Empty => new();
}
