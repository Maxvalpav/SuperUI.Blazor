// SuperUI/Base/SgCircuitAwareComponent.cs 
// Улучшения: 
// - Обработка перехода InteractiveAuto (Server → WebAssembly) 
// - Корректная очистка Circuit-специфичных ресурсов при переходе 
// - События OnCircuitConnected/Disconnected 
// - Reconnection handling (важно для Server-side) 
 
using System; 
using System.Threading.Tasks; 
using Microsoft.AspNetCore.Components; 
using SuperUI.Base.Services; 
 
namespace SuperUI.Base; 
 
/// <summary> 
/// Базовый класс для компонентов, которым важно знать о состоянии SignalR circuit. 
/// Поддерживает переход InteractiveAuto (Server → WebAssembly). 
/// </summary> 
public abstract class SgCircuitAwareComponent : SgComponentBase 
{ 
    private bool _circuitConnected; 
    private SgRenderMode _previousRenderMode = SgRenderMode.StaticSSR; 
 
    [Inject] private ISgRenderModeDetector RenderModeDetector { get; set; } = default!; 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Состояние circuit 
    // ────────────────────────────────────────────────────────────────────── 
 
    /// <summary>SignalR circuit активен.</summary> 
    protected bool IsCircuitConnected => _circuitConnected; 
 
    /// <summary>Текущий режим рендеринга.</summary>
    protected SgRenderMode CurrentMode => RenderModeDetector.CurrentMode;
 
    // ────────────────────────────────────────────────────────────────────── 
    // Жизненный цикл 
    // ────────────────────────────────────────────────────────────────────── 
 
    protected override async Task OnAfterRenderAsync(bool firstRender) 
    { 
        await base.OnAfterRenderAsync(firstRender); 
 
        var currentMode = RenderModeDetector.CurrentMode;
 
        if (firstRender) 
        { 
            _previousRenderMode = currentMode; 
 
            if (currentMode == SgRenderMode.InteractiveServer || 
                currentMode == SgRenderMode.InteractiveAuto) 
            { 
                _circuitConnected = true; 
                await OnCircuitConnectedAsync(); 
            } 
            else if (currentMode == SgRenderMode.InteractiveWebAssembly) 
            { 
                // WASM — нет circuit, но компонент активен 
                await OnWasmActivatedAsync(); 
            } 
        } 
        else 
        { 
            // Обнаруживаем переход InteractiveAuto: Server → WebAssembly 
            if (_previousRenderMode == SgRenderMode.InteractiveServer && 
                currentMode == SgRenderMode.InteractiveWebAssembly) 
            { 
                _circuitConnected = false; 
                await OnCircuitDisconnectedAsync(); 
                await OnAutoModeTransitionAsync(from: SgRenderMode.InteractiveServer, 
                                                to: SgRenderMode.InteractiveWebAssembly); 
                await OnWasmActivatedAsync(); 
            } 
        } 
 
        _previousRenderMode = currentMode; 
    } 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Переопределяемые события 
    // ────────────────────────────────────────────────────────────────────── 
 
    /// <summary>Вызывается когда SignalR circuit установлен.</summary> 
    protected virtual Task OnCircuitConnectedAsync() => Task.CompletedTask; 
 
    /// <summary> 
    /// Вызывается когда SignalR circuit разорван. 
    /// Освобождайте Server-specific ресурсы здесь. 
    /// </summary> 
    protected virtual Task OnCircuitDisconnectedAsync() => Task.CompletedTask; 
 
    /// <summary> 
    /// Вызывается при переходе InteractiveAuto: Server → WebAssembly. 
    /// </summary> 
    protected virtual Task OnAutoModeTransitionAsync(SgRenderMode from, SgRenderMode to) 
        => Task.CompletedTask; 
 
    /// <summary> 
    /// Вызывается когда компонент работает в WebAssembly (в том числе после перехода Auto). 
    /// </summary> 
    protected virtual Task OnWasmActivatedAsync() => Task.CompletedTask; 
}