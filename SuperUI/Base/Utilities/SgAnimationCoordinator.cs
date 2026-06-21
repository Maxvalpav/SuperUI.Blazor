// SuperUI/Base/Utilities/SgAnimationCoordinator.cs
// Централизованное управление анимациями: prefers-reduced-motion, параллельные
// анимации, идемпотентная отмена. Решает паттерн "каждый оверлей хранит
// свой _animationCts и Task.Delay".

using System.Threading;
using Microsoft.JSInterop;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Координатор анимаций: <c>prefers-reduced-motion</c> detection, идемпотентная
/// отмена через единый CTS, поддержка параллельных анимаций.
/// </summary>
/// <remarks>
/// <para>Используйте вместо прямого <c>Task.Delay(ClosingAnimationMs, _animationCts.Token)</c>.
/// Координатор сам определяет, нужно ли вообще запускать анимацию (если
/// пользователь предпочитает reduced motion — задержка = 0).</para>
/// <para>Scoped lifetime: каждый Blazor-цикл / WASM-сессия получает свой экземпляр
/// (разделяет состояние reduced-motion и cancellation).</para>
/// <para>Пример:</para>
/// <code>
/// // В OnClosingAsync:
/// using var cts = Animation.Begin(ClosingAnimationMs);
/// await cts.WaitAsync();
/// // ... дальнейшая логика
/// </code>
/// </remarks>
public sealed class SgAnimationCoordinator : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private bool _reducedMotionCached;
    private bool _reducedMotion;
    private CancellationTokenSource? _lifetimeCts;
    private int _disposed;

    /// <summary>Creates a new coordinator.</summary>
    public SgAnimationCoordinator(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>
    /// True, если пользователь предпочитает reduced motion (prefers-reduced-motion: reduce).
    /// Запрашивается у браузера лениво при первом вызове.
    /// </summary>
    public async ValueTask<bool> PrefersReducedMotionAsync()
    {
        if (_reducedMotionCached) return _reducedMotion;
        try
        {
            _reducedMotion = await _js.InvokeAsync<bool>("eval",
                "window.matchMedia('(prefers-reduced-motion: reduce)').matches");
        }
        catch (JSDisconnectedException) { _reducedMotion = false; }
        catch (TaskCanceledException)   { _reducedMotion = false; }
        catch (JSException)             { _reducedMotion = false; }
        _reducedMotionCached = true;
        return _reducedMotion;
    }

    /// <summary>
    /// Starts a coordinated animation. Returns a <see cref="CancellableDelay"/>
    /// that resolves after <paramref name="durationMs"/> ms (or immediately if
    /// reduced motion is enabled).
    /// </summary>
    /// <remarks>
    /// If the coordinator is disposed before the delay completes, the await
    /// throws <see cref="OperationCanceledException"/> — same semantics as
    /// <c>Task.Delay(ms, ct)</c>.
    /// </remarks>
    public async ValueTask<CancellableDelay> BeginAsync(int durationMs)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(durationMs);
        if (Volatile.Read(ref _disposed) == 1) return new CancellableDelay(0, CancellationToken.None);

        if (durationMs == 0) return new CancellableDelay(0, CancellationToken.None);
        if (await PrefersReducedMotionAsync()) return new CancellableDelay(0, CancellationToken.None);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(GetLifetimeToken());
        return new CancellableDelay(durationMs, cts);
    }

    /// <summary>Synchronous <see cref="BeginAsync(int)"/> — use when you already know reduced-motion state.</summary>
    public CancellableDelay Begin(int durationMs, bool reducedMotion = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(durationMs);
        if (Volatile.Read(ref _disposed) == 1) return new CancellableDelay(0, CancellationToken.None);
        if (durationMs == 0 || reducedMotion) return new CancellableDelay(0, CancellationToken.None);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(GetLifetimeToken());
        return new CancellableDelay(durationMs, cts);
    }

    private CancellationToken GetLifetimeToken()
    {
        _lifetimeCts ??= new CancellationTokenSource();
        return _lifetimeCts.Token;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return ValueTask.CompletedTask;
        var cts = Interlocked.Exchange(ref _lifetimeCts, null);
        if (cts is null) return ValueTask.CompletedTask;
        try { cts.Cancel(); } catch (ObjectDisposedException) { }
        cts.Dispose();
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Awaitable animation delay with built-in cancellation. Resolves with
/// <see cref="Task.Delay(int, CancellationToken)"/> semantics.
/// </summary>
public readonly struct CancellableDelay
{
    private readonly int _ms;
    private readonly CancellationToken _token;
    // When non-null, this delay owns a linked CTS that must be disposed once the delay
    // settles — otherwise each animation leaks a registration on the lifetime token.
    private readonly CancellationTokenSource? _ownedCts;

    internal CancellableDelay(int ms, CancellationToken token)
    {
        _ms = ms;
        _token = token;
        _ownedCts = null;
    }

    internal CancellableDelay(int ms, CancellationTokenSource ownedCts)
    {
        _ms = ms;
        _ownedCts = ownedCts;
        _token = ownedCts.Token;
    }

    /// <summary>Total duration in milliseconds (0 = no delay).</summary>
    public int DurationMs => _ms;

    /// <summary>True if the underlying cancellation token was already triggered.</summary>
    public bool IsCancellationRequested => _token.IsCancellationRequested;

    /// <summary>
    /// Returns a task that completes after the delay (or immediately if DurationMs=0).
    /// </summary>
    public async Task WaitAsync()
    {
        if (_ms <= 0) { _ownedCts?.Dispose(); return; }
        try
        {
            await Task.Delay(_ms, _token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { /* coordinator disposed mid-delay */ }
        finally { _ownedCts?.Dispose(); }
    }

    /// <summary>Returns a task that completes after the delay (or throws if cancelled).</summary>
    public async Task OrThrowAsync()
    {
        if (_ms <= 0) { _ownedCts?.Dispose(); return; }
        try
        {
            await Task.Delay(_ms, _token).ConfigureAwait(false);
        }
        finally { _ownedCts?.Dispose(); }
    }
}
