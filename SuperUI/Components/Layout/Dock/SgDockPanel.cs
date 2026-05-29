using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Defines the dock position of a panel inside <see cref="Components.SgDockManager"/>.
/// </summary>
public sealed class SgDockPanel
{
    /// <summary>Unique identifier for the panel.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>Tab title.</summary>
    public string Title { get; set; } = "";

    /// <summary>Optional SVG icon markup shown in the tab.</summary>
    public string? Icon { get; set; }

    /// <summary>Dock position. Default is <see cref="SgDockPosition.Center"/>.</summary>
    public SgDockPosition Position { get; set; } = SgDockPosition.Center;

    /// <summary>Whether the panel can be closed by the user. Default true.</summary>
    public bool Closable { get; set; } = true;

    /// <summary>Whether the panel is currently visible (not closed). Default true.</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Arbitrary data attached to the panel for use in templates.</summary>
    public object? Tag { get; set; }

    /// <summary>
    /// Creates a new builder for a dock panel.
    /// </summary>
    /// <param name="title">Panel title.</param>
    public static SgDockPanelBuilder Create(string title) => new(title);
}

/// <summary>
/// Fluent builder for <see cref="SgDockPanel"/>.
/// </summary>
public sealed class SgDockPanelBuilder
{
    private readonly SgDockPanel _panel = new();

    internal SgDockPanelBuilder(string title)
    {
        _panel.Title = title;
    }

    /// <summary>Sets the unique identifier for the panel.</summary>
    public SgDockPanelBuilder WithId(string id)
    {
        _panel.Id = id;
        return this;
    }

    /// <summary>Sets the SVG icon markup for the panel tab.</summary>
    public SgDockPanelBuilder WithIcon(string icon)
    {
        _panel.Icon = icon;
        return this;
    }

    /// <summary>Sets the dock position of the panel.</summary>
    public SgDockPanelBuilder At(SgDockPosition pos)
    {
        _panel.Position = pos;
        return this;
    }

    /// <summary>Sets whether the panel can be closed by the user.</summary>
    public SgDockPanelBuilder Closable(bool value = true)
    {
        _panel.Closable = value;
        return this;
    }

    /// <summary>Sets whether the panel is visible.</summary>
    public SgDockPanelBuilder Visible(bool value = true)
    {
        _panel.Visible = value;
        return this;
    }

    /// <summary>Attaches arbitrary data to the panel.</summary>
    public SgDockPanelBuilder WithTag(object? tag)
    {
        _panel.Tag = tag;
        return this;
    }

    /// <summary>Builds and returns the configured <see cref="SgDockPanel"/>.</summary>
    public SgDockPanel Build() => _panel;
}
