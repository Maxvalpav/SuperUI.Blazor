namespace SuperUI.Components;

public class SgGanttTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsMilestone { get; set; }
    public double Progress { get; set; } // 0 to 1
    public string? Color { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public string? ParentId { get; set; }
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
}
