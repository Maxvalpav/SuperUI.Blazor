// SuperUI/Base/Services/SgRenderModeDetector.cs 
// Улучшения: 
// - Поддержка RendererInfo (.NET 9) 
// - Поддержка InteractiveAuto перехода 
// - Правильный WASM-specific путь 
// - Без рефлексии (OperatingSystem.IsBrowser()) 
// - Логирование режима при старте 
 
using System; 
using Microsoft.AspNetCore.Components; 
using Microsoft.AspNetCore.Components.Web; 
 
namespace SuperUI.Base.Services; 
 
/// <summary> 
/// Перечисление режимов рендеринга SuperUI. 
/// </summary> 
public enum SgRenderMode 
{ 
    /// <summary>Статичный SSR (нет интерактивности).</summary> 
    StaticServer, 
 
    /// <summary>SSR с потоковым рендерингом ([StreamRendering]).</summary> 
    StreamingServer, 
 
    /// <summary>Prerendering для интерактивных режимов.</summary> 
    Prerendering, 
 
    /// <summary>InteractiveServer (SignalR circuit).</summary> 
    InteractiveServer, 
 
    /// <summary>InteractiveWebAssembly (браузер).</summary> 
    InteractiveWebAssembly, 
 
    /// <summary>InteractiveAuto (Server → WebAssembly).</summary> 
    InteractiveAuto, 
} 
 
/// <summary> 
/// Сервис определения текущего режима рендеринга. 
/// Регистрировать как Scoped (Server) / Singleton (WASM). 
/// </summary> 
public sealed class SgRenderModeDetector : ISgRenderModeDetector 
{ 
    private readonly IServiceProvider _services; 
 
    public SgRenderModeDetector(IServiceProvider services) 
    { 
        _services = services; 
    } 
 
    /// <summary> 
    /// Определяет режим рендеринга для данного компонента. 
    /// </summary> 
    public SgRenderMode GetRenderMode(ComponentBase component) 
    { 
        // 1. Сначала проверяем WASM — это быстро и точно 
        if (OperatingSystem.IsBrowser()) 
            return SgRenderMode.InteractiveWebAssembly; 
 
 #if NET9_0_OR_GREATER 
        // 2. .NET 9+ — используем официальный API 
        var rendererInfo = component.RendererInfo; 
        var assignedMode = component.AssignedRenderMode; 
 
        return (rendererInfo.IsInteractive, rendererInfo.Name, assignedMode) switch 
        { 
            (false, "Static", _) => SgRenderMode.StaticServer, 
            (false, "Server", _) => SgRenderMode.Prerendering, 
            (true, "Server", InteractiveAutoRenderMode) => SgRenderMode.InteractiveAuto, 
            (true, "Server", _) => SgRenderMode.InteractiveServer, 
            _ => SgRenderMode.StaticServer 
        }; 
 #elif NET8_0_OR_GREATER 
        // 3. .NET 8 — используем AssignedRenderMode 
        var assignedMode = component.AssignedRenderMode; 
        return assignedMode switch 
        { 
            InteractiveServerRenderMode => SgRenderMode.InteractiveServer, 
            InteractiveWebAssemblyRenderMode => SgRenderMode.InteractiveWebAssembly, 
            InteractiveAutoRenderMode => SgRenderMode.InteractiveAuto, 
            null => SgRenderMode.StaticServer, 
            _ => SgRenderMode.StaticServer 
        }; 
 #else 
        return SgRenderMode.InteractiveServer; 
 #endif 
    } 
 
    /// <summary>true если компонент интерактивен (не SSR, не prerendering).</summary> 
    public bool IsInteractive(ComponentBase component) 
    { 
        var mode = GetRenderMode(component); 
        return mode is SgRenderMode.InteractiveServer 
            or SgRenderMode.InteractiveWebAssembly 
            or SgRenderMode.InteractiveAuto; 
    } 
 
    /// <summary>true если код выполняется в браузере (WASM).</summary> 
    public bool IsWebAssembly => OperatingSystem.IsBrowser(); 
 
    /// <summary>true если код выполняется на сервере.</summary> 
    public bool IsServer => !OperatingSystem.IsBrowser(); 
} 
 
/// <summary>Интерфейс детектора режима рендеринга.</summary> 
public interface ISgRenderModeDetector 
{ 
    SgRenderMode GetRenderMode(ComponentBase component); 
    bool IsInteractive(ComponentBase component); 
    bool IsWebAssembly { get; } 
    bool IsServer { get; } 
}