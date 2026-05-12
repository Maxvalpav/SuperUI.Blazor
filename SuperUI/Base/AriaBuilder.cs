// SuperUI/Base/AriaBuilder.cs
//
// Fluent-builder для ARIA-атрибутов.
// Обеспечивает правильные значения и предотвращает ошибки (нет опечаток в строках).
//
// НОВОЕ:
// 1. Типизированные методы для всех стандартных ARIA-атрибутов.
// 2. Fluent API.
// 3. Build() → Dictionary<string, object>.
// 4. Merge с AdditionalAttributes.

namespace SuperUI.Base;

/// <summary>
/// Fluent-builder для ARIA-атрибутов HTML.
/// </summary>
public sealed class AriaBuilder
{
    private Dictionary<string, object>? _attrs;

    // ── Базовые методы ────────────────────────────────────────────────────────

    public AriaBuilder Set(string attribute, object value)
    {
        (_attrs ??= new(StringComparer.Ordinal))[attribute] = value;
        return this;
    }

    public AriaBuilder SetIf(bool condition, string attribute, object value)
    {
        if (condition) Set(attribute, value);
        return this;
    }

    // ── Роль и ориентация ────────────────────────────────────────────────────

    public AriaBuilder Role(string role)         => Set("role", role);
    public AriaBuilder TabIndex(int tabIndex)    => Set("tabindex", tabIndex);
    public AriaBuilder Hidden(bool value = true) => Set("aria-hidden", value ? "true" : "false");

    // ── Состояния ────────────────────────────────────────────────────────────

    public AriaBuilder Disabled(bool value = true)  => SetIf(value, "aria-disabled", "true");
    public AriaBuilder Expanded(bool? value)         => value.HasValue ? Set("aria-expanded", value.Value ? "true" : "false") : this;
    public AriaBuilder Selected(bool? value)         => value.HasValue ? Set("aria-selected", value.Value ? "true" : "false") : this;
    public AriaBuilder Checked(bool? value)
    {
        if (!value.HasValue) return this;
        return Set("aria-checked", value.Value ? "true" : "false");
    }
    public AriaBuilder Indeterminate()              => Set("aria-checked", "mixed");
    public AriaBuilder Pressed(bool? value)          => value.HasValue ? Set("aria-pressed", value.Value ? "true" : "false") : this;
    public AriaBuilder ReadOnly(bool value = true)   => SetIf(value, "aria-readonly", "true");
    public AriaBuilder Required(bool value = true)   => SetIf(value, "aria-required", "true");
    public AriaBuilder Invalid(bool value = true)    => SetIf(value, "aria-invalid", value ? "true" : "false");
    public AriaBuilder Busy(bool value = true)       => SetIf(value, "aria-busy", "true");
    public AriaBuilder Modal(bool value = true)      => SetIf(value, "aria-modal", "true");

    // ── Связи ────────────────────────────────────────────────────────────────

    public AriaBuilder Label(string? label)
    {
        if (!string.IsNullOrWhiteSpace(label)) Set("aria-label", label);
        return this;
    }

    public AriaBuilder LabelledBy(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id)) Set("aria-labelledby", id);
        return this;
    }

    public AriaBuilder DescribedBy(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id)) Set("aria-describedby", id);
        return this;
    }

    public AriaBuilder ErrorMessage(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id)) Set("aria-errormessage", id);
        return this;
    }

    public AriaBuilder Controls(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id)) Set("aria-controls", id);
        return this;
    }

    public AriaBuilder Owns(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id)) Set("aria-owns", id);
        return this;
    }

    public AriaBuilder ActiveDescendant(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id)) Set("aria-activedescendant", id);
        return this;
    }

    public AriaBuilder FlowTo(string? id)
    {
        if (!string.IsNullOrWhiteSpace(id)) Set("aria-flowto", id);
        return this;
    }

    // ── Значения ─────────────────────────────────────────────────────────────

    public AriaBuilder ValueMin(double min)  => Set("aria-valuemin", min);
    public AriaBuilder ValueMax(double max)  => Set("aria-valuemax", max);
    public AriaBuilder ValueNow(double now)  => Set("aria-valuenow", now);
    public AriaBuilder ValueText(string text) => Set("aria-valuetext", text);
    public AriaBuilder MaxLength(int max)    => Set("aria-maxlength", max);
    public AriaBuilder MinLength(int min)    => Set("aria-minlength", min);
    public AriaBuilder RowCount(int count)   => Set("aria-rowcount", count);
    public AriaBuilder ColCount(int count)   => Set("aria-colcount", count);
    public AriaBuilder RowIndex(int index)   => Set("aria-rowindex", index);
    public AriaBuilder ColIndex(int index)   => Set("aria-colindex", index);
    public AriaBuilder Level(int level)      => Set("aria-level", level);
    public AriaBuilder SetSize(int size)     => Set("aria-setsize", size);
    public AriaBuilder PosInSet(int pos)     => Set("aria-posinset", pos);

    // ── Live regions ─────────────────────────────────────────────────────────

    public AriaBuilder Live(string politeness = "polite")
        => Set("aria-live", politeness); // "off" | "polite" | "assertive"

    public AriaBuilder Atomic(bool value = true) => Set("aria-atomic", value ? "true" : "false");
    public AriaBuilder Relevant(string relevant = "additions text")
        => Set("aria-relevant", relevant);

    // ── Сборка ────────────────────────────────────────────────────────────────

    /// <summary>Собрать словарь ARIA-атрибутов.</summary>
    public IReadOnlyDictionary<string, object> Build()
        => _attrs ?? (IReadOnlyDictionary<string, object>)new Dictionary<string, object>();

    /// <summary>Объединить с AdditionalAttributes (aria-* и role/tabindex).</summary>
    public AriaBuilder MergeAdditional(IReadOnlyDictionary<string, object>? additional)
    {
        if (additional is null) return this;
        foreach (var kvp in additional)
        {
            var key = kvp.Key;
            if (key.StartsWith("aria-", StringComparison.OrdinalIgnoreCase)
                || key.Equals("role", StringComparison.OrdinalIgnoreCase)
                || key.Equals("tabindex", StringComparison.OrdinalIgnoreCase))
            {
                // User-provided атрибуты не перезаписывают программно заданные
                (_attrs ??= new(StringComparer.Ordinal)).TryAdd(key, kvp.Value);
            }
        }
        return this;
    }
}
