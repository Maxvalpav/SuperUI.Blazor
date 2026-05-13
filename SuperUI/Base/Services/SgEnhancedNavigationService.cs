// SuperUI/Base/Services/SgEnhancedNavigationService.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ FIX CS0407: OnLocationChanging возвращает ValueTask вместо Task
// ✅ FIX: убрано лишнее поле _locationChangedSubscription
// ✅ THREAD FIX: CopyOnWrite pattern для _navigatingHandlers/_navigatedHandlers
// ✅ NEW: используем NavigationManager.Refresh() в .NET 8+
// ✅ NEW: OnBeforeUnload — beforeunload confirmation dialog (JS interop)

using System.Collections.Immutable;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис для работы с Enhanced Navigation (.NET 8+).
/// </summary>
public interface IEnhancedNavigationService
{
    IDisposable OnNavigating(Func<LocationChangingContext, ValueTask> handler);
    IDisposable OnNavigated(Action<LocationChangedEventArgs> handler);
    IDisposable OnBeforeUnload(Func<string?> confirmationMessageProvider);
    bool IsEnhancedNavigationEnabled { get; }
    Task RefreshAsync(bool forceReload = false);
}

public sealed class SgEnhancedNavigationService : IEnhancedNavigationService, IDisposable, IAsyncDisposable
{
    private readonly NavigationManager _navigationManager;
    private readonly IJSRuntime _js;

    // THREAD FIX: ImmutableArray для lock-free чтения + атомарная замена
    private volatile ImmutableArray<Func<LocationChangingContext, ValueTask>> _navigatingHandlers
        = ImmutableArray<Func<LocationChangingContext, ValueTask>>.Empty;

    private volatile ImmutableArray<Action<LocationChangedEventArgs>> _navigatedHandlers
        = ImmutableArray<Action<LocationChangedEventArgs>>.Empty;

    private readonly IDisposable? _locationChangingSubscription;
    private readonly List<Func<string?>> _beforeUnloadProviders = new();
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
            // FIX CS0407: передаём метод с сигнатурой ValueTask
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

    public IDisposable OnNavigating(Func<LocationChangingContext, ValueTask> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ImmutableInterlocked.Update(ref _navigatingHandlers, h => h.Add(handler));
        return new UnsubscribeDisposable(() =>
            ImmutableInterlocked.Update(ref _navigatingHandlers, h => h.Remove(handler)));
    }

    public IDisposable OnNavigated(Action<LocationChangedEventArgs> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ImmutableInterlocked.Update(ref _navigatedHandlers, h => h.Add(handler));
        return new UnsubscribeDisposable(() =>
            ImmutableInterlocked.Update(ref _navigatedHandlers, h => h.Remove(handler)));
    }

    public IDisposable OnBeforeUnload(Func<string?> confirmationMessageProvider)
    {
        ArgumentNullException.ThrowIfNull(confirmationMessageProvider);
        lock (_beforeUnloadProviders)
        {
            _beforeUnloadProviders.Add(confirmationMessageProvider);
            _ = EnsureBeforeUnloadInitializedAsync(); // fire and forget
        }
        return new UnsubscribeDisposable(() =>
        {
            lock (_beforeUnloadProviders)
                _beforeUnloadProviders.Remove(confirmationMessageProvider);
        });
    }

    public Task RefreshAsync(bool forceReload = false)
    {
        // .NET 8+: NavigationManager.Refresh() — правильный способ
        // Fallback для .NET 7: NavigateTo с forceLoad
        try
        {
            // Используем рефлексию чтобы вызвать Refresh() если доступен (.NET 8+)
            var refreshMethod = _navigationManager.GetType()
                .GetMethod("Refresh", System.Reflection.BindingFlags.Public |
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

    // FIX CS0407: ValueTask вместо Task
    private async ValueTask OnLocationChanging(LocationChangingContext context)
    {
        // Snapshot (ImmutableArray) — безопасно итерировать без lock
        var handlers = _navigatingHandlers;
        foreach (var handler in handlers)
        {
            if (context.IsNavigationIntercepted) break;
            await handler(context);
        }
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var handlers = _navigatedHandlers;
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
            // JS not available or fails — silently ignore
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _navigationManager.LocationChanged -= OnLocationChanged;
        _locationChangingSubscription?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule is not null)
        {
            try { await _jsModule.DisposeAsync(); } catch { }
        }
        _dotNetRef?.Dispose();
        Dispose();
    }

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
