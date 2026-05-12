// SuperUI/Base/AriaBuilder.cs
//
// НОВЫЙ/ПОЛНЫЙ: Fluent builder для ARIA-атрибутов.
// Обеспечивает доступность (a11y) компонентов.
// Интегрируется с SgComponentBase.BuildAriaAttributes().
//
// Thread safety: immutable builder → thread-safe.

namespace SuperUI.Base;

/// <summary>
/// Fluent builder для ARIA-атрибутов доступности.
/// </summary>
/// <example>
/// <code>
/// var aria = AriaBuilder.Create()
///     .Role("dialog")
///     .Label("Подтвердите действие")
///     .Modal(true)
///     .Hidden(!Open)
///     .Build();
/// </code>
/// </example>
public sealed class AriaBuilder
{
    private readonly Dictionary<string, object> _attrs = new(StringComparer.Ordinal);

    private AriaBuilder() { }

    /// <summary>Создать новый builder.</summary>
    public static AriaBuilder Create() => new();

    // ── Role ──────────────────────────────────────────────────────────────────

    /// <summary>Установить role атрибут.</summary>
    public AriaBuilder Role(string role)
    {
        if (!string.IsNullOrWhiteSpace(role))
            _attrs["role"] = role;
        return this;
    }

    // ── Стандартные ARIA атрибуты ─────────────────────────────────────────────

    /// <summary>aria-label.</summary>
    public AriaBuilder Label(string? label)
        => Set("aria-label", label);

    /// <summary>aria-labelledby (ID элемента с текстом метки).</summary>
    public AriaBuilder LabelledBy(string? elementId)
        => Set("aria-labelledby", elementId);

    /// <summary>aria-describedby (ID элемента с описанием).</summary>
    public AriaBuilder DescribedBy(string? elementId)
        => Set("aria-describedby", elementId);

    /// <summary>aria-hidden.</summary>
    public AriaBuilder Hidden(bool hidden)
        => Set("aria-hidden", hidden ? "true" : "false");

    /// <summary>aria-expanded.</summary>
    public AriaBuilder Expanded(bool expanded)
        => Set("aria-expanded", expanded ? "true" : "false");

    /// <summary>aria-selected.</summary>
    public AriaBuilder Selected(bool selected)
        => Set("aria-selected", selected ? "true" : "false");

    /// <summary>aria-checked (true/false/mixed).</summary>
    public AriaBuilder Checked(bool? value)
        => Set("aria-checked", value switch { true => "true", false => "false", null => "mixed" });

    /// <summary>aria-disabled.</summary>
    public AriaBuilder Disabled(bool disabled)
    {
        if (disabled) Set("aria-disabled", "true");
        else _attrs.Remove("aria-disabled");
        return this;
    }

    /// <summary>aria-readonly.</summary>
    public AriaBuilder ReadOnly(bool readOnly)
    {
        if (readOnly) Set("aria-readonly", "true");
        else _attrs.Remove("aria-readonly");
        return this;
    }

    /// <summary>aria-required.</summary>
    public AriaBuilder Required(bool required)
    {
        if (required) Set("aria-required", "true");
        else _attrs.Remove("aria-required");
        return this;
    }

    /// <summary>aria-invalid (для форм).</summary>
    public AriaBuilder Invalid(bool invalid, string? description = null)
    {
        if (invalid)
        {
            Set("aria-invalid", "true");
            if (description is not null) Set("aria-errormessage", description);
        }
        else
        {
            _attrs.Remove("aria-invalid");
            _attrs.Remove("aria-errormessage");
        }
        return this;
    }

    /// <summary>aria-modal (для dialog/alertdialog).</summary>
    public AriaBuilder Modal(bool modal = true)
    {
        if (modal) Set("aria-modal", "true");
        else _attrs.Remove("aria-modal");
        return this;
    }

    /// <summary>aria-busy (для загрузки).</summary>
    public AriaBuilder Busy(bool busy)
        => Set("aria-busy", busy ? "true" : "false");

    /// <summary>aria-live (для динамических регионов).</summary>
    public AriaBuilder Live(AriaLive live)
        => Set("aria-live", live switch
        {
            AriaLive.Polite    => "polite",
            AriaLive.Assertive => "assertive",
            _                  => "off"
        });

