// SuperUI/Base/AriaBuilder.cs
namespace SuperUI.Base;

/// <summary>
/// Fluent builder для ARIA атрибутов.
/// Покрывает WAI-ARIA 1.2 specification.
/// 
/// ИСПРАВЛЕНИЯ:
/// - Добавлен Build(Dictionary) overload для zero-allocation merge с existing dict
/// - Добавлены недостающие ARIA 1.2 атрибуты (aria-colcount, aria-rowcount и др.)
/// - AriaExtensions перенесены в отдельный internal static class
/// - Добавлен метод Merge для слияния двух AriaBuilder
/// </summary>
public sealed class AriaBuilder
{
    private readonly Dictionary<string, object> _attrs = new(StringComparer.Ordinal);

    // ── Роли ────────────────────────────────────────────────────────────────────
    public AriaBuilder Role(string role)         => Set("role", role);
    public AriaBuilder Button()                  => Role("button");
    public AriaBuilder Dialog()                  => Role("dialog").Set("aria-modal", "true");
    public AriaBuilder AlertDialog()             => Role("alertdialog").Set("aria-modal", "true");
    public AriaBuilder Alert()                   => Role("alert");
    public AriaBuilder Status()                  => Role("status");
    public AriaBuilder Tree()                    => Role("tree");
    public AriaBuilder TreeItem()                => Role("treeitem");
    public AriaBuilder Grid()                    => Role("grid");
    public AriaBuilder Row()                     => Role("row");
    public AriaBuilder Cell()                    => Role("gridcell");
    public AriaBuilder ColumnHeader()            => Role("columnheader");
    public AriaBuilder RowHeader()               => Role("rowheader");
    public AriaBuilder List()                    => Role("list");
    public AriaBuilder ListItem()                => Role("listitem");
    public AriaBuilder ComboBox()                => Role("combobox");
    public AriaBuilder ListBox()                 => Role("listbox");
    public AriaBuilder Option()                  => Role("option");
    public AriaBuilder Tab()                     => Role("tab");
    public AriaBuilder TabPanel()                => Role("tabpanel");
    public AriaBuilder TabList()                 => Role("tablist");
    public AriaBuilder Menu()                    => Role("menu");
    public AriaBuilder MenuBar()                 => Role("menubar");
    public AriaBuilder MenuItem()                => Role("menuitem");
    public AriaBuilder MenuItemCheckBox()        => Role("menuitemcheckbox");
    public AriaBuilder MenuItemRadio()           => Role("menuitemradio");
    public AriaBuilder Slider()                  => Role("slider");
    public AriaBuilder SpinButton()              => Role("spinbutton");
    public AriaBuilder Switch()                  => Role("switch");
    public AriaBuilder CheckBox()                => Role("checkbox");
    public AriaBuilder Radio()                   => Role("radio");
    public AriaBuilder RadioGroup()              => Role("radiogroup");
    public AriaBuilder SearchBox()               => Role("searchbox");
    public AriaBuilder TextBox()                 => Role("textbox");
    public AriaBuilder ProgressBar()             => Role("progressbar");
    public AriaBuilder Toolbar()                 => Role("toolbar");
    public AriaBuilder Tooltip()                 => Role("tooltip");
    public AriaBuilder Banner()                  => Role("banner");
    public AriaBuilder Navigation()              => Role("navigation");
    public AriaBuilder Main()                    => Role("main");
    public AriaBuilder Region()                  => Role("region");
    public AriaBuilder Form()                    => Role("form");
    public AriaBuilder Log()                     => Role("log");
    public AriaBuilder Note()                    => Role("note");
    public AriaBuilder Application()             => Role("application");
    public AriaBuilder Document()                => Role("document");
    public AriaBuilder Figure()                  => Role("figure");
    public AriaBuilder Group()                   => Role("group");
    public AriaBuilder Heading(int level = 2)    => Role("heading").Level(level);
    public AriaBuilder Img(string? label = null) => Role("img").LabelIf(label);
    public AriaBuilder Link()                    => Role("link");
    public AriaBuilder None()                    => Role("none");
    public AriaBuilder Presentation()            => Role("presentation");
    public AriaBuilder Separator()               => Role("separator");
    public AriaBuilder Term()                    => Role("term");
    public AriaBuilder Definition()              => Role("definition");
    public AriaBuilder Article()                 => Role("article");

    // ── Состояния ──────────────────────────────────────────────────────────────
    public AriaBuilder Disabled(bool value = true)    => Set("aria-disabled", value.ToAriaString());
    public AriaBuilder Expanded(bool value)           => Set("aria-expanded", value.ToAriaString());
    public AriaBuilder Selected(bool value)           => Set("aria-selected", value.ToAriaString());
    public AriaBuilder Checked(bool? value)           => Set("aria-checked", value?.ToAriaString() ?? "mixed");
    public AriaBuilder Hidden(bool value = true)      => Set("aria-hidden", value.ToAriaString());
    public AriaBuilder Busy(bool value = true)        => Set("aria-busy", value.ToAriaString());
    public AriaBuilder Invalid(bool value = true)     => Set("aria-invalid", value.ToAriaString());
    public AriaBuilder Required(bool value = true)    => Set("aria-required", value.ToAriaString());
    public AriaBuilder ReadOnly(bool value = true)    => Set("aria-readonly", value.ToAriaString());
    public AriaBuilder Pressed(bool? value)           => Set("aria-pressed", value?.ToAriaString() ?? "mixed");
    public AriaBuilder HasPopup(string type = "true") => Set("aria-haspopup", type);
    public AriaBuilder Current(string value = "true") => Set("aria-current", value); // ARIA 1.1
    public AriaBuilder Sort(string value)             => Set("aria-sort", value); // none|ascending|descending|other
    public AriaBuilder Grabbed(bool? value)           => Set("aria-grabbed", value?.ToAriaString() ?? "undefined");
    public AriaBuilder DropEffect(string value)       => Set("aria-dropeffect", value);
    public AriaBuilder Modal(bool value = true)       => Set("aria-modal", value.ToAriaString());
    public AriaBuilder MultiLine(bool value = true)   => Set("aria-multiline", value.ToAriaString());
    public AriaBuilder MultiSelectable(bool value)    => Set("aria-multiselectable", value.ToAriaString());
    public AriaBuilder Orientation(string value)      => Set("aria-orientation", value); // horizontal|vertical

