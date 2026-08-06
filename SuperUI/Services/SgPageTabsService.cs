namespace SuperUI.Services;

/// <summary>
/// Service for managing multi-tab navigation pages.
/// </summary>
public class SgPageTabsService
{
    private readonly List<SgPageTab> _tabs = new();
    private readonly object _lock = new();

    public IReadOnlyList<SgPageTab> Tabs
    {
        get
        {
            lock (_lock) return _tabs.ToArray();
        }
    }

    public SgPageTab? ActiveTab { get; private set; }

    public event Action? OnChanged;

    /// <summary>
    /// Opens a new tab or activates an existing one.
    /// </summary>
    public void OpenTab(string href, string title, string? icon = null)
    {
        lock (_lock)
        {
            var existing = _tabs.FirstOrDefault(t => t.Href.Equals(href, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                existing = new SgPageTab { Href = href, Title = title, Icon = icon };
                _tabs.Add(existing);
            }
            else
            {
                // Update title/icon if provided
                if (!string.IsNullOrEmpty(title)) existing.Title = title;
                if (!string.IsNullOrEmpty(icon)) existing.Icon = icon;
            }

            ActiveTab = existing;
        }
        NotifyChanged();
    }

    /// <summary>
    /// Removes a tab by its href.
    /// </summary>
    public void RemoveTab(string href)
    {
        lock (_lock)
        {
            var tab = _tabs.FirstOrDefault(t => t.Href.Equals(href, StringComparison.OrdinalIgnoreCase));
            if (tab == null) return;

            int index = _tabs.IndexOf(tab);
            _tabs.Remove(tab);

            if (ActiveTab == tab)
            {
                ActiveTab = _tabs.Count > 0 ? _tabs[Math.Min(index, _tabs.Count - 1)] : null;
            }
        }
        NotifyChanged();
    }

    /// <summary>
    /// Activates a tab by its href.
    /// </summary>
    public void SetActiveTab(string href)
    {
        lock (_lock)
        {
            var tab = _tabs.FirstOrDefault(t => t.Href.Equals(href, StringComparison.OrdinalIgnoreCase));
            if (tab == null || ActiveTab == tab) return;
            ActiveTab = tab;
        }
        NotifyChanged();
    }

    /// <summary>
    /// Clears all tabs.
    /// </summary>
    public void ClearAll()
    {
        lock (_lock)
        {
            _tabs.Clear();
            ActiveTab = null;
        }
        NotifyChanged();
    }

    private void NotifyChanged() => OnChanged?.Invoke();
}

public class SgPageTab
{
    public string Href { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
}