    /// <summary>aria-atomic.</summary>
    public AriaBuilder Atomic(bool atomic = true)
        => Set("aria-atomic", atomic ? "true" : "false");

    /// <summary>aria-haspopup.</summary>
    public AriaBuilder HasPopup(AriaHasPopup popup = AriaHasPopup.True)
        => Set("aria-haspopup", popup switch
        {
            AriaHasPopup.Menu    => "menu",
            AriaHasPopup.Listbox => "listbox",
            AriaHasPopup.Tree    => "tree",
            AriaHasPopup.Grid    => "grid",
            AriaHasPopup.Dialog  => "dialog",
            AriaHasPopup.True    => "true",
            _                    => "false"
        });

    /// <summary>aria-controls (IDs контролируемых элементов).</summary>
    public AriaBuilder Controls(params string[] elementIds)
        => Set("aria-controls", string.Join(" ", elementIds.Where(id => !string.IsNullOrEmpty(id))));

    /// <summary>aria-owns.</summary>
    public AriaBuilder Owns(params string[] elementIds)
        => Set("aria-owns", string.Join(" ", elementIds.Where(id => !string.IsNullOrEmpty(id))));

    /// <summary>tabindex.</summary>
    public AriaBuilder TabIndex(int index)
        => Set("tabindex", index.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>Удалить tabindex из табуляции.</summary>
    public AriaBuilder RemoveFromTabOrder()
        => Set("tabindex", "-1");

    /// <summary>aria-valuemin/max/now для range компонентов.</summary>
    public AriaBuilder ValueRange(double min, double max, double current)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        Set("aria-valuemin", min.ToString(inv));
        Set("aria-valuemax", max.ToString(inv));
        Set("aria-valuenow", current.ToString(inv));
        return this;
    }

    /// <summary>aria-valuetext (текстовое представление значения).</summary>
    public AriaBuilder ValueText(string? text)
        => Set("aria-valuetext", text);

    /// <summary>aria-level (для heading, tree и т.д.).</summary>
    public AriaBuilder Level(int level)
        => Set("aria-level", level.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <summary>aria-posinset / aria-setsize (для элементов в списке).</summary>
    public AriaBuilder Position(int posInSet, int setSize)
    {
        Set("aria-posinset", posInSet.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Set("aria-setsize",  setSize.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return this;
    }

    /// <summary>aria-sort (для заголовков таблицы).</summary>
    public AriaBuilder Sort(AriaSort sort)
        => Set("aria-sort", sort switch
        {
            AriaSort.Ascending  => "ascending",
            AriaSort.Descending => "descending",
            AriaSort.Other      => "other",
            _                   => "none"
        });

    /// <summary>Добавить произвольный ARIA-атрибут.</summary>
    public AriaBuilder Custom(string attribute, string value)
    {
        if (!string.IsNullOrWhiteSpace(attribute))
            _attrs[attribute] = value;
        return this;
    }

    /// <summary>Объединить с существующим словарём атрибутов.</summary>
    public AriaBuilder Merge(IReadOnlyDictionary<string, object>? existing)
    {
        if (existing is not null)
            foreach (var kvp in existing)
                if (!_attrs.ContainsKey(kvp.Key)) // existing не перезаписывает новые
                    _attrs[kvp.Key] = kvp.Value;
        return this;
    }

    /// <summary>Условное применение конфигурации (fluent helper).</summary>
    public AriaBuilder If(bool condition, Func<AriaBuilder, AriaBuilder> configure)
    {
        if (condition) configure(this);
        return this;
    }

    /// <summary>Собрать словарь ARIA-атрибутов.</summary>
    public IReadOnlyDictionary<string, object> Build()
        => new Dictionary<string, object>(_attrs, StringComparer.Ordinal);

    /// <summary>Implicit конвертация для использования в компонентах.</summary>
    public static implicit operator Dictionary<string, object>(AriaBuilder builder)
        => new(builder._attrs, StringComparer.Ordinal);

    private AriaBuilder Set(string key, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            _attrs[key] = value;
        return this;
    }
}

/// <summary>Значения aria-live.</summary>
public enum AriaLive { Off, Polite, Assertive }

/// <summary>Значения aria-haspopup.</summary>
public enum AriaHasPopup { False, True, Menu, Listbox, Tree, Grid, Dialog }

/// <summary>Значения aria-sort.</summary>
public enum AriaSort { None, Ascending, Descending, Other }