    // ── Связи ──────────────────────────────────────────────────────────────────
    public AriaBuilder Label(string text)            => Set("aria-label", text);
    public AriaBuilder LabelledBy(string id)         => Set("aria-labelledby", id);
    public AriaBuilder DescribedBy(string id)        => Set("aria-describedby", id);
    public AriaBuilder Details(string id)            => Set("aria-details", id); // ARIA 1.1
    public AriaBuilder Controls(string id)           => Set("aria-controls", id);
    public AriaBuilder Owns(string id)               => Set("aria-owns", id);
    public AriaBuilder FlowTo(string id)             => Set("aria-flowto", id);
    public AriaBuilder ActiveDescendant(string id)   => Set("aria-activedescendant", id);
    public AriaBuilder ErrorMessage(string id)       => Set("aria-errormessage", id); // ARIA 1.1
    public AriaBuilder KeyShortcuts(string keys)     => Set("aria-keyshortcuts", keys); // ARIA 1.1
    public AriaBuilder RoleDescription(string desc)  => Set("aria-roledescription", desc); // ARIA 1.1
    public AriaBuilder Placeholder(string text)      => Set("aria-placeholder", text); // ARIA 1.1
    private AriaBuilder LabelIf(string? label)       => label != null ? Label(label) : this;

    // ── Live regions ───────────────────────────────────────────────────────────
    public AriaBuilder Live(string politeness = "polite") => Set("aria-live", politeness);
    public AriaBuilder Polite()                           => Live("polite");
    public AriaBuilder Assertive()                        => Live("assertive");
    public AriaBuilder Off()                              => Live("off");
    public AriaBuilder Atomic(bool value = true)          => Set("aria-atomic", value.ToAriaString());
    public AriaBuilder Relevant(string value = "additions text") => Set("aria-relevant", value);

    // ── Числовые атрибуты ─────────────────────────────────────────────────────
    public AriaBuilder ValueMin(double min)   => Set("aria-valuemin", min);
    public AriaBuilder ValueMax(double max)   => Set("aria-valuemax", max);
    public AriaBuilder ValueNow(double now)   => Set("aria-valuenow", now);
    public AriaBuilder ValueText(string text) => Set("aria-valuetext", text);
    public AriaBuilder Level(int level)       => Set("aria-level", level);
    public AriaBuilder SetSize(int size)      => Set("aria-setsize", size);
    public AriaBuilder PosInSet(int pos)      => Set("aria-posinset", pos);
    public AriaBuilder ColCount(int count)    => Set("aria-colcount", count); // ARIA 1.1
    public AriaBuilder ColIndex(int index)    => Set("aria-colindex", index); // ARIA 1.1
    public AriaBuilder ColSpan(int span)      => Set("aria-colspan", span);   // ARIA 1.1
    public AriaBuilder RowCount(int count)    => Set("aria-rowcount", count); // ARIA 1.1
    public AriaBuilder RowIndex(int index)    => Set("aria-rowindex", index); // ARIA 1.1
    public AriaBuilder RowSpan(int span)      => Set("aria-rowspan", span);   // ARIA 1.1

    // ── Keyboard ───────────────────────────────────────────────────────────────
    public AriaBuilder TabIndex(int index) => Set("tabindex", index);
    public AriaBuilder TabStop()           => TabIndex(0);
    public AriaBuilder NoTabStop()         => TabIndex(-1);

    // ── Сборка ────────────────────────────────────────────────────────────────
    private AriaBuilder Set(string key, object? value)
    {
        if (value is not null)
            _attrs[key] = value;
        return this;
    }

    public IReadOnlyDictionary<string, object> Build() => _attrs;

    /// <summary>
    /// НОВЫЙ: Влить атрибуты в существующий словарь (zero-allocation merge).
    /// </summary>
    public void MergeInto(Dictionary<string, object> target)
    {
        foreach (var (k, v) in _attrs)
            target.TryAdd(k, v); // пользовательские атрибуты имеют приоритет
    }

    /// <summary>
    /// НОВЫЙ: Объединить два AriaBuilder.
    /// </summary>
    public AriaBuilder Merge(AriaBuilder other)
    {
        foreach (var (k, v) in other._attrs)
            _attrs.TryAdd(k, v);
        return this;
    }
}

internal static class AriaExtensions
{
    public static string ToAriaString(this bool value) => value ? "true" : "false";
}
