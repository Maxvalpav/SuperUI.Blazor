// SuperUI/Base/SgContexts.cs
//
// ИСПРАВЛЕНИЯ:
// ✅ CS0101: убраны дублирующиеся SgThemeContext / SgConfigContext
//    (определены ТОЛЬКО здесь, удалить из других файлов проекта)
// ✅ IRenderHook убран — определён в Base/Hooks/IRenderHook.cs
// УЛУЧШЕНИЯ:
// ✅ SgThemeContext: добавлены CssVariables, Direction
// ✅ SgConfigContext: добавлены ZIndex-параметры
// ✅ SgDataGridContext: добавлены SortDescriptor, SelectionMode
// ✅ SgDataFormContext: добавлены LabelWidth, Colon
// ✅ SgVirtualContext: добавлены OverscanCount, ItemWidth

using Microsoft.AspNetCore.Components.Forms;

namespace SuperUI.Base;

// ── Тема и конфигурация ───────────────────────────────────────────────────────

/// <summary>
/// Контекст текущей темы SuperUI.
/// Передаётся через CascadingValue в SgConfigProvider.
/// </summary>
public sealed class SgThemeContext
{
    /// <summary>Текущая тема.</summary>
    public SgTheme Theme { get; init; } = SgTheme.Auto;

    /// <summary>RTL (right-to-left) режим.</summary>
    public bool IsRtl { get; init; }

    /// <summary>Направление текста для CSS (ltr/rtl).</summary>
    public string Direction => IsRtl ? "rtl" : "ltr";

    /// <summary>CSS-переменные темы (для кастомизации).</summary>
    public IReadOnlyDictionary<string, string> Variables { get; init; }
        = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>CSS-класс темы для корневого элемента.</summary>
    public string ThemeClass => Theme switch
    {
        SgTheme.Dark  => "sg-theme-dark",
        SgTheme.Light => "sg-theme-light",
        _             => "sg-theme-auto"
    };

    /// <summary>Тёмная тема активна.</summary>
    public bool IsDark => Theme == SgTheme.Dark;
}

/// <summary>
/// Контекст конфигурации SuperUI.
/// Передаётся через CascadingValue в SgConfigProvider.
/// </summary>
public sealed class SgConfigContext
{
    /// <summary>Размер компонентов по умолчанию.</summary>
    public SgSize DefaultSize { get; init; } = SgSize.Medium;

    /// <summary>Анимации включены.</summary>
    public bool AnimationsEnabled { get; init; } = true;

    /// <summary>Длительность анимаций в миллисекундах.</summary>
    public int AnimationDurationMs { get; init; } = 300;

    /// <summary>Включить ARIA-атрибуты (accessibility).</summary>
    public bool EnableAria { get; init; } = true;

    /// <summary>Язык/локаль (BCP 47: "ru-RU", "en-US").</summary>
    public string Locale { get; init; } = "ru-RU";

    /// <summary>Базовый z-index для overlay-компонентов.</summary>
    public int BaseZIndex { get; init; } = 800;

    /// <summary>Шаг z-index между слоями.</summary>
    public int ZIndexStep { get; init; } = 10;

    /// <summary>Префикс CSS-классов.</summary>
    public string CssPrefix { get; init; } = "sg-";
}

// ── Форма ─────────────────────────────────────────────────────────────────────

/// <summary>Контекст EditForm/SgDataForm для дочерних полей.</summary>
public sealed class SgFormContext
{
    /// <summary>EditContext Blazor (для валидации).</summary>
    public EditContext? EditContext { get; init; }

    /// <summary>Форма в процессе отправки.</summary>
    public bool IsSubmitting { get; init; }

    /// <summary>Все поля только для чтения.</summary>
    public bool IsReadOnly { get; init; }

    /// <summary>Форма прошла валидацию.</summary>
    public bool IsValid { get; init; }

    /// <summary>Количество попыток отправки.</summary>
    public int SubmitCount { get; init; }
}

/// <summary>Контекст SgDataForm для дочерних полей формы.</summary>
public sealed class SgDataFormContext
{
    /// <summary>Родительский контекст формы.</summary>
    public SgFormContext? Form { get; init; }

    /// <summary>Показывать ошибки валидации.</summary>
    public bool ShowValidation { get; init; } = true;

    /// <summary>Все поля только для чтения.</summary>
    public bool IsReadOnly { get; init; }

    /// <summary>Позиция метки: "top" | "left" | "right".</summary>
    public string LabelPosition { get; init; } = "top";

    /// <summary>Ширина метки (при LabelPosition = "left"/"right"), напр. "120px".</summary>
    public string? LabelWidth { get; init; }

    /// <summary>Показывать двоеточие после метки.</summary>
    public bool Colon { get; init; } = true;
}

// ── DataGrid ──────────────────────────────────────────────────────────────────

/// <summary>
/// Контекст DataGrid для вложенных компонентов (колонки, фильтры, тулбары).
/// Non-generic: Blazor не поддерживает generic CascadingParameter.
/// Используйте ItemType для runtime type-safety.
/// </summary>
public sealed class SgDataGridContext
{
    /// <summary>Runtime-тип элемента (вместо generic T).</summary>
    public Type? ItemType { get; init; }

