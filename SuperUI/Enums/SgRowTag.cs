namespace SuperUI.Enums;

/// <summary>
/// HTML tag rendered by the <c>SgRow</c> container.
/// Default is <see cref="Div"/>. Use semantic alternatives
/// (<see cref="Nav"/>, <see cref="Section"/>, etc.) to improve
/// accessibility/SEO without changing visual behaviour.
/// </summary>
public enum SgRowTag
{
    /// <summary>Generic block container.</summary>
    Div,
    /// <summary>Section of related content.</summary>
    Section,
    /// <summary>Header / banner.</summary>
    Header,
    /// <summary>Footer / closing content.</summary>
    Footer,
    /// <summary>Navigation block.</summary>
    Nav,
    /// <summary>Main content area.</summary>
    Main,
    /// <summary>List of items (children must be <c>&lt;li&gt;</c>).</summary>
    Ul,
    /// <summary>Self-contained composition.</summary>
    Article,
    /// <summary>Tangentially related content (sidebars).</summary>
    Aside
}
