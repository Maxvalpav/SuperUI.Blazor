using Microsoft.AspNetCore.Components;

namespace SuperUI.Components;

/// <summary>
/// Represents a command item in the SgCommandBar component.
/// </summary>
public sealed class CommandBarItem
{
    /// <summary>
    /// Unique identifier for the command.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[0..8];

    /// <summary>
    /// The display text for the command.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The icon to display (use SgIcons constants).
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// The keyboard shortcut text to display (e.g., "Ctrl+S").
    /// </summary>
    public string? Shortcut { get; set; }

    /// <summary>
    /// Whether the command is disabled.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Whether the command is visible.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Whether the command is in a toggle state.
    /// </summary>
    public bool IsToggle { get; set; }

    /// <summary>
    /// The toggle state for toggle commands.
    /// </summary>
    public bool IsPressed { get; set; }

    /// <summary>
    /// Tooltip text to display on hover.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Required permission to see this command.
    /// </summary>
    public string? RequiredPermission { get; set; }

    /// <summary>
    /// Required role to see this command.
    /// </summary>
    public string? RequiredRole { get; set; }

    /// <summary>
    /// Whether this item is a separator.
    /// </summary>
    public bool IsSeparator { get; set; }

    /// <summary>
    /// Whether this item opens a submenu.
    /// </summary>
    public bool HasSubmenu => Submenu?.Any() == true;

    /// <summary>
    /// Submenu items for dropdown commands.
    /// </summary>
    public List<CommandBarItem>? Submenu { get; set; }

    /// <summary>
    /// The click handler for the command.
    /// </summary>
    public EventCallback<CommandBarItem> OnClick { get; set; }

    /// <summary>
    /// Custom content render fragment.
    /// </summary>
    public RenderFragment? Content { get; set; }

    /// <summary>
    /// The priority of the command (higher = more important, stays visible longer when resizing).
    /// </summary>
    public int Priority { get; set; } = 50;

    /// <summary>
    /// Creates a separator item.
    /// </summary>
    public static CommandBarItem Separator() => new() { IsSeparator = true };

    /// <summary>
    /// Creates a command item with the specified text and optional icon.
    /// </summary>
    public static CommandBarItem Create(string text, string? icon = null, Action<CommandBarItem>? onClick = null)
    {
        var item = new CommandBarItem { Text = text, Icon = icon };
        if (onClick != null)
        {
            item.OnClick = EventCallback.Factory.Create<CommandBarItem>(item, onClick);
        }
        return item;
    }
}
