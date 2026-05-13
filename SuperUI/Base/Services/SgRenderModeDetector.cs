// SuperUI/Base/Services/SgRenderModeDetector.cs
// НОВЫЙ: определение режима рендеринга (.NET 8+)
//
// Позволяет компонентам определить, в каком режиме они рендерятся:
//   - Static SSR (без интерактивности)
//   - InteractiveServer (SignalR)
//   - InteractiveWebAssembly (WASM)
//   - InteractiveAuto

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace SuperUI.Base.Services;

/// <summary>
/// Определяет текущий режим рендеринга компонента (.NET 8+).
/// Работает в SSR, InteractiveServer, InteractiveWebAssembly, InteractiveAuto.
/// </summary>
public interface IRenderModeDetector
{
    /// <summary>Компонент находится в фазе статического SSR (нет SignalR/интерактивности).</summary>
    bool IsStaticSSR { get; }

    /// <summary>Компонент рендерится интерактивно через SignalR (InteractiveServer).</summary>
    bool IsInteractiveServer { get; }

    /// <summary>Компонент рендерится в WebAssembly (InteractiveWebAssembly).</summary>
    bool IsInteractiveWebAssembly { get; }

    /// <summary>Интерактивность доступна (любой режим кроме Static SSR).</summary>
    bool IsInteractive { get; }

    /// <summary>
    /// Текущий RenderMode компонента.
    /// Для .NET 7- — всегда null.
    /// </summary>
    IComponentRenderMode? CurrentRenderMode { get; }

    /// <summary>Запущено ли приложение в режиме InteractiveAuto.</summary>
    bool IsInteractiveAuto { get; }
}

public sealed class SgRenderModeDetector : IRenderModeDetector
{
    // RenderMode присваивается через CascadingParameter или вручную
    public IComponentRenderMode? CurrentRenderMode { get; set; }

    public bool IsStaticSSR => CurrentRenderMode is null;

    public bool IsInteractiveServer =>
        CurrentRenderMode is InteractiveServerRenderMode;

    public bool IsInteractiveWebAssembly =>
        CurrentRenderMode is InteractiveWebAssemblyRenderMode;

    public bool IsInteractiveAuto =>
        CurrentRenderMode is InteractiveAutoRenderMode;

    public bool IsInteractive => !IsStaticSSR;
}