// SuperUI/Base/Services/SgEnhancedNavigationService.cs
// ИСПРАВЛЕНИЯ v3:
// ✅ FIX CS0677 (x2): заменён volatile ImmutableArray<...> на CopyOnWriteList<T>
//     (ImmutableArray<T> не может быть volatile в .NET 8+)
// ✅ NEW: BlockNavigation — полная блокировка навигации (например, при сохранении)
// ✅ NEW: BeforeUnloadHandler использует OnBeforeUnload JS API (MDN standard)
// ✅ PERF: CopyOnWriteList — Volatile.Read для lock-free чтения, lock только при записи

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using SuperUI.Base.Utilities;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис для работы с Enhanced Navigation (.NET 8+).
/// </summary>
public interface IEnhancedNavigationService
{
    /// <summary>Подписаться на событие начала навигации (позволяет отменить переход).</summary>
    IDisposable OnNavigating(Func<LocationChangingContext, ValueTask> handler);

    /// <summary>Подписаться на событие завершения навигации.</summary>
    IDisposable OnNavigated(Action<LocationChangedEventArgs> handler);

    /// <summary>
    /// Подписаться на beforeunload (предупреждение при уходе со страницы).
    /// Возвращает сообщение для пользователя или null (если можно уходить).
    /// </summary>
    IDisposable OnBeforeUnload(Func<string?> confirmationMessageProvider);

    /// <summary>
    /// Заблокировать навигацию (например, во время асинхронного сохранения).
    /// Blocker возвращает true — навигация заблокирована.
    /// </summary>
    IDisposable BlockNavigation(Func<LocationChangingContext, ValueTask<bool>> blocker);

    /// <summary>Включена ли Enhanced Navigation.</summary>
    bool IsEnhancedNavigationEnabled { get; }

    /// <summary>Принудительно обновить страницу.</summary>
    Task RefreshAsync(bool forceReload = false);
}

public sealed class SgEnhancedNavigationService : IEnhancedNavigationService, IAsyncDisposable
{
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _js;

    // ✅ FIX CS0677: SgCopyOnWriteList вместо volatile ImmutableArray<T>
    private readonly SgCopyOnWriteList<Func<LocationChangingContext, ValueTask>> _navigatingHandlers = new();
    private readonly SgCopyOnWriteList<Action<LocationChangedEventArgs>> _navigatedHandlers = new();

    private readonly List<Func<string?>> _beforeUnloadProviders = new();
    private readonly List<Func<LocationChangingContext, ValueTask<bool>>> _navigationBlockers = new();

    private readonly IDisposable? _locationChangingSubscription;
    private int _disposed;

    // JS interop для beforeunload
    private DotNetObjectReference<SgEnhancedNavigationService>? _dotNetRef;
    private IJSObjectReference? _jsModule;
    private bool _beforeUnloadInitialized;

    public bool IsEnhancedNavigationEnabled { get; }

    public SgEnhancedNavigationService(NavigationManager navigationManager, IJSRuntime js)
    {
        _navigationManager = navigationManager;
        _js = js;

        try
        {
            _locationChangingSubscription =
                _navigationManager.RegisterLocationChangingHandler(OnLocationChanging);
            IsEnhancedNavigationEnabled = true;
        }
        catch (InvalidOperationException)
        {
            IsEnhancedNavigationEnabled = false;
        }

        _navigationManager.LocationChanged += OnLocationChanged;
    }

    // ── Subscriptions ──────────────────────────────────────────────────────────

    public IDisposable OnNavigating(Func<LocationChangingContext, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _navigatingHandlers.Add(handler);
        return new UnsubscribeDisposable(() => _navigatingHandlers.Remove(handler));
    }

    public IDisposable OnNavigated(Action<LocationChangedEventArgs> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        _navigatedHandlers.Add(handler);
        return new UnsubscribeDisposable(() => _navigatedHandlers.Remove(handler));
    }

