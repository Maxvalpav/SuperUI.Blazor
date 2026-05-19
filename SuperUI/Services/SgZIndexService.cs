namespace SuperUI.Services;

/// <summary>
/// Manages z-index layering for overlay components (modals, drawers, popovers, tooltips, toasts).
/// Ensures proper stacking order when multiple overlays are open simultaneously.
/// </summary>
public sealed class SgZIndexService
{
    private int _currentZIndex = 1000;
    private readonly List<ZIndexEntry> _entries = new();

    private sealed record ZIndexEntry(int Value, object Owner);

    /// <summary>
    /// Base z-index for floating windows.
    /// </summary>
    public const int WindowBase = 2000;

    /// <summary>
    /// Base z-index for backdrop layers.
    /// </summary>
    public const int BackdropBase = 1000;

    /// <summary>
    /// Base z-index for modal dialogs.
    /// </summary>
    public const int ModalBase = 1100;

    /// <summary>
    /// Base z-index for drawer panels.
    /// </summary>
    public const int DrawerBase = 1200;

    /// <summary>
    /// Base z-index for popovers.
    /// </summary>
    public const int PopoverBase = 1300;

    /// <summary>
    /// Base z-index for tooltips.
    /// </summary>
    public const int TooltipBase = 1400;

    /// <summary>
    /// Base z-index for toast notifications.
    /// </summary>
    public const int ToastBase = 1500;

    /// <summary>
    /// Event raised when the top-most overlay owner changes.
    /// </summary>
    public event Action<object?>? TopOwnerChanged;

    /// <summary>
    /// Allocates a new z-index for an overlay component.
    /// </summary>
    /// <param name="owner">The owner object (usually the component instance).</param>
    /// <param name="baseZIndex">The base z-index for the component type.</param>
    /// <returns>The allocated z-index value.</returns>
    public int Allocate(object owner, int baseZIndex = ModalBase)
    {
        var oldTop = GetTopOwner();

        // Remove existing entry for this owner if any
        _entries.RemoveAll(x => x.Owner == owner);

        _currentZIndex = Math.Max(_currentZIndex, baseZIndex) + 10;
        _entries.Add(new ZIndexEntry(_currentZIndex, owner));

        var newTop = GetTopOwner();
        if (!ReferenceEquals(oldTop, newTop))
        {
            TopOwnerChanged?.Invoke(newTop);
        }

        return _currentZIndex;
    }

    /// <summary>
    /// Releases a previously allocated z-index.
    /// </summary>
    /// <param name="owner">The owner object to release.</param>
    public void Release(object owner)
    {
        var oldTop = GetTopOwner();
        _entries.RemoveAll(x => x.Owner == owner);
        var newTop = GetTopOwner();

        if (_entries.Count == 0)
        {
            _currentZIndex = 1000;
        }

        if (!ReferenceEquals(oldTop, newTop))
        {
            TopOwnerChanged?.Invoke(newTop);
        }
    }

    /// <summary>
    /// Gets the owner of the top-most overlay.
    /// </summary>
    public object? GetTopOwner() => _entries.Count > 0 ? _entries[^1].Owner : null;

    /// <summary>
    /// Gets the current highest z-index in use.
    /// </summary>
    public int CurrentZIndex => _entries.Count > 0 ? _entries[^1].Value : _currentZIndex;

    /// <summary>
    /// Resets the z-index allocation (useful for testing).
    /// </summary>
    public void Reset()
    {
        _currentZIndex = 1000;
        _entries.Clear();
    }
}
