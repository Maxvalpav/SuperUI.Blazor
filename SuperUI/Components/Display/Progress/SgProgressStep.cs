namespace SuperUI.Components;

/// <summary>Represents a single step in the SgProgress Steps display.</summary>
public class SgProgressStep
{
    /// <summary>The display label for this step.</summary>
    public string Label { get; set; } = "";

    /// <summary>Optional description shown below the label.</summary>
    public string? Description { get; set; }

    /// <summary>Optional icon string (emoji or icon font character) shown inside the step indicator.</summary>
    public string? Icon { get; set; }

    /// <summary>Whether this step has been completed. Completed steps show a filled indicator.</summary>
    public bool Completed { get; set; }

    /// <summary>Whether this step is currently active. The active step uses the accent color.</summary>
    public bool Active { get; set; }

    /// <summary>Whether this step is in an error state. Error steps use a danger color.</summary>
    public bool Error { get; set; }
}
