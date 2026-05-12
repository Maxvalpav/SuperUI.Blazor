using System.Collections.Concurrent;
using Microsoft.JSInterop;
using SuperUI.Base.Interop;

namespace SuperUI.Base.Services;

/// <summary>
/// Реализация сервиса управления ловушкой фокуса.
/// Поддерживает стек вложенных оверлеев.
///
/// ИСПРАВЛЕНО:
/// 1. Prerendering guard — пропускает JS вызовы при SSR.
/// 2. _trapStack — ConcurrentStack для thread-safety на Server.
/// 3. Push выполняется ПОСЛЕ успешного JS вызова (атомарность стека).
/// 4. module null-check.
/// </summary>
public sealed class FocusTrapService : IFocusTrapService, IAsyncDisposable
{
    private readonly SgJsInterop _jsInterop;
    private readonly IPrerenderingDetector _prerenderingDetector;

    // ИСПРАВЛЕНО: ConcurrentStack для thread-safety на Server
    private readonly ConcurrentStack<string> _trapStack = new();

    public FocusTrapService(SgJsInterop jsInterop, IPrerenderingDetector prerenderingDetector)
    {
        _jsInterop = jsInterop ?? throw new ArgumentNullException(nameof(jsInterop));
        _prerenderingDetector = prerenderingDetector ?? throw new ArgumentNullException(nameof(prerenderingDetector));
    }

    public async Task ActivateAsync(string containerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

        // ИСПРАВЛЕНО: guard — не вызываем JS при prerendering
        if (_prerenderingDetector.IsPrerendering) return;

        var module = await _jsInterop.GetModuleAsync("_content/SuperUI/focustrap.js");

        // ИСПРАВЛЕНО: null-check вместо null-forgiving !
        if (module is null) return;

        await module.InvokeVoidAsync("activate", containerId);

        // ИСПРАВЛЕНО: Push ПОСЛЕ успешного JS (стек согласован при ошибке)
        _trapStack.Push(containerId);
    }

    public async Task DeactivateAsync(string containerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(containerId);

        if (_prerenderingDetector.IsPrerendering) return;

        // Снимаем с вершины если это наш ID
        if (_trapStack.TryPeek(out var top) && top == containerId)
            _trapStack.TryPop(out _);

        var module = await _jsInterop.GetModuleAsync("_content/SuperUI/focustrap.js");
        if (module is null) return;

        await module.InvokeVoidAsync("deactivate", containerId);

        // Восстанавливаем предыдущий trap если был стек
        if (_trapStack.TryPeek(out var previous))
            await module.InvokeVoidAsync("activate", previous);
    }

    public async Task MoveFocusAsync(string containerId, FocusDirection direction)
    {
        if (_prerenderingDetector.IsPrerendering) return;

        var module = await _jsInterop.GetModuleAsync("_content/SuperUI/focustrap.js");
        if (module is null) return;

        await module.InvokeVoidAsync("moveFocus", containerId, direction.ToString().ToLowerInvariant());
    }

    public ValueTask DisposeAsync()
    {
        _trapStack.Clear();
        return ValueTask.CompletedTask;
    }
}
