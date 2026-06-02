using Microsoft.AspNetCore.Components;
using SuperUI.Components;
using SuperUI.Enums;

namespace SuperUI.Services;

/// <summary>
/// Service for programmatic drawer management.
/// Register in DI: <c>builder.Services.AddScoped&lt;SgDrawerService&gt;()</c>.
/// Host a <c>SgDrawerProvider</c> in MainLayout to render drawers opened via this service.
/// </summary>
public class SgDrawerService
{
    private readonly object _gate = new();
    private readonly List<DrawerInstance> _drawers = new();

    /// <summary>Иммутабельный снимок всех открытых drawer'ов (безопасно для итерации из подписчиков).</summary>
    public IReadOnlyList<DrawerInstance> Drawers
    {
        get { lock (_gate) { return _drawers.ToArray(); } }
    }

    /// <summary>Количество открытых drawer'ов.</summary>
    public int Count
    {
        get { lock (_gate) { return _drawers.Count; } }
    }

    /// <summary>Проверяет, открыт ли указанный drawer.</summary>
    public bool IsOpen(DrawerInstance instance)
    {
        if (instance is null) return false;
        lock (_gate) { return _drawers.Contains(instance); }
    }

    public event Action? StateChanged;

    /// <summary>
    /// Opens a drawer with the specified configuration.
    /// Returns a <see cref="DrawerInstance"/> that can be used to close the drawer programmatically.
    /// </summary>
    public DrawerInstance Show(string title, RenderFragment body, Action<DrawerConfig>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(body);
        var config = new DrawerConfig();
        configure?.Invoke(config);

        var instance = new DrawerInstance
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title,
            Body = body,
            Config = config,
            Visible = true
        };

        instance.OnClose = () => Close(instance);

        lock (_gate) { _drawers.Add(instance); }
        RaiseStateChanged();
        return instance;
    }

    /// <summary>
    /// Opens a drawer with typed content component.
    /// </summary>
    public DrawerInstance Show<T>(string title, Action<DrawerConfig>? configure = null) where T : IComponent
    {
        return Show(title, builder =>
        {
            builder.OpenComponent<T>(0);
            builder.CloseComponent();
        }, configure);
    }

    /// <summary>
    /// Closes the specified drawer instance.
    /// </summary>
    public void Close(DrawerInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        bool changed;
        lock (_gate)
        {
            instance.Visible = false;
            changed = _drawers.Remove(instance);
        }
        if (changed) RaiseStateChanged();
    }

    /// <summary>
    /// Closes the drawer with the specified id.
    /// </summary>
    public bool CloseById(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        DrawerInstance? found = null;
        lock (_gate)
        {
            foreach (var d in _drawers)
            {
                if (d.Id == id) { found = d; break; }
            }
            if (found is null) return false;
            found.Visible = false;
            _drawers.Remove(found);
        }
        RaiseStateChanged();
        return true;
    }

    /// <summary>
    /// Closes all open drawers.
    /// </summary>
    public void CloseAll()
    {
        bool changed;
        lock (_gate)
        {
            foreach (var d in _drawers) d.Visible = false;
            changed = _drawers.Count > 0;
            _drawers.Clear();
        }
        if (changed) RaiseStateChanged();
    }

    private void RaiseStateChanged()
    {
        // snapshot to avoid concurrent modification during dispatch
        var handler = StateChanged;
        if (handler is null) return;
        foreach (var d in handler.GetInvocationList())
        {
            try { ((Action)d).Invoke(); }
            catch { /* one subscriber must not break others */ }
        }
    }
}

public class DrawerInstance
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public RenderFragment? Body { get; set; }
    public DrawerConfig Config { get; set; } = new();
    public bool Visible { get; set; }
    public Action? OnClose { get; set; }
}

public class DrawerConfig
{
    public SgPlacement Placement { get; set; } = SgPlacement.Right;
    public string Size { get; set; } = "360px";
    public bool Resizable { get; set; }
    public bool ShowClose { get; set; } = true;
    public bool ShowHeader { get; set; } = true;
    public bool NoPadding { get; set; }
    public bool CloseOnBackdrop { get; set; } = true;
    public bool CloseOnEscape { get; set; } = true;
    public SgDrawerAnimation Animation { get; set; } = SgDrawerAnimation.Slide;
    public string? BackdropBlur { get; set; }
    public bool FullScreen { get; set; }
    public RenderFragment? FooterContent { get; set; }
    public RenderFragment? HeaderContent { get; set; }
    public bool Loading { get; set; }
    public Func<Task<bool>>? CloseConfirm { get; set; }
    public EventCallback OnOpened { get; set; }
    public EventCallback OnClosed { get; set; }
}
