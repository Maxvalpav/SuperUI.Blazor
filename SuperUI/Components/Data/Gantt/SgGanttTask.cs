namespace SuperUI.Components;

/// <summary>Represents a task in the <see cref="SgGantt"/> chart.</summary>
public class SgGanttTask
{
    /// <summary>Unique identifier for the task.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>The display name of the task.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>The start date of the task.</summary>
    public DateTime Start { get; set; }
    /// <summary>The end date of the task.</summary>
    public DateTime End { get; set; }
    /// <summary>Whether this task is a milestone (zero-duration marker).</summary>
    public bool IsMilestone { get; set; }
    /// <summary>Completion progress as a value between 0 and 1.</summary>
    public double Progress { get; set; }
    /// <summary>The bar color for the task. If null, a default color is used.</summary>
    public string? Color { get; set; }
    /// <summary>IDs of tasks this task depends on (predecessors).</summary>
    public List<string> Dependencies { get; set; } = new();
    /// <summary>ID of the parent task for hierarchical grouping.</summary>
    public string? ParentId { get; set; }
    /// <summary>Whether child tasks are visible when this task is a parent.</summary>
    public bool Expanded { get; set; } = true;

    /// <summary>
    /// Initial planned start date.
    /// </summary>
    public DateTime? BaselineStart { get; set; }

    /// <summary>
    /// Initial planned end date.
    /// </summary>
    public DateTime? BaselineEnd { get; set; }

    /// <summary>
    /// Indicates if the task is on the critical path.
    /// </summary>
    public bool IsCritical { get; set; }

    /// <summary>
    /// Task description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Custom tag object.
    /// </summary>
    public object? Tag { get; set; }
}
