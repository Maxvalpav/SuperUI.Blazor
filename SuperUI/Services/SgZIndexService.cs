using System.Diagnostics;

namespace SuperUI.Services;

/// <summary>
/// Manages z-index layering for overlay components (modals, drawers, popovers, tooltips, toasts, portals).
/// Ensures proper stacking order when multiple overlays are open simultaneously.
/// Allocates monotonically increasing values per component type base, resetting when all entries are released.
/// </summary>
[DebuggerDisplay("Count = {Count}, CurrentZIndex = {CurrentZIndex}")]
public sealed class SgZIndexService
{
    /// <summary>Initial z-index value when no entries are allocated. Equal to <c>1000</c>.</summary>
    public const int DefaultInitialZIndex = 1000;

    private int _currentZIndex = DefaultInitialZIndex;
    private readonly List<ZIndexEntry> _entries = new();

    [DebuggerDisplay("Value = {Value}, Owner = {Owner}")]
    private sealed record ZIndexEntry(int Value, object Owner);

    /// <summary>Base z-index for modal dialogs.</summary>
    public const int ModalBase = 1100;

    /// <summary>Base z-index for drawer panels.</summary>
    public const int DrawerBase = 1200;

    /// <summary>Base z-index for popovers.</summary>
    public const int PopoverBase = 1300;

    /// <summary>Base z-index for tooltips.</summary>
    public const int TooltipBase = 1400;

    /// <summary>Base z-index for toast notifications.</summary>
    public const int ToastBase = 1500;

    /// <summary>Base z-index for floating windows (e.g. <c>SgDockWindow</c>).</summary>
    public const int WindowBase = 2000;

    /// <summary>Base z-index for portal teleported content.</summary>
    public const int PortalBase = 9000;

    /// <summary>Raised when the top-most overlay owner changes.</summary>
    public event Action<object?>? TopOwnerChanged;

    /// <summary>Number of active z-index allocations.</summary>
    public int Count => _entries.Count;

    /// <summary>Gets the current highest z-index in use, or <see cref="DefaultInitialZIndex"/> if none.</summary>
    public int CurrentZIndex => _entries.Count > 0 ? _entries[^1].Value : _currentZIndex;

    /// <summary>
    /// Checks whether the specified owner has an active z-index allocation.
    /// </summary>
    public bool IsActive(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return _entries.Exists(x => ReferenceEquals(x.Owner, owner));
    }

    /// <summary>
    /// Gets the allocated z-index for the specified owner.
    /// </summary>
    /// <returns>The z-index value, or <c>0</c> if the owner has no allocation.</returns>
    public int GetZIndex(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        var entry = _entries.Find(x => ReferenceEquals(x.Owner, owner));
        return entry?.Value ?? 0;
    }

    /// <summary>
    /// Tries to get the allocated z-index for the specified owner.
    /// </summary>
    /// <returns><c>true</c> if the owner has an active allocation.</returns>
    public bool TryGetZIndex(object owner, out int zIndex)
    {
        ArgumentNullException.ThrowIfNull(owner);
        var entry = _entries.Find(x => ReferenceEquals(x.Owner, owner));
        if (entry is not null)
        {
            zIndex = entry.Value;
            return true;
        }
        zIndex = 0;
        return false;
    }

    /// <summary>
    /// Allocates a new z-index for an overlay component and brings it to the top of its stack.
    /// If the owner already has an allocation, it is released and re-allocated at a higher value.
    /// </summary>
    /// <param name="owner">The owner object (usually the component instance). Must not be null.</param>
    /// <param name="baseZIndex">The base z-index for the component type (e.g. <see cref="ModalBase"/>).</param>
    /// <returns>The allocated z-index value.</returns>
    public int Allocate(object owner, int baseZIndex)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var oldTop = GetTopOwner();

        // Remove existing entry for this owner (re-allocate to bring to front)
        _entries.RemoveAll(x => ReferenceEquals(x.Owner, owner));

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
    /// Convenience: allocates and returns the z-index, applying <c>BringToFront</c> semantics.
    /// Same as calling <see cref="Release"/> then <see cref="Allocate"/> for the same owner.
    /// </summary>
    public int BringToFront(object owner, int baseZIndex)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return Allocate(owner, baseZIndex);
    }

    /// <summary>
    /// Releases a previously allocated z-index for the specified owner.
    /// If all entries are released, the internal counter resets to <see cref="DefaultInitialZIndex"/>.
    /// </summary>
    /// <param name="owner">The owner object to release.</param>
    public void Release(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var oldTop = GetTopOwner();
        _entries.RemoveAll(x => ReferenceEquals(x.Owner, owner));
        var newTop = GetTopOwner();

        if (_entries.Count == 0)
        {
            _currentZIndex = DefaultInitialZIndex;
        }

        if (!ReferenceEquals(oldTop, newTop))
        {
            TopOwnerChanged?.Invoke(newTop);
        }
    }

    /// <summary>Gets the owner of the top-most (highest z-index) overlay.</summary>
    public object? GetTopOwner() => _entries.Count > 0 ? _entries[^1].Owner : null;

    /// <summary>Resets all allocations (useful for testing).</summary>
    public void Reset()
    {
        _currentZIndex = DefaultInitialZIndex;
        _entries.Clear();
    }
}
