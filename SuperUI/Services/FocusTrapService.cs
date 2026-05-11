// ─────────────────────────────────────────────────────────────────
// FILE: Services/FocusTrapService.cs
// Описание: Сервис управления FocusTrap для оверлеев.
// ─────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SuperUI.Services;

/// <summary>
/// Интерфейс сервиса управления ловушкой фокуса.
/// </summary>
public interface IFocusTrapService
{
    Task ActivateAsync(ElementReference container, string id);
    Task DeactivateAsync(string id);
}

/// <summary>
/// Singleton-сервис управления ловушкой фокуса.
/// Стек — поддерживает вложенные оверлеи (Modal внутри Modal).
/// </summary>
public sealed class FocusTrapService : IFocusTrapService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly Stack<string> _trapStack = new();
    private IJSObjectReference? _module;

    public FocusTrapService(IJSRuntime jsRuntime) => _jsRuntime = jsRuntime;

    private async ValueTask<IJSObjectReference?> GetModuleAsync()
    {
        if (_module is not null) return _module;
        try
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "_content/SuperUI/focus-trap.js");
            return _module;
        }
        catch { return null; }
    }

    public async Task ActivateAsync(ElementReference container, string id)
    {
        var mod = await GetModuleAsync();
        if (mod is null) return;
        await mod.InvokeVoidAsync("activate", container, id);
        _trapStack.Push(id);
    }

    public async Task DeactivateAsync(string id)
    {
        var mod = await GetModuleAsync();
        if (mod is null) return;
        await mod.InvokeVoidAsync("deactivate", id);
        if (_trapStack.TryPeek(out var top) && top == id)
            _trapStack.Pop();
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); }
            catch { }
        }
    }
}
