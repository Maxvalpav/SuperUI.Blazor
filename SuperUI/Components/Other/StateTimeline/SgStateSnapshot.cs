using System;

namespace SuperUI.Components;

public class SgStateSnapshot<TState>
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public TState State { get; set; } = default!;
    public string Description { get; set; } = string.Empty;
}
