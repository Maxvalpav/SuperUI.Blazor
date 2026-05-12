// SuperUI/Base/SgEnums.cs
//
// ИСПРАВЛЕНИЯ:
// ✅ CS0101: SgExportFormat — единственное объявление (было в SgDataBase.cs тоже)
// ✅ CS0117: SgPlacement.TopRight — добавлено (использовалось в SgToastHost и SgToastService)
// ✅ Obsolete-алиасы Asc/Desc сохранены для обратной совместимости

namespace SuperUI.Base;

// ── Общие ────────────────────────────────────────────────────────────────────

/// <summary>Вариант/тема компонента.</summary>
public enum SgVariant
{
    Default,
    Primary,
    Secondary,
    Success,
    Warning,
    Danger,
    Info,
    Ghost,
    Link
}

/// <summary>
/// Позиционирование/размещение overlay-компонентов и уведомлений.
/// ✅ FIX CS0117: добавлены TopRight, TopLeft, BottomRight, BottomLeft, TopCenter, BottomCenter
/// </summary>
public enum SgPlacement
{
    // Popup/Tooltip placements
    Top,
    TopStart,
    TopEnd,
    Bottom,
    BottomStart,
    BottomEnd,
    Left,
    LeftStart,
    LeftEnd,
    Right,
    RightStart,
    RightEnd,
    
    // Toast/Notification placements ← FIX CS0117
    TopRight,
    TopLeft,
    BottomRight,
    BottomLeft,
    TopCenter,
    BottomCenter
}

// ── Input ────────────────────────────────────────────────────────────────────

/// <summary>Вариант отображения поля ввода.</summary>
public enum SgInputVariant
{
    Outlined,
    Filled,
    Standard,
    Borderless
}

// ── Avatar ───────────────────────────────────────────────────────────────────

/// <summary>Форма аватара.</summary>
public enum SgAvatarShape
{
    Circle,
    Square
}

// ── DataGrid / Сортировка ────────────────────────────────────────────────────

/// <summary>Направление сортировки.</summary>
public enum SgSortDirection
{
    /// <summary>Сортировка не применена.</summary>
    None,
    
    /// <summary>По возрастанию (A→Z, 0→9).</summary>
    Ascending,
    
    /// <summary>По убыванию (Z→A, 9→0).</summary>
    Descending,
    
    // ── Обратная совместимость ─────────────────────────────────────────────
    
    /// <summary>Устаревший алиас. Используйте <see cref="Ascending"/>.</summary>
    [Obsolete("Use SgSortDirection.Ascending. Asc will be removed in v2.0.")]
    Asc = Ascending,
    
    /// <summary>Устаревший алиас. Используйте <see cref="Descending"/>.</summary>
    [Obsolete("Use SgSortDirection.Descending. Desc will be removed in v2.0.")]
    Desc = Descending
}

/// <summary>Тип колонки DataGrid.</summary>
public enum SgColumnType
{
    Auto,
    Text,
    Number,
    Date,
    Boolean,
    Custom
}

/// <summary>Режим выделения строк.</summary>
public enum SgSelectionMode
{
    None,
    Single,
    Multiple
}

// ── Overlay ──────────────────────────────────────────────────────────────────

/// <summary>Расположение выдвижной панели (Drawer).</summary>
public enum SgDrawerPlacement
{
    Left,
    Right,
    Top,
    Bottom
}

/// <summary>Размер модального окна.</summary>
public enum SgModalSize
{
    Small,
    Medium,
    Large,
    ExtraLarge,
    FullScreen
}

// ── Progress ─────────────────────────────────────────────────────────────────

/// <summary>Вариант полосы прогресса.</summary>
public enum SgProgressVariant
{
    Default,
    Success,
    Warning,
    Danger,
    Info
}

// ── Skeleton ─────────────────────────────────────────────────────────────────

/// <summary>Форма скелетона загрузки.</summary>
public enum SgSkeletonVariant
{
    Text,
    Circle,
    Rectangle,
    Button
}

// ── Checkbox ─────────────────────────────────────────────────────────────────

/// <summary>Три-состояние чекбокса.</summary>
public enum SgCheckState
{
    Unchecked,
    Checked,
    Indeterminate
}

// ── Animation ────────────────────────────────────────────────────────────────

/// <summary>Тип анимации.</summary>
public enum SgAnimation
{
    None,
    Fade,
    Slide,
    Scale,
    Flip
}

// ── Экспорт ──────────────────────────────────────────────────────────────────

/// <summary>
/// Формат экспорта данных.
/// ✅ FIX CS0101: единственное объявление (убрано из SgDataBase.cs и SgDataTypes.cs).
/// </summary>
public enum SgExportFormat
{
    Csv,
    Excel,
    Json,
    Pdf
}

// ── Тема ─────────────────────────────────────────────────────────────────────

/// <summary>Тема оформления.</summary>
public enum SgTheme
{
    Light,
    Dark,
    Auto
}

// ── Состояние загрузки ───────────────────────────────────────────────────────

/// <summary>Детализированное состояние загрузки компонента.</summary>
public enum SgLoadingState
{
    Idle,
    Loading,
    Success,
    Error,
    Empty
}

// ── Resize / Splitter ────────────────────────────────────────────────────────

/// <summary>Позиции захватчика для изменения размера.</summary>
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

// ── TreeView ─────────────────────────────────────────────────────────────────

/// <summary>Состояние узла дерева.</summary>
public enum SgTreeNodeState
{
    Collapsed,
    Expanded,
    Loading,
    Error
}

// ── Render ───────────────────────────────────────────────────────────────────

/// <summary>Режим рендеринга (оптимизация).</summary>
public enum SgRenderMode
{
    Auto,
    Force,
    Manual
}

// ── DataGrid Row ─────────────────────────────────────────────────────────────

/// <summary>Тип строки DataGrid.</summary>
public enum SgRowType
{
    Data,
    Group,
    Footer,
    Placeholder
}

// ── Toast ────────────────────────────────────────────────────────────────────

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

// ── Confirm ──────────────────────────────────────────────────────────────────

/// <summary>Вариант подтверждения.</summary>
public enum SgConfirmVariant
{
    Default,
    Info,
    Warning,
    Danger,
    Success
}

// ── Notification ─────────────────────────────────────────────────────────────

/// <summary>Тип уведомления в ленте.</summary>
public enum SgNotificationType
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>Позиционирование элемента.</summary>
public enum SgPosition
{
    Static,
    Relative,
    Absolute,
    Fixed,
    Sticky
}

/// <summary>Точка останова для responsive дизайна.</summary>
public enum SgBreakpoint
{
    Xs,
    Sm,
    Md,
    Lg,
    Xl,
    Xxl
}

/// <summary>Выравнивание текста.</summary>
public enum SgTextAlign
{
    Left,
    Center,
    Right,
    Justify
}

/// <summary>Приоритет рендеринга.</summary>
public enum SgRenderPriority
{
    Low,
    Normal,
    High,
    Critical
}
