using Microsoft.Extensions.Options;

namespace SuperUI.Base.Services;

/// <summary>
/// Централизованные настройки компонентов.
/// Позволяет задать defaults для всей библиотеки через DI.
/// </summary>
public interface IComponentOptionsService
{
    SgButtonOptions Button { get; }
    SgInputOptions Input { get; }
    SgDataGridOptions DataGrid { get; }
    SgOverlayOptions Overlay { get; }
    SgToastOptions Toast { get; }
}

public sealed record SgButtonOptions
{
    public Base.SgVariant DefaultVariant { get; init; } = Base.SgVariant.Primary;
    public Base.SgSize DefaultSize { get; init; } = Base.SgSize.Medium;
    public bool ShowRipple { get; init; } = true;
}

public sealed record SgInputOptions
{
    public int DefaultDebounceMs { get; init; } = 300;
    public bool ShowClearButton { get; init; } = true;
    public Base.SgInputVariant DefaultVariant { get; init; } = Base.SgInputVariant.Outlined;
}

public sealed record SgDataGridOptions
{
    public int DefaultPageSize { get; init; } = 25;
    public bool DefaultVirtualization { get; init; } = true;
    public bool DefaultShowSearch { get; init; } = false;
}

public sealed record SgOverlayOptions
{
    public int DefaultAnimationMs { get; init; } = 300;
    public bool DefaultCloseOnEscape { get; init; } = true;
    public bool DefaultTrapFocus { get; init; } = true;
}

public sealed record SgToastOptions
{
    public int DefaultDurationMs { get; init; } = 4000;
    public Base.SgPlacement DefaultPlacement { get; init; } = Base.SgPlacement.TopRight;
}

/// <summary>
/// Главный класс настроек библиотеки.
/// </summary>
public sealed record SgLibraryOptions
{
    public SgButtonOptions? Button { get; init; }
    public SgInputOptions? Input { get; init; }
    public SgDataGridOptions? DataGrid { get; init; }
    public SgOverlayOptions? Overlay { get; init; }
    public SgToastOptions? Toast { get; init; }
}

/// <summary>
/// Реализация сервиса настроек компонентов.
/// </summary>
public sealed class ComponentOptionsService : IComponentOptionsService
{
    public ComponentOptionsService(IOptions<SgLibraryOptions> options)
    {
        var o = options.Value;
        Button = o.Button ?? new();
        Input = o.Input ?? new();
        DataGrid = o.DataGrid ?? new();
        Overlay = o.Overlay ?? new();
        Toast = o.Toast ?? new();
    }

    public SgButtonOptions Button { get; }
    public SgInputOptions Input { get; }
    public SgDataGridOptions DataGrid { get; }
    public SgOverlayOptions Overlay { get; }
    public SgToastOptions Toast { get; }
}