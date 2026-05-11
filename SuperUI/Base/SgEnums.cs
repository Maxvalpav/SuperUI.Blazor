// SuperUI/Base/SgEnums.cs
namespace SuperUI.Base;

/// <summary>Вариант/тема компонента.</summary>
public enum SgVariant
{
    Default,
    Primary,
    Secondary,
    Outline,
    Text,
    Danger,
    Success,
    Warning,
    Info
}

/// <summary>Размер компонента (XS→XL).</summary>
public enum SgSize { XSmall, Small, Medium, Large, XLarge }

/// <summary>Направление размещения (для Tooltip, Drawer, Popover, Placement).</summary>
public enum SgPlacement
{
    // Абсолютные (для Drawer, Toast)
    TopLeft, TopCenter, TopRight,
    BottomLeft, BottomCenter, BottomRight,
    Left, Right, Top, Bottom,
    // Относительные (для Tooltip, Popover)
    StartTop, StartBottom, EndTop, EndBottom,
    TopStart, TopEnd, BottomStart, BottomEnd
}

/// <summary>Вариант отображения поля ввода.</summary>
public enum SgInputVariant { Outlined, Filled, Underlined }

/// <summary>Вариант кнопки.</summary>
public enum SgButtonVariant { Default, Primary, Secondary, Danger, Success, Warning, Ghost, Link }

/// <summary>Вариант бейджа.</summary>
public enum SgBadgeVariant { Default, Primary, Success, Danger, Warning, Info, Muted }

/// <summary>Вариант алерта/тоста.</summary>
public enum SgAlertVariant { Info, Success, Warning, Danger }

/// <summary>Форма аватара.</summary>
public enum SgAvatarShape { Circle, Square, Rounded }

/// <summary>Режим чекбокса (tri-state).</summary>
public enum SgCheckState { Unchecked, Checked, Indeterminate }

/// <summary>Ориентация компонента.</summary>
public enum SgOrientation { Horizontal, Vertical }

/// <summary>Режим анимации.</summary>
public enum SgAnimation { None, Fade, Slide, Scale, Flip }
