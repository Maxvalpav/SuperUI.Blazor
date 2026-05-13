// SuperUI/Base/SgCircuitAwareComponent.cs
//
// Circuit-aware component для Server-side Blazor.
// Использует ICircuitAwareness для определения состояния Circuit без прямой
// зависимости от Microsoft.AspNetCore.Components.Server (Server-only package).
//
// Архитектура:
//   SgCircuitAwareComponent ← SgComponentBase
//       ↓ использует
//   ICircuitAwareness (service abstraction)
//       ├── ServerCircuitAwareness — подписывается на CircuitHandler (Server)
//       └── WasmCircuitAwareness — always-connected (WASM)

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Services;

namespace SuperUI.Base;

/// <summary>
/// Расширение SgComponentBase с учётом Circuit lifetime (Server-side Blazor).
/// Автоматически отменяет pending-операции при разрыве Circuit.
/// </summary>
/// <remarks>
/// На WASM и Static SSR ведёт себя как обычный SgComponentBase — 
/// <see cref="IsCircuitConnected"/> всегда true вне InteractiveServer.
/// </remarks>
public abstract class SgCircuitAwareComponent : SgComponentBase
{
    [Inject] private ICircuitAwareness CircuitAwareness { get; set; } = null!;

    /// <summary>Circuit активен (только для InteractiveServer).</summary>
    protected bool IsCircuitConnected => CircuitAwareness.IsConnected;

    /// <summary>ID Circuit (для диагностики). null на WASM/SSR.</summary>
    protected string? CircuitId => CircuitAwareness.CircuitId;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (IsInteractiveServer)
        {
            CircuitAwareness.OnCircuitDisconnected += OnCircuitDisconnectedHandler;
        }
    }

    private void OnCircuitDisconnectedHandler()
    {
        Logger.LogWarning("[{Id}] Circuit disconnected, cancelling pending operations", ComponentId);

        OnCircuitDisconnected();
    }

    /// <summary>
    /// Вызывается при разрыве Circuit.
    /// Переопределите для кастомной логики (например, сохранение состояния,
    /// отмена long-running операций).
    /// </summary>
    protected virtual void OnCircuitDisconnected() { }

    protected override async ValueTask DisposeComponentAsync()
    {
        if (IsInteractiveServer)
        {
            CircuitAwareness.OnCircuitDisconnected -= OnCircuitDisconnectedHandler;
        }

        await base.DisposeComponentAsync();
    }
}