    public IDisposable OnBeforeUnload(Func<string?> confirmationMessageProvider)
    {
        ArgumentNullException.ThrowIfNull(confirmationMessageProvider);
        lock (_beforeUnloadProviders)
        {
            _beforeUnloadProviders.Add(confirmationMessageProvider);
            _ = EnsureBeforeUnloadInitializedAsync();
        }
        return new UnsubscribeDisposable(() =>
        {
            lock (_beforeUnloadProviders)
                _beforeUnloadProviders.Remove(confirmationMessageProvider);
        });
    }

    public IDisposable BlockNavigation(Func<LocationChangingContext, ValueTask<bool>> blocker)
    {
        ArgumentNullException.ThrowIfNull(blocker);
        lock (_navigationBlockers)
            _navigationBlockers.Add(blocker);
        return new UnsubscribeDisposable(() =>
        {
            lock (_navigationBlockers)
                _navigationBlockers.Remove(blocker);
        });
    }

    public Task RefreshAsync(bool forceReload = false)
    {
        try
        {
            // .NET 8+: NavigationManager.Refresh(bool)
            var refreshMethod = _navigationManager.GetType()
                .GetMethod("Refresh",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance,
                    [typeof(bool)]);

            if (refreshMethod is not null)
            {
                refreshMethod.Invoke(_navigationManager, [forceReload]);
                return Task.CompletedTask;
            }
        }
        catch { /* fallback */ }

        _navigationManager.NavigateTo(_navigationManager.Uri, forceLoad: forceReload);
        return Task.CompletedTask;
    }

    // ── Internal handlers ──────────────────────────────────────────────────────

    private async ValueTask OnLocationChanging(LocationChangingContext context)
    {
        // Сначала проверяем блокировщики
        Func<LocationChangingContext, ValueTask<bool>>[] blockers;
        lock (_navigationBlockers)
            blockers = _navigationBlockers.ToArray();

        foreach (var blocker in blockers)
        {
            try
            {
                if (await blocker(context))
                {
                    context.PreventNavigation();
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SgEnhancedNavigation] Navigation blocker error: {ex.Message}");
            }
        }

        // Затем оповещаем подписчиков (lock-free snapshot)
        var handlers = _navigatingHandlers.Snapshot();
        foreach (var handler in handlers)
        {
            if (context.IsNavigationIntercepted) break;
            try { await handler(context); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SgEnhancedNavigation] Navigating handler error: {ex.Message}");
            }
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var handlers = _navigatedHandlers.Snapshot();
        foreach (var handler in handlers)
        {
            try { handler(e); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SgEnhancedNavigation] LocationChanged handler error: {ex.Message}");
            }
        }
    }

    [JSInvokable]
    public string? GetBeforeUnloadConfirmation()
    {
        lock (_beforeUnloadProviders)
        {
            foreach (var provider in _beforeUnloadProviders)
            {
                try
                {
                    var msg = provider();
                    if (!string.IsNullOrEmpty(msg))
                        return msg;
                }
                catch { }
            }
        }
        return null;
    }

    private async Task EnsureBeforeUnloadInitializedAsync()
    {
        if (_beforeUnloadInitialized) return;
        if (!OperatingSystem.IsBrowser()) return;

        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            _jsModule = await _js.InvokeAsync<IJSObjectReference>(
                "import", "./_content/SuperUI/superui.js");
            await _jsModule.InvokeVoidAsync("addBeforeUnloadListener", _dotNetRef);
            _beforeUnloadInitialized = true;
        }
        catch
        {
            // JS недоступен или упал — игнорируем
        }
    }

    // ── Dispose ────────────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        _navigationManager.LocationChanged -= OnLocationChanged;
        _locationChangingSubscription?.Dispose();

        if (_jsModule is not null)
        {
            try { await _jsModule.DisposeAsync(); } catch { }
        }
        _dotNetRef?.Dispose();
    }

    // ── UnsubscribeDisposable ──────────────────────────────────────────────────

    private sealed class UnsubscribeDisposable : IDisposable
    {
        private readonly Action _unsubscribe;
        private int _disposed;

        public UnsubscribeDisposable(Action unsubscribe) => _unsubscribe = unsubscribe;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            _unsubscribe();
        }
    }
}
