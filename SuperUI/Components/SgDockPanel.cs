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

    public SgDockPanelBuilder WithId(string id)
    {
        _panel.Id = id;
        return this;
    }

    public SgDockPanelBuilder WithIcon(string icon)
    {
        _panel.Icon = icon;
        return this;
    }

    public SgDockPanelBuilder At(SgDockPosition pos)
    {
        _panel.Position = pos;
        return this;
    }

    public SgDockPanelBuilder Closable(bool value = true)
    {
        _panel.Closable = value;
        return this;
    }

    public SgDockPanelBuilder Visible(bool value = true)
    {
        _panel.Visible = value;
        return this;
    }

    public SgDockPanelBuilder WithTag(object? tag)
    {
        _panel.Tag = tag;
        return this;
    }

    public SgDockPanel Build() => _panel;
}
