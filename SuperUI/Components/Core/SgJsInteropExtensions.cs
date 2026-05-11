using Microsoft.JSInterop;

namespace SuperUI.Core;

/// <summary>
/// Extension methods over <see cref="IJSRuntime"/> and <see cref="IJSObjectReference"/>
/// that swallow expected lifetime exceptions (<see cref="JSDisconnectedException"/>,
/// <see cref="OperationCanceledException"/>) so they do not surface as unobserved
/// task exceptions in services and components that outlive a single render.
/// </summary>
public static class SgJsInteropExtensions
{
    /// <summary>
    /// Invokes a JS function and returns <c>false</c> if the call could not complete
    /// because the circuit was torn down or the operation was cancelled.
    /// </summary>
    public static async ValueTask<bool> TryInvokeVoidAsync(
        this IJSRuntime js,
        string identifier,
        CancellationToken ct = default,
        params object?[] args)
    {
        if (js is null) return false;
        try
        {
            await js.InvokeVoidAsync(identifier, ct, args).ConfigureAwait(false);
            return true;
        }
        catch (JSDisconnectedException) { return false; }
        catch (OperationCanceledException) { return false; }
        catch (ObjectDisposedException) { return false; }
    }

    /// <inheritdoc cref="TryInvokeVoidAsync(IJSRuntime, string, CancellationToken, object?[])"/>
    public static async ValueTask<TResult?> TryInvokeAsync<TResult>(
        this IJSRuntime js,
        string identifier,
        CancellationToken ct = default,
        params object?[] args)
    {
        if (js is null) return default;
        try
        {
            return await js.InvokeAsync<TResult>(identifier, ct, args).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { return default; }
        catch (OperationCanceledException) { return default; }
        catch (ObjectDisposedException) { return default; }
    }

    /// <inheritdoc cref="TryInvokeVoidAsync(IJSRuntime, string, CancellationToken, object?[])"/>
    public static async ValueTask<bool> TryInvokeVoidAsync(
        this IJSObjectReference? module,
        string identifier,
        CancellationToken ct = default,
        params object?[] args)
    {
        if (module is null) return false;
        try
        {
            await module.InvokeVoidAsync(identifier, ct, args).ConfigureAwait(false);
            return true;
        }
        catch (JSDisconnectedException) { return false; }
        catch (OperationCanceledException) { return false; }
        catch (ObjectDisposedException) { return false; }
    }

    /// <inheritdoc cref="TryInvokeVoidAsync(IJSRuntime, string, CancellationToken, object?[])"/>
    public static async ValueTask<TResult?> TryInvokeAsync<TResult>(
        this IJSObjectReference? module,
        string identifier,
        CancellationToken ct = default,
        params object?[] args)
    {
        if (module is null) return default;
        try
        {
            return await module.InvokeAsync<TResult>(identifier, ct, args).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { return default; }
        catch (OperationCanceledException) { return default; }
        catch (ObjectDisposedException) { return default; }
    }

    /// <summary>
    /// Disposes a JS module reference, swallowing the disconnect that fires when
    /// the user closed the tab or the circuit died mid-disposal.
    /// </summary>
    public static async ValueTask SafeDisposeAsync(this IJSObjectReference? module)
    {
        if (module is null) return;
        try
        {
            await module.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    /// <summary>
    /// Imports a JS module from the canonical SuperUI co-located path
    /// <c>./_content/SuperUI/{folder}/{componentName}.razor.js</c>.
    /// Returns <c>null</c> during pre-render or when the circuit is disconnected.
    /// </summary>
    public static async ValueTask<IJSObjectReference?> ImportSuperUiModuleAsync(
        this IJSRuntime js,
        string componentName,
        string folder = "Components",
        CancellationToken ct = default)
    {
        var path = string.IsNullOrEmpty(folder)
            ? $"./_content/SuperUI/{componentName}.razor.js"
            : $"./_content/SuperUI/{folder}/{componentName}.razor.js";
        return await js.TryInvokeAsync<IJSObjectReference>("import", ct, path).ConfigureAwait(false);
    }
}
