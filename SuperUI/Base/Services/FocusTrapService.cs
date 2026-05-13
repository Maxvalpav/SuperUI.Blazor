// SuperUI/Base/Services/FocusTrapService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SuperUI.Base.Services;

/// <summary>
/// Manages focus trapping for modals, dialogs, dropdowns, etc.
/// Implements a stack-based approach for nested focus traps.
/// Supports both WASM (JS interop) and Server-side rendering.
/// </summary>
public class FocusTrapService : IFocusTrapService, IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly FocusTrapStack _stack;
    private bool _initialized;
    private IJSObjectReference? _module;

    public FocusTrapService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        _stack = new FocusTrapStack();
    }

    public bool IsTrappingActive => _stack.Count > 0;

    /// <summary>Initialize the JS module (lazy, called once).</summary>
    private async ValueTask EnsureInitializedAsync()
    {
        if (!_initialized)
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/js/focusTrap.js");
            _initialized = true;
        }
    }

    /// <summary>Activate focus trap for a specific element.</summary>
    public async ValueTask TrapFocusAsync(ElementReference element, FocusTrapOptions? options = null)
    {
        await EnsureInitializedAsync();
        _stack.Push(element, options ?? new FocusTrapOptions());
        await _module!.InvokeVoidAsync("activate", element, options);
    }

    /// <summary>Deactivate the current focus trap (LIFO).</summary>
    public async ValueTask ReleaseFocusAsync()
    {
        if (!_stack.TryPop(out var entry))
            return;

        await EnsureInitializedAsync();
        await _module!.InvokeVoidAsync("deactivate", entry.Element);

        // Restore focus to previous trap if any
        if (_stack.TryPeek(out var previous))
        {
            await _module!.InvokeVoidAsync("activate", previous.Element, previous.Options);
        }
    }

    /// <summary>Release all focus traps.</summary>
    public async ValueTask ReleaseAllAsync()
    {
        await EnsureInitializedAsync();
        while (_stack.TryPop(out var entry))
        {
            await _module!.InvokeVoidAsync("deactivate", entry.Element);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            try { await _module.DisposeAsync(); } catch { /* JS runtime may be disposed */ }
        }
        GC.SuppressFinalize(this);
    }
}

/// <summary>Stack entry for nested focus trapping.</summary>
public readonly struct FocusTrapEntry
{
    public ElementReference Element { get; }
    public FocusTrapOptions Options { get; }

    public FocusTrapEntry(ElementReference element, FocusTrapOptions options)
    {
        Element = element;
        Options = options;
    }
}

/// <summary>Options for focus trap behavior.</summary>
public sealed class FocusTrapOptions
{
    /// <summary>Return focus to trigger element on deactivate?</summary>
    public bool ReturnFocusOnDeactivate { get; set; } = true;

    /// <summary>Allow escape key to deactivate?</summary>
    public bool EscapeDeactivates { get; set; } = true;

    /// <summary>Allow click outside to deactivate?</summary>
    public bool ClickOutsideDeactivates { get; set; } = true;

    /// <summary>Initial focus selector (CSS selector).</summary>
    public string? InitialFocus { get; set; }

    /// <summary>Whether to prevent scroll on focus.</summary>
    public bool PreventScroll { get; set; }

    /// <summary>In SSR mode, focus trapping may be deferred.</summary>
    public bool DeferInSsr { get; set; }
}
