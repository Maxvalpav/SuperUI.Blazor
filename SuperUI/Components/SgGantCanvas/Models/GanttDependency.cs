namespace SuperUI.Components.SgGantCanvas.Models;

public enum DependencyType
{
    FinishToStart,
    StartToStart,
    FinishToFinish,
    StartToFinish
}

public class GanttDependency
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FromTaskId { get; set; } = string.Empty;
    public string ToTaskId { get; set; } = string.Empty;
    public DependencyType Type { get; set; } = DependencyType.FinishToStart;
    public TimeSpan Lag { get; set; } = TimeSpan.Zero;
    public string? Color { get; set; }
}
