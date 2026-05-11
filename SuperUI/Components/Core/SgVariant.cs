namespace SuperUI.Core;

/// <summary>
/// Shared semantic intent shared across SuperUI components. Used for buttons, alerts,
/// chips, badges, toasts when a generic intent is appropriate. Component-specific
/// variant enums (e.g. <c>SgButtonVariant</c>) remain for shape/appearance choices that
/// do not carry semantic meaning.
/// </summary>
public enum SgVariant
{
    /// <summary>Default neutral appearance.</summary>
    Default,
    /// <summary>Primary action / brand color.</summary>
    Primary,
    /// <summary>Secondary action / muted brand color.</summary>
    Secondary,
    /// <summary>Positive outcome / confirmation.</summary>
    Success,
    /// <summary>Warning / requires attention.</summary>
    Warning,
    /// <summary>Destructive / error.</summary>
    Danger,
    /// <summary>Informational / neutral notice.</summary>
    Info,
    /// <summary>Transparent surface with hover affordance.</summary>
    Ghost,
    /// <summary>Renders as a hyperlink, no surface.</summary>
    Link
}