    /// <summary>Элементы текущей страницы (как object[]).</summary>
    public IReadOnlyList<object> Items { get; init; } = [];

    /// <summary>Размер страницы.</summary>
    public int PageSize { get; init; } = 25;

    /// <summary>Виртуализация включена.</summary>
    public bool IsVirtualized { get; init; }

    /// <summary>Функция получения ключа строки (для row identity).</summary>
    public Func<object, object>? KeySelector { get; init; }

    /// <summary>Группировка включена.</summary>
    public bool IsGrouped { get; init; }

    /// <summary>Текущий дескриптор сортировки.</summary>
    public SgSortDescriptor? CurrentSort { get; init; }

    /// <summary>Режим выделения строк.</summary>
    public SgSelectionMode SelectionMode { get; init; } = SgSelectionMode.None;

    /// <summary>Множество выделенных ключей строк.</summary>
    public IReadOnlySet<object> SelectedKeys { get; init; } = new HashSet<object>();
}

/// <summary>Контекст конкретной строки DataGrid (для ячеек и inline-edit).</summary>
public sealed class SgGridRowContext
{
    /// <summary>Данные строки (object, т.к. non-generic).</summary>
    public object? Item { get; init; }

    /// <summary>Индекс строки (0-based).</summary>
    public int RowIndex { get; init; }

    /// <summary>Строка выделена.</summary>
    public bool IsSelected { get; init; }

    /// <summary>Строка в режиме редактирования.</summary>
    public bool IsEditing { get; init; }

    /// <summary>Строка (группа) развёрнута.</summary>
    public bool IsExpanded { get; init; }

    /// <summary>Строка — строка-заглушка (placeholder) при виртуализации.</summary>
    public bool IsPlaceholder { get; init; }
}

// ── Tabs / Accordion ──────────────────────────────────────────────────────────

/// <summary>Контекст Tab-панели для дочерних вкладок.</summary>
public sealed class SgTabsContext
{
    /// <summary>ID активной вкладки.</summary>
    public string? ActiveTabId { get; init; }

    /// <summary>Callback выбора вкладки.</summary>
    public Action<string>? OnTabSelected { get; init; }

    /// <summary>Ориентация вкладок.</summary>
    public SgOrientation Orientation { get; init; } = SgOrientation.Horizontal;

    /// <summary>Вкладки уничтожаются при скрытии (false = hidden, true = destroyed).</summary>
    public bool DestroyOnHide { get; init; }
}

/// <summary>Контекст аккордеона для дочерних SgAccordionItem.</summary>
public sealed class SgAccordionContext
{
    /// <summary>Разрешено открывать несколько секций одновременно.</summary>
    public bool AllowMultiple { get; init; }

    /// <summary>
    /// Открытые секции (по ID).
    /// IReadOnlySet: изменение только через OnItemToggle.
    /// </summary>
    public IReadOnlySet<string> OpenItems { get; init; } = new HashSet<string>();

    /// <summary>Callback переключения секции.</summary>
    public Action<string>? OnItemToggle { get; init; }
}

// ── Menu / Navigation ─────────────────────────────────────────────────────────

/// <summary>Контекст меню для вложенных пунктов (SgContextMenu, SgMenu).</summary>
public sealed class SgMenuContext
{
    /// <summary>Меню открыто.</summary>
    public bool IsOpen { get; init; }

    /// <summary>Уровень вложенности (0 = корень).</summary>
    public int Level { get; init; }

    /// <summary>Callback закрытия меню.</summary>
    public Action? OnClose { get; init; }

    /// <summary>Ориентация меню.</summary>
    public SgOrientation Orientation { get; init; } = SgOrientation.Vertical;
}

// ── Virtualization ────────────────────────────────────────────────────────────

/// <summary>Контекст виртуализированного списка/дерева.</summary>
public sealed class SgVirtualContext
{
    /// <summary>Индекс первого видимого элемента.</summary>
    public int VisibleStartIndex { get; init; }

    /// <summary>Индекс последнего видимого элемента.</summary>
    public int VisibleEndIndex { get; init; }

    /// <summary>Общее количество элементов.</summary>
    public int TotalCount { get; init; }

    /// <summary>Высота одного элемента (px).</summary>
    public double ItemHeight { get; init; }

    /// <summary>Ширина одного элемента (px, для горизонтальной виртуализации).</summary>
    public double ItemWidth { get; init; }

    /// <summary>Количество pre-rendered элементов за пределами viewport.</summary>
    public int OverscanCount { get; init; } = 3;
}

// ── Splitter ──────────────────────────────────────────────────────────────────

/// <summary>Контекст SgSplitter для дочерних панелей.</summary>
public sealed class SgSplitterContext
{
    /// <summary>Ориентация разделителя.</summary>
    public SgOrientation Orientation { get; init; }

    /// <summary>Размеры панелей в процентах (sum = 100).</summary>
    public double[] PanelSizes { get; init; } = [];

    /// <summary>Callback изменения размера панели.</summary>
    public Action<int, double>? OnPanelResize { get; init; }

    /// <summary>Минимальный размер панели (%).</summary>
    public double MinPanelSize { get; init; } = 5.0;
}
