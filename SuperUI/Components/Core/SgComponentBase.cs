using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SuperUI.Components;

namespace SuperUI.Core;

/// <summary>
/// Base class for all SuperUI components. Provides a uniform visual contract
/// (Id / Class / Style / Disabled / Size / Visible / Attributes), lifecycle helpers,
/// safe rendering after disposal, JS module loading with co-located convention,
/// <see cref="DotNetObjectReference{T}"/> lifetime management, and synchronous + asynchronous
/// disposal that mirrors patterns used by MudBlazor / Radzen / AntDesign Blazor / FluentUI.
/// </summary>
public abstract class SgComponentBase : ComponentBase, IDisposable, IAsyncDisposable
{
    private static long _idSeed;

    private bool _disposed;
    private CancellationTokenSource? _cts;

    /// <summary>JS runtime injected by the framework. Available to derived components.</summary>
    [Inject] protected IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Stable DOM id for the root element. Generated lazily on first access if not supplied
    /// by the consumer, so derived components can always bind <c>id="@Id"</c>.
    /// </summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Additional CSS class names appended after the component's own classes.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Inline style appended after the component's own style.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// When false the component renders nothing. Mirrors the <c>v-show</c>/<c>Visible</c>
    /// convention from other Blazor libraries to avoid wrapping in <c>@if</c> blocks.
    /// </summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>
    /// Disabled state propagated to derived components. Base class does not render it —
    /// it is exposed here so every component speaks the same vocabulary.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Logical size of the component. Defaults to <see cref="SgSize.Md"/>.</summary>
    [Parameter] public SgSize Size { get; set; } = SgSize.Md;

    /// <summary>Catch-all for unmatched HTML attributes forwarded to the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>True once <see cref="IDisposable.Dispose"/> / <see cref="DisposeAsync"/> has run.</summary>
    protected bool IsDisposed => _disposed;

    /// <summary>
    /// Token cancelled when the component is disposed. Pass it to async work so background
    /// continuations do not race with disposal.
    /// </summary>
    protected CancellationToken ComponentCt
    {
        get
        {
            _cts ??= new CancellationTokenSource();
            return _cts.Token;
        }
    }

    /// <summary>Lazy id used as a fallback when <see cref="Id"/> is not provided.</summary>
    private string? _autoId;

    /// <summary>Element id to bind in markup. Stable across re-renders.</summary>
    protected string ElementId => Id ?? (_autoId ??= GenerateId(GetType().Name));

    /// <summary>
    /// Combines the component-defined CSS classes with the user-supplied <see cref="Class"/>.
    /// Skips empty / whitespace fragments.
    /// </summary>
    protected static string BuildClass(params string?[] fragments)
    {
        if (fragments is null || fragments.Length == 0) return string.Empty;
        return string.Join(' ', fragments.Where(f => !string.IsNullOrWhiteSpace(f)));
    }

    /// <summary>Combines component-defined style with the user-supplied <see cref="Style"/>.</summary>
    protected static string BuildStyle(params string?[] fragments)
    {
        if (fragments is null || fragments.Length == 0) return string.Empty;
        var parts = fragments
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => f!.TrimEnd(';'));
        return string.Join("; ", parts);
    }

    /// <summary>Generates a DOM-safe unique id with the given prefix.</summary>
    protected static string GenerateId(string prefix)
        => $"{prefix.ToLowerInvariant()}-{Interlocked.Increment(ref _idSeed):x}";

    /// <summary>
    /// Safely requests a re-render. Becomes a no-op after disposal and always marshals
    /// to the renderer's sync context. Use this instead of calling <see cref="StateHasChanged"/>
    /// from background threads, JS callbacks, or timers.
    /// </summary>
    protected Task SafeStateHasChangedAsync()
    {
        if (_disposed) return Task.CompletedTask;
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Imports the JS module co-located with a component using the convention
    /// <c>./_content/SuperUI/Components/{ComponentName}.razor.js</c>. Returns <c>null</c>
    /// during pre-render (when <see cref="IJSRuntime"/> is the un-initialized
    /// <c>UnsupportedJavaScriptRuntime</c>).
    /// </summary>
    protected async Task<IJSObjectReference?> ImportModuleAsync(string componentName, string? folder = "Components")
    {
        if (_disposed) return null;
        var path = string.IsNullOrEmpty(folder)
            ? $"./_content/SuperUI/{componentName}.razor.js"
            : $"./_content/SuperUI/{folder}/{componentName}.razor.js";
        try
        {
            return await JS.InvokeAsync<IJSObjectReference>("import", ComponentCt, path);
        }
        catch (OperationCanceledException) { return null; }
        catch (JSDisconnectedException) { return null; }
    }

    /// <summary>
    /// Creates a <see cref="DotNetObjectReference{T}"/> bound to the given target.
    /// Caller owns the reference and must dispose it (typically in <see cref="DisposeAsyncCore"/>).
    /// </summary>
    protected static DotNetObjectReference<T> CreateDotNetRef<T>(T target) where T : class
        => DotNetObjectReference.Create(target);

    /// <summary>
    /// Invokes a JS function defensively: swallows disconnects (Server-side Blazor circuit drop,
    /// component disposed mid-call) so they do not surface as unobserved exceptions.
    /// </summary>
    protected async ValueTask SafeInvokeVoidAsync(IJSObjectReference? module, string identifier, params object?[] args)
    {
        if (_disposed || module is null) return;
        try
        {
            await module.InvokeVoidAsync(identifier, ComponentCt, args);
        }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
    }

    /// <inheritdoc cref="SafeInvokeVoidAsync(IJSObjectReference?, string, object?[])"/>
    protected async ValueTask<TResult?> SafeInvokeAsync<TResult>(IJSObjectReference? module, string identifier, params object?[] args)
    {
        if (_disposed || module is null) return default;
        try
        {
            return await module.InvokeAsync<TResult>(identifier, ComponentCt, args);
        }
        catch (OperationCanceledException) { return default; }
        catch (JSDisconnectedException) { return default; }
    }

    /// <summary>
    /// Override to release managed resources (event handlers, timers, subscriptions).
    /// Called by both <see cref="Dispose"/> and <see cref="DisposeAsync"/>.
    /// Asynchronous cleanup (JS module, IJSObjectReference) should go in <see cref="DisposeAsyncCore"/>.
    /// </summary>
    protected virtual void Dispose(bool disposing) { }

    /// <summary>Override to release async resources (JS modules, network handles).</summary>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            _cts?.Cancel();
            Dispose(true);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            GC.SuppressFinalize(this);
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            _cts?.Cancel();
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(true);
        }
        catch (JSDisconnectedException) { }
        catch (OperationCanceledException) { }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            GC.SuppressFinalize(this);
        }
    }
}
