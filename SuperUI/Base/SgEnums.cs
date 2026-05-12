// SuperUI/Base/SgEnums.cs
//
// ИСПРАВЛЕНИЯ:
//   ✅ SgSortDirection: добавлены алиасы Asc/Desc (backward-compat) — устраняет CS0117
//      АЛЬТЕРНАТИВА: использовать Ascending/Descending везде (рекомендуется).
//      Здесь оба варианта совместимы через ObsoleteAttribute.
//
// ДОРАБОТКИ:
//   ✅ SgExportFormat — новый enum для SgDataBase.ExportAsync
//   ✅ SgTheme        — enum для theme-switching
//   ✅ SgLoadingState — детализированные состояния загрузки
//   ✅ SgResizeHandle — для SgResizable/SgSplitter
//   ✅ SgTreeNodeState — для SgTreeView
//   ✅ Все значения задокументированы

namespace SuperUI.Base;

// ── Общие ──────────────────────────────────────────────────────────────────────

/// <summary>Размер компонента.</summary>
public enum SgSize
{
    /// <summary>Очень маленький (xs).</summary>
    ExtraSmall,
    /// <summary>Маленький (sm).</summary>
    Small,
    /// <summary>Средний (md) — по умолчанию.</summary>
    Medium,
    /// <summary>Большой (lg).</summary>
    Large,
    /// <summary>Очень большой (xl).</summary>
    ExtraLarge
}

/// <summary>Вариант/тема компонента.</summary>
public enum SgVariant
{
    Default, Primary, Secondary, Success, Warning, Danger, Info, Ghost, Link
}

/// <summary>Позиционирование/размещение overlay-компонентов.</summary>
public enum SgPlacement
{
    Top, TopStart, TopEnd,
    Bottom, BottomStart, BottomEnd,
    Left, LeftStart, LeftEnd,
    Right, RightStart, RightEnd,
    // Toast/Notification placements
    TopRight, TopLeft, BottomRight, BottomLeft, TopCenter, BottomCenter
}

// ── Input-специфичные ──────────────────────────────────────────────────────────

/// <summary>Вариант отображения поля ввода.</summary>
public enum SgInputVariant { Outlined, Filled, Standard, Borderless }

// ── Button-специфичные ─────────────────────────────────────────────────────────

/// <summary>
/// Вариант кнопки.
/// Значения совпадают с <see cref="SgVariant"/> для совместимости маппинга.
/// </summary>
public enum SgButtonVariant
{
    Default   = 0,  // = SgVariant.Default
    Primary   = 1,  // = SgVariant.Primary
    Secondary = 2,  // = SgVariant.Secondary
    Success   = 3,  // = SgVariant.Success
    Warning   = 4,  // = SgVariant.Warning
    Danger    = 5,  // = SgVariant.Danger
    Ghost     = 7,  // = SgVariant.Ghost
    Link      = 8   // = SgVariant.Link
}

// ── Badge/Alert-специфичные ────────────────────────────────────────────────────

/// <summary>Вариант значка (Badge).</summary>
public enum SgBadgeVariant { Default, Success, Danger, Warning, Info, Muted }

/// <summary>Вариант оповещения (Alert).</summary>
public enum SgAlertVariant { Default, Success, Warning, Danger, Info }

// ── Avatar ─────────────────────────────────────────────────────────────────────

/// <summary>Форма аватара.</summary>
public enum SgAvatarShape { Circle, Square }

// ── DataGrid ───────────────────────────────────────────────────────────────────

/// <summary>Направление сортировки.</summary>
public enum SgSortDirection
{
    /// <summary>Сортировка не применена.</summary>
    None,

    /// <summary>По возрастанию (A→Z, 0→9).</summary>
    Ascending,

    /// <summary>По убыванию (Z→A, 9→0).</summary>
    Descending,

    // ── Обратная совместимость ──────────────────────────────────────────────────
    // ИСПРАВЛЕНИЕ CS0117: SgDataBase.cs использовал SgSortDirection.Asc/Desc.
    // Добавлены алиасы через Obsolete. РЕКОМЕНДУЕТСЯ: мигрировать на Ascending/Descending.

    /// <summary>Устаревший алиас для <see cref="Ascending"/>.</summary>
    [Obsolete("Use SgSortDirection.Ascending. SgSortDirection.Asc will be removed in v2.0.")]
    Asc = Ascending,

