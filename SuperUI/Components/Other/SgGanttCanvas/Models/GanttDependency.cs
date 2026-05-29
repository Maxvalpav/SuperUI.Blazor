namespace SuperUI.Components.SgGanttCanvas.Models;

/// <summary>Types of task dependency relationships.</summary>
public enum DependencyType
{
    FinishToStart,
    StartToStart,
    FinishToFinish,
    StartToFinish
}

/// <summary>A dependency link between two Gantt tasks.</summary>
public class GanttDependency
{
    /// <summary>Unique dependency identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>ID of the predecessor task.</summary>
    public string FromTaskId { get; set; } = string.Empty;
    /// <summary>ID of the successor task.</summary>
    public string ToTaskId { get; set; } = string.Empty;
    /// <summary>Type of dependency relationship.</summary>
    public DependencyType Type { get; set; } = DependencyType.FinishToStart;
    /// <summary>Lag or lead time between the tasks.</summary>
    public TimeSpan Lag { get; set; } = TimeSpan.Zero;
    /// <summary>Optional color for the dependency line.</summary>
    public string? Color { get; set; }
}
