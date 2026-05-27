using Microsoft.AspNetCore.Components;

namespace SuperUI.Components;

public class SgCommandGroup
{
    public string Title { get; set; } = "";
    public string? Icon { get; set; }
    public List<SgCommand> Commands { get; set; } = new();
}

public class SgCommand
{
    public string Text { get; set; } = "";
    public string? Description { get; set; }
    public RenderFragment? Icon { get; set; }
    public string? Shortcut { get; set; }
    public EventCallback OnExecute { get; set; }
}
