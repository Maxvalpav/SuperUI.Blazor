// SuperUI/Base/Reactive/SgIntersectionObserver.cs — НОВЫЙ КЛАСС
// Поддержка: .NET 8/9/10, InteractiveServer + WASM
// SSR: graceful degradation (нет JS → IsVisible = false)
// Аналог: Angular CDK IntersectionObserver, React IntersectionObserver hook

using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Наблюдатель пересечения элемента с viewport (Intersection Observer API).
/// Позволяет реализовывать:
/// - Lazy loading изображений
/// - Анимации при появлении элемента
/// - Паузу видео/анимаций вне viewport
/// - Infinite scroll триггеры
/// <para>
/// Регистрация: <c>builder.Services.AddScoped&lt;SgIntersectionObserver&gt;()</c>
/// </para>
/// </summary>
public sealed class SgIntersectionObserver : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly ILogger<SgIntersectionObserver>? _logger;
    private IJSObjectReference? _jsModule;
    private readonly Dictionary<string, IntersectionSubscription> _subscriptions = [];
    private int _disposed;
    private DotNetObjectReference<SgIntersectionObserver>? _dotNetRef;

    public SgIntersectionObserver(IJSRuntime js, ILogger<SgIntersectionObserver>? logger = null)
    {
        _js = js;
        _logger = logger;
    }

    /// <summary>
    /// Наблюдать за элементом по ElementReference.
    /// Возвращает <see cref="IDisposable"/> для отмены наблюдения.
    /// </summary>
    public async Task<IDisposable> ObserveAsync(
        ElementReference element,
        Action<IntersectionState> onChanged,
        IntersectionObserverOptions? options = null)
    {
        if (Volatile.Read(ref _disposed) == 1)
            throw new ObjectDisposedException(nameof(SgIntersectionObserver));

        await EnsureInitializedAsync();

        var id = Guid.NewGuid().ToString("N");
        var subscription = new IntersectionSubscription(id, onChanged);
        _subscriptions[id] = subscription;

        try
        {
            await _jsModule!.InvokeVoidAsync(
                "observe",
                id, element, _dotNetRef,
                options?.Threshold ?? 0.0,
                options?.RootMargin ?? "0px");
        }
        catch (JSException ex)
        {
            _subscriptions.Remove(id);
            _logger?.LogWarning(ex, "[SgIntersectionObserver] Failed to start observing element.");
            throw;
        }

        return new Subscription(() => _ = UnobserveAsync(id));
    }

    /// <summary>Callback из JS при изменении состояния пересечения.</summary>
    [JSInvokable]
    public void OnIntersectionChanged(string id, bool isIntersecting, double ratio)
    {
        if (_subscriptions.TryGetValue(id, out var sub))
        {
            var state = new IntersectionState(isIntersecting, ratio);
            try { sub.Callback(state); }
            catch (Exception ex) { _logger?.LogError(ex, "[SgIntersectionObserver] Callback error."); }
        }
    }

    private async Task UnobserveAsync(string id)
    {
        _subscriptions.Remove(id);
        if (_jsModule is not null)
        {
            try { await _jsModule.InvokeVoidAsync("unobserve", id); }
            catch (JSException) { }
        }
    }

    private async Task EnsureInitializedAsync()
    {
        if (_jsModule is not null) return;

        try
        {
            // JS модуль должен быть добавлен в wwwroot/js/sg-intersection-observer.js
            _jsModule = await _js.InvokeAsync<IJSObjectReference>(
                "import", "./_content/SuperUI/js/sg-intersection-observer.js");
            _dotNetRef = DotNetObjectReference.Create(this);
        }
        catch (JSException ex)
        {
            _logger?.LogDebug(ex, "[SgIntersectionObserver] JS module not available. SSR mode?");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        _subscriptions.Clear();

        if (_jsModule is not null)
        {
            try { await _jsModule.InvokeVoidAsync("disposeAll"); }
            catch { }
            await _jsModule.DisposeAsync();
        }

        _dotNetRef?.Dispose();
    }

    private sealed record IntersectionSubscription(string Id, Action<IntersectionState> Callback);
}

/// <summary>Состояние пересечения элемента с viewport.</summary>
public sealed record IntersectionState(bool IsIntersecting, double IntersectionRatio);

/// <summary>Опции для Intersection Observer.</summary>
public sealed class IntersectionObserverOptions
{
    /// <summary>Порог срабатывания (0.0 - 1.0). По умолчанию: 0.0 (любое пересечение).</summary>
    public double Threshold { get; set; } = 0.0;

    /// <summary>Отступы от viewport (CSS margin). По умолчанию: "0px".</summary>
    public string RootMargin { get; set; } = "0px";
}
