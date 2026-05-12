// SuperUI/Base/SgContexts.cs
//
// УЛУЧШЕНИЯ:
//   ✅ SgDataGridContext: non-generic interface (Blazor не поддерживает generic cascade)
//   ✅ SgAccordionContext: HashSet → IReadOnlySet для инкапсуляции
//   ✅ SgFormContext: добавлены IsValid, SubmitCount
//   ✅ SgTabsContext: добавлен Orientation
//   ✅ SgMenuContext: новый контекст для вложенных меню
//   ✅ SgGridRowContext: новый контекст для ячеек строки DataGrid

using Microsoft.AspNetCore.Components.Forms;

namespace SuperUI.Base;

/// <summary>Контекст формы (EditContext + метаданные).</summary>
public sealed class SgFormContext
{
    public EditContext? EditContext { get; init; }
    public bool IsSubmitting { get; init; }
    public bool IsReadOnly { get; init; }
    public bool IsValid { get; init; }
    public int SubmitCount { get; init; }
}

/// <summary>
/// Контекст DataGrid для вложенных компонентов колонок.
/// Non-generic: Blazor не поддерживает generic cascading parameters.
/// Используйте <see cref="ItemType"/> для runtime проверки типа.
/// </summary>
public sealed class SgDataGridContext
{
    /// <summary>Тип элемента (runtime type safety вместо generics).</summary>
    public Type? ItemType { get; init; }

    /// <summary>Элементы как object (кастуйте к нужному типу через ItemType).</summary>
    public IReadOnlyList<object> Items { get; init; } = [];

    /// <summary>Количество строк на странице.</summary>
    public int PageSize { get; init; } = 25;

    /// <summary>Виртуализация включена.</summary>
    public bool IsVirtualized { get; init; }

    /// <summary>Функция получения ключа строки.</summary>
    public Func<object, object?>? KeySelector { get; init; }

    /// <summary>Включена группировка.</summary>
    public bool IsGrouped { get; init; }
}

/// <summary>Контекст строки DataGrid (для ячеек и inline-edit компонентов).</summary>
public sealed class SgGridRowContext
{
    public object? Item { get; init; }
    public int RowIndex { get; init; }
    public bool IsSelected { get; init; }
    public bool IsEditing { get; init; }
    public bool IsExpanded { get; init; }
}

/// <summary>Контекст вкладок (TabPanel → Tab).</summary>
public sealed class SgTabsContext
{
    public string? ActiveTabId { get; init; }
    public Action<string>? OnTabSelected { get; init; }
    public SgOrientation Orientation { get; init; } = SgOrientation.Horizontal;
}

/// <summary>Контекст аккордеона.</summary>
public sealed class SgAccordionContext
{
    public bool AllowMultiple { get; init; }

    // УЛУЧШЕНИЕ: IReadOnlySet для инкапсуляции (изменение только через OnItemToggle)
    public IReadOnlySet<string> OpenItems { get; init; } = new HashSet<string>();
    public Action<string>? OnItemToggle { get; init; }
}

/// <summary>Контекст меню для вложенных пунктов.</summary>
public sealed class SgMenuContext
{
    public bool IsOpen { get; init; }
    public int Level { get; init; }
    public Action? OnClose { get; init; }
}

/// <summary>Контекст SgVirtualList / SgTreeView для виртуализированных компонентов.</summary>
public sealed class SgVirtualContext
{
    public int VisibleStartIndex { get; init; }
    public int VisibleEndIndex { get; init; }
    public int TotalCount { get; init; }
    public double ItemHeight { get; init; }
}

/// <summary>Контекст SgSplitter для дочерних панелей.</summary>
public sealed class SgSplitterContext
{
    public SgOrientation Orientation { get; init; }
    public double[] PanelSizes { get; init; } = [];
    public Action<int, double>? OnPanelResize { get; init; }
}

/// <summary>Контекст SgDataForm для полей формы.</summary>
public sealed class SgDataFormContext
{
    public SgFormContext? Form { get; init; }
    public bool ShowValidation { get; init; } = true;
    public bool IsReadOnly { get; init; }
    public string? LabelPosition { get; init; } = "top"; // "top" | "left" | "right"
}
