namespace SuperUI.Base;

/// <summary>
/// Fluent builder для ARIA атрибутов.
/// Покрывает WAI-ARIA 1.2 specification.
/// </summary>
public sealed class AriaBuilder
{
    private readonly Dictionary<string, object?> _attrs = new();

    // ── Роли ──────────────────────────────────────────────────────────────────
    public AriaBuilder Role(string role) => Set("role", role);
    public AriaBuilder Button() => Role("button");
    public AriaBuilder Dialog() => Role("dialog").Set("aria-modal", "true");
    public AriaBuilder Alert() => Role("alert");
    public AriaBuilder Status() => Role("status");
    public AriaBuilder Tree() => Role("tree");
    public AriaBuilder TreeItem() => Role("treeitem");
    public AriaBuilder Grid() => Role("grid");
    public AriaBuilder Row() => Role("row");
    public AriaBuilder Cell() => Role("gridcell");
    public AriaBuilder List() => Role("list");
    public AriaBuilder ListItem() => Role("listitem");
    public AriaBuilder ComboBox() => Role("combobox");
    public AriaBuilder ListBox() => Role("listbox");
    public AriaBuilder Option() => Role("option");
    public AriaBuilder Tab() => Role("tab");
    public AriaBuilder TabPanel() => Role("tabpanel");
    public AriaBuilder TabList() => Role("tablist");

    // ── Состояния ─────────────────────────────────────────────────────────────
    public AriaBuilder Disabled(bool value = true) => Set("aria-disabled", value.ToAriaString());
    public AriaBuilder Expanded(bool value) => Set("aria-expanded", value.ToAriaString());
    public AriaBuilder Selected(bool value) => Set("aria-selected", value.ToAriaString());
    public AriaBuilder Checked(bool? value) => Set("aria-checked", value?.ToAriaString() ?? "mixed");
    public AriaBuilder Hidden(bool value = true) => Set("aria-hidden", value.ToAriaString());
    public AriaBuilder Busy(bool value = true) => Set("aria-busy", value.ToAriaString());
    public AriaBuilder Invalid(bool value = true) => Set("aria-invalid", value.ToAriaString());
    public AriaBuilder Required(bool value = true) => Set("aria-required", value.ToAriaString());
    public AriaBuilder ReadOnly(bool value = true) => Set("aria-readonly", value.ToAriaString());
    public AriaBuilder Pressed(bool? value) => Set("aria-pressed", value?.ToAriaString() ?? "mixed");
    public AriaBuilder HasPopup(string type = "true") => Set("aria-haspopup", type);

    // ── Связи ─────────────────────────────────────────────────────────────────
    public AriaBuilder Label(string text) => Set("aria-label", text);
    public AriaBuilder LabelledBy(string id) => Set("aria-labelledby", id);
    public AriaBuilder DescribedBy(string id) => Set("aria-describedby", id);
    public AriaBuilder Controls(string id) => Set("aria-controls", id);
    public AriaBuilder Owns(string id) => Set("aria-owns", id);
    public AriaBuilder ActiveDescendant(string id) => Set("aria-activedescendant", id);

    // ── Live regions ──────────────────────────────────────────────────────────
    public AriaBuilder Live(string politeness = "polite") => Set("aria-live", politeness);
    public AriaBuilder Atomic(bool value = true) => Set("aria-atomic", value.ToAriaString());
    public AriaBuilder Relevant(string value = "additions text") => Set("aria-relevant", value);

    // ── Числовые атрибуты ─────────────────────────────────────────────────────
    public AriaBuilder ValueMin(double min) => Set("aria-valuemin", min);
    public AriaBuilder ValueMax(double max) => Set("aria-valuemax", max);
    public AriaBuilder ValueNow(double now) => Set("aria-valuenow", now);
    public AriaBuilder ValueText(string text) => Set("aria-valuetext", text);
    public AriaBuilder Level(int level) => Set("aria-level", level);
    public AriaBuilder SetSize(int size) => Set("aria-setsize", size);
    public AriaBuilder PosInSet(int pos) => Set("aria-posinset", pos);

    // ── Keyboard ──────────────────────────────────────────────────────────────
    public AriaBuilder TabIndex(int index) => Set("tabindex", index);
    public AriaBuilder TabStop() => TabIndex(0);
    public AriaBuilder NoTabStop() => TabIndex(-1);

    // ── Сборка ───────────────────────────────────────────────────────────────
    private AriaBuilder Set(string key, object? value)
    {
        _attrs[key] = value;
        return this;
    }

    public IReadOnlyDictionary<string, object?> Build() => _attrs;
}

internal static class AriaExtensions
{
    public static string ToAriaString(this bool value) => value ? "true" : "false";
}
