// SuperUI/Base/SgEnums.cs
//
// Все перечисления SuperUI.
// Централизованы для единого импорта (@using SuperUI.Base).

namespace SuperUI.Base;

// ── Общие ────────────────────────────────────────────────────────────────────

/// <summary>Размер компонента.</summary>
public enum SgSize
{
    ExtraSmall,
    Small,
    Medium,
    Large,
    ExtraLarge
}

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

/// <summary>Позиционирование/размещение overlay-компонентов.</summary>
public enum SgPlacement
{
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
    // Toast/Notification placements
    TopRight,
    TopLeft,
    BottomRight,
    BottomLeft,
    TopCenter,
    BottomCenter
}

// ── Input-специфичные ────────────────────────────────────────────────────────

/// <summary>Вариант отображения поля ввода.</summary>
public enum SgInputVariant
{
    Outlined,
    Filled,
    Standard,
    Borderless
}

// ── Button-специфичные ───────────────────────────────────────────────────────

/// <summary>Вариант кнопки (псевдоним SgVariant для обратной совместимости).</summary>
public enum SgButtonVariant
{
    Default   = SgVariant.Default,
    Primary   = SgVariant.Primary,
    Secondary = SgVariant.Secondary,
    Success   = SgVariant.Success,
    Warning   = SgVariant.Warning,
    Danger    = SgVariant.Danger,
    Ghost     = SgVariant.Ghost,
    Link      = SgVariant.Link
}

// ── Badge/Alert-специфичные ──────────────────────────────────────────────────

public enum SgBadgeVariant { Default, Success, Danger, Warning, Info, Muted }

public enum SgAlertVariant { Default, Success, Warning, Danger, Info }

// ── Avatar ───────────────────────────────────────────────────────────────────

public enum SgAvatarShape { Circle, Square }

// ── DataGrid ─────────────────────────────────────────────────────────────────

/// <summary>Направление сортировки.</summary>
public enum SgSortDirection { None, Ascending, Descending }

/// <summary>Тип колонки.</summary>
public enum SgColumnType { Auto, Text, Number, Date, Boolean, Custom }

/// <summary>Режим выделения строк.</summary>
public enum SgSelectionMode { None, Single, Multiple }

// ── Overlay ──────────────────────────────────────────────────────────────────

public enum SgDrawerPlacement { Left, Right, Top, Bottom }

public enum SgModalSize { Small, Medium, Large, ExtraLarge, FullScreen }

// ── Progress ─────────────────────────────────────────────────────────────────

public enum SgProgressVariant { Default, Success, Warning, Danger, Info }

// ── Skeleton ─────────────────────────────────────────────────────────────────

public enum SgSkeletonVariant { Text, Circle, Rectangle, Button }

// ── Checkbox/Toggle ──────────────────────────────────────────────────────────

/// <summary>Три-состояние чекбокса.</summary>
public enum SgCheckState { Unchecked, Checked, Indeterminate }

// ── Orientation ──────────────────────────────────────────────────────────────

public enum SgOrientation { Horizontal, Vertical }

// ── Animation ─────────────────────────────────────────────────────────────────

public enum SgAnimation { None, Fade, Slide, Scale, Flip }
