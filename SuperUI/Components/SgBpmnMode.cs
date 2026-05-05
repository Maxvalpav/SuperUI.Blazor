namespace SuperUI.Components;

/// <summary>Operating mode for the <see cref="SgBpmn"/> component.</summary>
public enum SgBpmnMode
{
    /// <summary>Full editor with palette, context pad, and command stack.</summary>
    Modeler,
    /// <summary>Read-only viewer (lightweight, no palette).</summary>
    Viewer,
    /// <summary>Read-only viewer with pan / zoom navigation.</summary>
    NavigatedViewer,
}
