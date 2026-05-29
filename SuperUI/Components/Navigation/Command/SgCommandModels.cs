using Microsoft.AspNetCore.Components;

namespace SuperUI.Components;

/// <summary>A group of related commands in the command palette.</summary>
public class SgCommandGroup
{
    /// <summary>Group display title.</summary>
    public string Title { get; set; } = "";
    /// <summary>Optional SVG icon markup for the group.</summary>
    public string? Icon { get; set; }
    /// <summary>Commands belonging to this group.</summary>
    public List<SgCommand> Commands { get; set; } = new();
}

/// <summary>A single command shown in the command palette.</summary>
public class SgCommand
{
    /// <summary>Display text for the command.</summary>
    public string Text { get; set; } = "";
    /// <summary>Optional description shown below the command text.</summary>
    public string? Description { get; set; }
    /// <summary>Optional icon rendered before the command text.</summary>
    public RenderFragment? Icon { get; set; }
    /// <summary>Keyboard shortcut hint displayed next to the command.</summary>
    public string? Shortcut { get; set; }
    /// <summary>Callback invoked when the command is executed.</summary>
    public EventCallback OnExecute { get; set; }
}
