namespace SuperUI.Enums;

/// <summary>
/// Тип режима рендеринга Blazor-компонента.
/// Используется RenderModeProvider для адаптивного поведения.
/// </summary>
public enum RenderModeType
{
    /// <summary>Статический SSR без потоковой передачи.</summary>
    StaticSSR,
    /// <summary>Статический SSR с Streaming Rendering.</summary>
    StaticSSRStreaming,
    /// <summary>Интерактивный Server-Side Rendering (SignalR).</summary>
    InteractiveServer,
    /// <summary>Интерактивный WebAssembly.</summary>
    InteractiveWebAssembly,
    /// <summary>Auto-режим (Server при первой загрузке, затем WASM).</summary>
    InteractiveAuto
}
