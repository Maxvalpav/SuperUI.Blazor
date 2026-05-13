// SuperUI/Base/Services/SgRenderModeDetector.cs
// ИСПРАВЛЕНИЯ:
// ✅ FIX CS0246: добавлен using Microsoft.AspNetCore.Components.Web
// ✅ FIX ARCH: CurrentRenderMode через internal set (только для DI/CascadingParameter)
// ✅ NEW: IsPrerendering через OperatingSystem + IComponentRenderMode

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;   // ← FIX CS0246

namespace SuperUI.Base.Services;

/// <summary>
/// Определяет текущий режим рендеринга компонента (.NET 8+).
/// </summary>
public interface IRenderModeDetector
{
    bool IsStaticSSR { get; }
    bool IsInteractiveServer { get; }
    bool IsInteractiveWebAssembly { get; }
    bool IsInteractive { get; }
    bool IsInteractiveAuto { get; }
    IComponentRenderMode? CurrentRenderMode { get; }
}

public sealed class SgRenderModeDetector : IRenderModeDetector
{
    // FIX ARCH: internal set — устанавливается только из компонента через CascadingParameter
    public IComponentRenderMode? CurrentRenderMode { get; internal set; }

    public bool IsStaticSSR        => CurrentRenderMode is null;
    public bool IsInteractiveServer    => CurrentRenderMode is InteractiveServerRenderMode;
    public bool IsInteractiveWebAssembly => CurrentRenderMode is InteractiveWebAssemblyRenderMode;
    public bool IsInteractiveAuto  => CurrentRenderMode is InteractiveAutoRenderMode;
    public bool IsInteractive      => CurrentRenderMode is not null;

    /// <summary>
    /// NEW: Определяет фазу prerendering (компонент рендерится на сервере перед гидрацией).
    /// На WASM prerendering — когда ещё нет интерактивности.
    /// </summary>
    public bool IsPrerendering
        => IsInteractiveServer && !OperatingSystem.IsBrowser()
        || IsInteractiveAuto   && !OperatingSystem.IsBrowser();
}