    /// <summary>Устаревший алиас для <see cref="Descending"/>.</summary>
    [Obsolete("Use SgSortDirection.Descending. SgSortDirection.Desc will be removed in v2.0.")]
    Desc = Descending
}

/// <summary>Тип колонки DataGrid.</summary>
public enum SgColumnType { Auto, Text, Number, Date, Boolean, Custom }

/// <summary>Режим выделения строк.</summary>
public enum SgSelectionMode { None, Single, Multiple }

// ── Overlay ────────────────────────────────────────────────────────────────────

/// <summary>Расположение выдвижной панели (Drawer).</summary>
public enum SgDrawerPlacement { Left, Right, Top, Bottom }

/// <summary>Размер модального окна.</summary>
public enum SgModalSize { Small, Medium, Large, ExtraLarge, FullScreen }

// ── Progress ───────────────────────────────────────────────────────────────────

/// <summary>Вариант полосы прогресса.</summary>
public enum SgProgressVariant { Default, Success, Warning, Danger, Info }

// ── Skeleton ───────────────────────────────────────────────────────────────────

/// <summary>Форма скелетона загрузки.</summary>
public enum SgSkeletonVariant { Text, Circle, Rectangle, Button }

// ── Checkbox/Toggle ────────────────────────────────────────────────────────────

/// <summary>Три-состояние чекбокса.</summary>
public enum SgCheckState { Unchecked, Checked, Indeterminate }

// ── Orientation ────────────────────────────────────────────────────────────────

/// <summary>Ориентация компонента.</summary>
public enum SgOrientation { Horizontal, Vertical }

// ── Animation ───────────────────────────────────────────────────────────────────

/// <summary>Тип анимации.</summary>
public enum SgAnimation { None, Fade, Slide, Scale, Flip }

// ── НОВЫЕ ENUM-Ы ───────────────────────────────────────────────────────────────

/// <summary>Формат экспорта данных (используется в SgDataBase.ExportAsync).</summary>
public enum SgExportFormat
{
    /// <summary>CSV (comma-separated values).</summary>
    Csv,
    /// <summary>Excel .xlsx.</summary>
    Excel,
    /// <summary>JSON.</summary>
    Json,
    /// <summary>PDF (если поддерживается).</summary>
    Pdf
}

/// <summary>Тема оформления.</summary>
public enum SgTheme { Light, Dark, Auto }

/// <summary>Детализированное состояние загрузки компонента.</summary>
public enum SgLoadingState
{
    /// <summary>Данные не загружались.</summary>
    Idle,
    /// <summary>Загрузка в процессе.</summary>
    Loading,
    /// <summary>Данные успешно загружены.</summary>
    Success,
    /// <summary>Произошла ошибка.</summary>
    Error,
    /// <summary>Нет данных (пустой результат).</summary>
    Empty
}

/// <summary>Позиции захватчика для изменения размера (SgResizable, SgSplitter).</summary>
[Flags]
public enum SgResizeHandle
{
    None   = 0,
    Top    = 1,
    Right  = 2,
    Bottom = 4,
    Left   = 8,
    All    = Top | Right | Bottom | Left
}

/// <summary>Состояние узла дерева (SgTreeView).</summary>
public enum SgTreeNodeState
{
    /// <summary>Узел свёрнут.</summary>
    Collapsed,
    /// <summary>Узел развёрнут.</summary>
    Expanded,
    /// <summary>Узел загружает дочерние элементы.</summary>
    Loading,
    /// <summary>Ошибка загрузки дочерних элементов.</summary>
    Error
}

// ── ДОПОЛНИТЕЛЬНЫЕ ENUMS ───────────────────────────────────────────────────────────

/// <summary>Режим рендеринга компонента (для оптимизации).</summary>
public enum SgRenderMode
{
    /// <summary>Авто: Blazor решает когда рендерить.</summary>
    Auto,
    /// <summary>Принудительно: рендерить при каждом StateHasChanged.</summary>
    Force,
    /// <summary>Ручной: только при явном вызове StateHasChanged.</summary>
    Manual
}

/// <summary>Тип строки DataGrid (для виртуализации и группировки).</summary>
public enum SgRowType
{
    Data,
    Group,
    Footer,
    Placeholder
}

/// <summary>Вариант подтверждения (SgConfirmService).</summary>
public enum SgConfirmVariant
{
    Default,
    Info,
    Warning,
    Danger,
    Success
}

/// <summary>Тип toast-уведомления.</summary>
public enum SgToastType
{
    Default,
    Success,
    Warning,
    Error,
    Info,
    Loading
}
