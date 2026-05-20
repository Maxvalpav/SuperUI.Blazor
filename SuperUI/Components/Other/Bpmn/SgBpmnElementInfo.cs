namespace SuperUI.Components;

/// <summary>Snapshot of a BPMN element passed to .NET event callbacks.</summary>
public sealed class SgBpmnElementInfo
{
    /// <summary>Stable BPMN id (e.g. <c>Activity_1abc</c>).</summary>
    public string Id { get; set; } = "";
    /// <summary>BPMN element type (e.g. <c>bpmn:Task</c>, <c>bpmn:SequenceFlow</c>).</summary>
    public string Type { get; set; } = "";
    /// <summary>Optional business label (<c>name</c> attribute).</summary>
    public string? Name { get; set; }
    /// <summary>Source element id when this element is a connection.</summary>
    public string? SourceId { get; set; }
    /// <summary>Target element id when this element is a connection.</summary>
    public string? TargetId { get; set; }
}
