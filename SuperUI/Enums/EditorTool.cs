namespace SuperUI.Enums;

/// <summary>
/// Available tools in the <c>SgImageEditor</c> component.
/// </summary>
public enum EditorTool
{
    /// <summary>Default mode — no interactive tool active.</summary>
    Select,
    /// <summary>Crop mode — drag to define a crop region; Apply/Cancel buttons appear.</summary>
    Crop,
    /// <summary>Freehand drawing mode — draw on the image with the configured pen.</summary>
    Draw
}
