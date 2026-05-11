
using Microsoft.JSInterop;
using SuperUI.Base.Interop;

namespace SuperUI.Base.Services;

/// <summary>
/// Реализация сервиса управления ловушкой фокуса.
/// Поддерживает стек вложенных оверлеев.
/// </summary>
public sealed class FocusTrapService : IFocusTrapService, IAsyncDisposable
{
    private readonly SgJsInterop _jsInterop;
    private readonly Stack<string> _trapStack = new(); // поддержка вложенных оверлеев

    public FocusTrapService(SgJsInterop jsInterop) => _jsInterop = jsInterop;

    public async Task ActivateAsync(string containerId)
    {
        _trapStack.Push(containerId);
        var module = await _jsInterop.GetModuleAsync("_content/SuperUI/focustrap.js");
        await module!.InvokeVoidAsync("activate", containerId);
    }

    public async Task DeactivateAsync(string containerId)
    {
        if (_trapStack.Count > 0 && _trapStack.Peek() == containerId)
            _trapStack.Pop();

        var module = await _jsInterop.GetModuleAsync("_content/SuperUI/focustrap.js");
        await module!.InvokeVoidAsync("deactivate", containerId);

        // Восстановить предыдущий trap если был стек
        if (_trapStack.Count > 0)
            await module.InvokeVoidAsync("activate", _trapStack.Peek());
    }

    public async Task MoveFocusAsync(string containerId, FocusDirection direction)
    {
        var module = await _jsInterop.GetModuleAsync("_content/SuperUI/focustrap.js");
        await module!.InvokeVoidAsync("moveFocus", containerId, direction.ToString().ToLower());
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
