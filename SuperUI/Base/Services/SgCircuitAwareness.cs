// SuperUI/Base/Services/SgCircuitAwareness.cs
//
// Абстракция состояния Circuit для Server-side Blazor.
// Не требует прямой ссылки на Microsoft.AspNetCore.Components.Server —
// Server-реализация использует условную компиляцию.

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис для определения состояния Circuit (Server-side Blazor).
/// На WASM и Static SSR всегда возвращает connected=true.
/// </summary>
public interface ICircuitAwareness
{
    /// <summary>Circuit активен. Всегда true на WASM и Static SSR.</summary>
    bool IsConnected { get; }

    /// <summary>ID Circuit (для диагностики). null на WASM/SSR.</summary>
    string? CircuitId { get; }

    /// <summary>Событие: Circuit отключён (только InteractiveServer).</summary>
    event Action? OnCircuitDisconnected;
}

/// <summary>
/// WASM-реализация — всегда connected. Используется на WASM и Static SSR.
/// </summary>
public sealed class WasmCircuitAwareness : ICircuitAwareness
{
    public bool IsConnected => true;
    public string? CircuitId => null;
    public event Action? OnCircuitDisconnected { add { } remove { } }
}