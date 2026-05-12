// SuperUI/Base/SgContexts.cs
//
// Контексты для каскадных параметров.
// Все каскадные типы собраны здесь для единого импорта.

namespace SuperUI.Base;

/// <summary>Контекст формы (EditContext + метаданные).</summary>
public sealed class SgFormContext
{
    public Microsoft.AspNetCore.Components.Forms.EditContext? EditContext { get; init; }
    public bool IsSubmitting { get; init; }
    public bool IsReadOnly   { get; init; }
}

/// <summary>Контекст DataGrid (для вложенных компонентов колонок).</summary>
public sealed class SgDataGridContext<TItem>
{
    public IReadOnlyList<TItem> Items { get; init; } = [];
    public int PageSize { get; init; } = 25;
    public bool IsVirtualized { get; init; }
    public Func<TItem, object>? KeySelector { get; init; }
}

/// <summary>Контекст вкладок (TabPanel → Tab).</summary>
public sealed class SgTabsContext
{
    public string? ActiveTabId { get; init; }
    public Action<string>? OnTabSelected { get; init; }
}

/// <summary>Контекст аккордеона.</summary>
public sealed class SgAccordionContext
{
    public bool AllowMultiple { get; init; }
    public HashSet<string> OpenItems { get; init; } = new();
    public Action<string, bool>? OnItemToggle { get; init; }
}
