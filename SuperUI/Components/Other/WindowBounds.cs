namespace SuperUI.Components;

/// <summary>Snapshot of a dock-window's position and size after a drag or resize.</summary>
public readonly record struct WindowBounds(double Left, double Top, double Width, double Height);
