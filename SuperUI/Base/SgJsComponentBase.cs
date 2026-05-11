// SuperUI/Base/SgJsComponentBase.cs
// ИСПРАВЛЕНО:
// 1. ObjectDisposedException ловится в finally блоке GetModuleAsync
// 2. DisposeComponentAsync — правильный порядок: Cancel → модуль → ref → CTS
// 3. _moduleLock.Release() защищён от двойного вызова
// 4. DotNetRef создаётся атомарно через Interlocked (уже было — OK)
// 5. SafeInvokeVoidAsync — использует overrideToken для Dispose-сценария

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SuperUI.Base.Services;
using SuperUI.Base.Tokens;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов, использующих JavaScript Interop.
/// SemaphoreSlim(1,1) вместо SpinWait — корректно на Blazor Server.
/// </summary>
public abstract class SgJsComponentBase : SgComponentBase
{
    // ── Инъекции ──────────────────────────────────────────────────────────────
    [Inject] protected IJSRuntime JS { get; set; } = null!;
    [Inject] protected IPrerendingDetector PrerendingDetector { get; set; } = null!;

    // ── JS Module ──────────────────────────────────────────────────────────────
    private readonly SemaphoreSlim _moduleLock = new(1, 1);
    private volatile bool _moduleLockDisposed;
    private IJSObjectReference? _module;
    protected virtual string? JsModulePath => null;

    // ── DotNetRef — атомарное создание ───────────────────────────────────────
    private DotNetObjectReference<SgJsComponentBase>? _dotNetRef;
    protected DotNetObjectReference<SgJsComponentBase> DotNetRef
    {
        get
        {
            if (_dotNetRef is null)
            {
                var newRef = DotNetObjectReference.Create<SgJsComponentBase>(this);
                var existing = Interlocked.CompareExchange(ref _dotNetRef, newRef, null);
                if (existing is not null) newRef.Dispose(); // проиграли гонку
            }
            return _dotNetRef;
        }
    }

    // ── Prerendering ──────────────────────────────────────────────────────────
    protected bool IsPrerendering => PrerendingDetector.IsPrerendering;

    // ── LifecycleToken ────────────────────────────────────────────────────────
    private LifecycleToken? _lifecycleToken;
    protected CancellationToken ComponentToken =>
        (_lifecycleToken ??= new LifecycleToken()).Token;

    protected override void OnInitialized()
    {
        // Пересоздаём токен при переинициализации (например Hot Reload)
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

        // Быстрый путь без блокировки
        if (_module is not null) return _module;

        // ИСПРАВЛЕНО: передаём ComponentToken в WaitAsync — прервётся при Dispose
        if (_moduleLockDisposed) return null;
        try
        {
            await _moduleLock.WaitAsync(ComponentToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { return null; }
        catch (ObjectDisposedException) { return null; }

        try
        {
            if (_module is not null) return _module; // double-check
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
            // ИСПРАВЛЕНО: Release только если семафор не был Dispose'd
            if (!_moduleLockDisposed)
            {
                try { _moduleLock.Release(); }
                catch (ObjectDisposedException) { }
                catch (SemaphoreFullException) { }
            }
        }
    }

    // ── SafeInvokeVoidAsync ───────────────────────────────────────────────────
    protected async ValueTask SafeInvokeVoidAsync(
        string identifier,
        CancellationToken? overrideToken = null,
        params object?[] args)
    {
#if DEBUG
        Diagnostics.JsCallCount++;
#endif
        // ИСПРАВЛЕНО: при Dispose используем overrideToken, игнорируем ComponentToken
        var ct = overrideToken ?? ComponentToken;
        if (IsPrerendering || (overrideToken is null && IsDisposed)) return;
        if (ct.IsCancellationRequested) return;

        try
        {
            var module = await GetModuleAsync();
            if (module is null) return;
            await module.InvokeVoidAsync(identifier, ct, args);
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

    // Перегрузка без overrideToken для нормального использования
    protected ValueTask SafeInvokeVoidAsync(string identifier, params object?[] args)
        => SafeInvokeVoidAsync(identifier, null, args);

    protected async ValueTask<TResult?> SafeInvokeAsync<TResult>(
        string identifier,
        params object?[] args)
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
            return await module.InvokeAsync<TResult>(identifier, ComponentToken, args);
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

    protected async ValueTask SafeGlobalInvokeVoidAsync(
        string identifier,
        params object?[] args)
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

    // ── Dispose ───────────────────────────────────────────────────────────────
    protected override async ValueTask DisposeComponentAsync()
    {
        // 1. Отменяем токен ПЕРВЫМ — прерывает все ожидающие операции
        _lifecycleToken?.Cancel();

        // 2. Помечаем семафор как disposed ДО его Dispose — предотвращает Race
        _moduleLockDisposed = true;
        _moduleLock.Dispose();

        // 3. Утилизируем JS модуль
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); }
            catch { /* ignore — connection may be gone */ }
            _module = null;
        }

        // 4. DotNet ref
        _dotNetRef?.Dispose();
        _dotNetRef = null;

        // 5. Lifecycle token (уже отменён, теперь освобождаем)
        _lifecycleToken?.Dispose();
        _lifecycleToken = null;

        await base.DisposeComponentAsync();
    }
}
