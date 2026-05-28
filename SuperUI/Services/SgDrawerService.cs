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
    private readonly List<DrawerInstance> _drawers = new();

    public IReadOnlyList<DrawerInstance> Drawers => _drawers.AsReadOnly();

    public event Action? StateChanged;

    /// <summary>
    /// Opens a drawer with the specified configuration.
    /// Returns a <see cref="DrawerInstance"/> that can be used to close the drawer programmatically.
    /// </summary>
    public DrawerInstance Show(string title, RenderFragment body, Action<DrawerConfig>? configure = null)
    {
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

        _drawers.Add(instance);
        StateChanged?.Invoke();
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
        instance.Visible = false;
        _drawers.Remove(instance);
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Closes all open drawers.
    /// </summary>
    public void CloseAll()
    {
        foreach (var d in _drawers.ToList())
            d.Visible = false;
        _drawers.Clear();
        StateChanged?.Invoke();
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
