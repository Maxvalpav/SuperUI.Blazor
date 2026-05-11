using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SuperUI.Base.Services;
using SuperUI.Base.Tokens;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов, использующих JavaScript Interop.
/// ИСПРАВЛЕНО: SpinWait → SemaphoreSlim для корректной работы на Blazor Server.
/// </summary>
public abstract class SgJsComponentBase : SgComponentBase
{
    // ── Инъекции ────────────────────────────────────────────────────────────
    [Inject] protected IJSRuntime JS { get; set; } = null!;
    [Inject] protected IPrerendingDetector PrerendingDetector { get; set; } = null!;

    // ── JS Module ────────────────────────────────────────────────────────────
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private IJSObjectReference? _module;

    protected virtual string? JsModulePath => null;

    // ── DotNetRef ────────────────────────────────────────────────────────────
    private DotNetObjectReference<SgJsComponentBase>? _dotNetRef;

    protected DotNetObjectReference<SgJsComponentBase> DotNetRef
    {
        get
        {
            if (_dotNetRef is null)
            {
                var newRef = DotNetObjectReference.Create<SgJsComponentBase>(this);
                var existing = Interlocked.CompareExchange(ref _dotNetRef, newRef, null);
                if (existing is not null)
                    newRef.Dispose();
            }
            return _dotNetRef;
        }
    }

    // ── Prerendering ─────────────────────────────────────────────────────────
    protected bool IsPrerendering => PrerendingDetector.IsPrerendering;

    // ── LifecycleToken ───────────────────────────────────────────────────────
    private LifecycleToken? _lifecycleToken;
    protected CancellationToken ComponentToken =>
        (_lifecycleToken ??= new LifecycleToken()).Token;

    protected override void OnInitialized()
    {
        var old = Interlocked.Exchange(ref _lifecycleToken, new LifecycleToken());
        old?.Cancel();
        old?.Dispose();
        base.OnInitialized();
    }

    // ── GetModuleAsync ────────────────────────────────────────────────────────
    protected async ValueTask<IJSObjectReference?> GetModuleAsync()
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return null;

        if (_module is not null) return _module;

        await _moduleLock.WaitAsync(ComponentToken).ConfigureAwait(false);
        try
        {
            if (_module is not null) return _module;
            if (IsDisposed || ComponentToken.IsCancellationRequested) return null;

            var path = JsModulePath ?? "_content/SuperUI/superui.js";
            _module = await JS.InvokeAsync<IJSObjectReference>(
                "import", ComponentToken, path);
            return _module;
        }
        catch (TaskCanceledException) { return null; }
        catch (OperationCanceledException) { return null; }
        catch (JSDisconnectedException) { return null; }
        catch (ObjectDisposedException) { return null; }
        catch (JSException ex)
        {
            Logger.LogError(ex, "[{Id}] JS module load failed: {Path}",
                ComponentId, JsModulePath);
            return null;
        }
        finally
        {
            if (!IsDisposed)
            {
                try { _moduleLock.Release(); }
                catch (ObjectDisposedException) { }
            }
        }
    }

    protected async ValueTask SafeInvokeVoidAsync(string identifier, params object?[] args)
    {
#if DEBUG
        Diagnostics.JsCallCount++;
#endif
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;
            await module.InvokeVoidAsync(identifier, ComponentToken, args);
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (Exception ex)
        {
#if DEBUG
            Diagnostics.JsErrorCount++;
#endif
            Logger.LogError(ex, "[{ComponentId}] JS call '{Identifier}' failed",
                ComponentId, identifier);
        }
    }

    protected async ValueTask<T?> SafeInvokeAsync<T>(string identifier, params object?[] args)
    {
#if DEBUG
        Diagnostics.JsCallCount++;
#endif
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested)
            return default;
        try
        {
            var module = await GetModuleAsync();
            if (module is null) return default;
            return await module.InvokeAsync<T>(identifier, ComponentToken, args);
        }
        catch (TaskCanceledException) { return default; }
        catch (OperationCanceledException) { return default; }
        catch (JSDisconnectedException) { return default; }
        catch (ObjectDisposedException) { return default; }
        catch (Exception ex)
        {
#if DEBUG
            Diagnostics.JsErrorCount++;
#endif
            Logger.LogError(ex, "[{ComponentId}] JS call '{Identifier}' failed",
                ComponentId, identifier);
            return default;
        }
    }

    protected async ValueTask SafeGlobalInvokeVoidAsync(string identifier, params object?[] args)
    {
        if (IsPrerendering || IsDisposed || ComponentToken.IsCancellationRequested) return;
        try
        {
            await JS.InvokeVoidAsync(identifier, ComponentToken, args);
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
    }

    // ── Dispose ──────────────────────────────────────────────────────────────
    protected override async ValueTask DisposeComponentAsync()
    {
        _lifecycleToken?.Cancel();

        _moduleLock.Dispose();

        if (_module is not null)
        {
            try { await _module.DisposeAsync(); }
            catch { /* ignore */ }
            _module = null;
        }

        _dotNetRef?.Dispose();
        _dotNetRef = null;

        _lifecycleToken?.Dispose();
        _lifecycleToken = null;

        await base.DisposeComponentAsync();
    }
}
