// SuperUI/Base/Optimization/SgRenderBudgetGuard.cs — НОВЫЙ
// Что это: ограничитель частоты рендеров.
// Если компонент пытается рендериться чаще N раз в секунду — throttles.
// Аналог: RateLimiter в ASP.NET Core, но для Blazor компонентов.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SuperUI.Base.Optimization;

/// <summary>
/// Защита от перегрузки рендерами на уровне компонента.
/// Ограничивает число рендеров в секунду.
///
/// Использование:
/// <code>
/// private readonly SgRenderBudgetGuard _guard = new(maxPerSecond: 60);
///
/// public void RequestRender()
/// {
///     if (_guard.TryRender())
///         base.RequestRender();
/// }
/// </code>
/// </summary>
public sealed class SgRenderBudgetGuard
{
    private readonly int _maxPerSecond;
    private readonly int _burstSize;
    private long _lastRefillTimestamp;
    private int _tokens;
    private readonly double _refillRate; // токенов в секунду

    /// <param name="maxPerSecond">Максимальное число рендеров в секунду.</param>
    /// <param name="burstSize">Максимальный burst (начальный запас токенов).</param>
    public SgRenderBudgetGuard(int maxPerSecond = 60, int burstSize = 5)
    {
        _maxPerSecond = Math.Max(1, maxPerSecond);
        _burstSize = Math.Max(1, burstSize);
        _tokens = _burstSize;
        _refillRate = (double)_maxPerSecond / Stopwatch.Frequency;
        _lastRefillTimestamp = Stopwatch.GetTimestamp();
    }

    /// <summary>Максимальное число рендеров в секунду.</summary>
    public int MaxPerSecond => _maxPerSecond;

    /// <summary>Доступно токенов для рендера.</summary>
    public int AvailableTokens => Volatile.Read(ref _tokens);

    /// <summary>
    /// Попытаться выполнить рендер. Возвращает true если рендер разрешён.
    /// Потокобезопасен.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRender()
    {
        RefillTokens();

        int current;
        do
        {
            current = Volatile.Read(ref _tokens);
            if (current <= 0) return false;
        }
        while (Interlocked.CompareExchange(ref _tokens, current - 1, current) != current);

        return true;
    }

    /// <summary>
    /// Ждать пока токен не станет доступен (с таймаутом).
    /// </summary>
    public async Task<bool> WaitForRenderAsync(TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();

        while (sw.Elapsed < timeout)
        {
            if (TryRender()) return true;
            await Task.Delay(16).ConfigureAwait(false); // ~60fps wait
        }

        return TryRender(); // последняя попытка
    }

    /// <summary>Сбросить счётчики.</summary>
    public void Reset()
    {
        Volatile.Write(ref _tokens, _burstSize);
        Volatile.Write(ref _lastRefillTimestamp, Stopwatch.GetTimestamp());
    }

    private void RefillTokens()
    {
        var now = Stopwatch.GetTimestamp();
        var last = Volatile.Read(ref _lastRefillTimestamp);

        // Только один поток обновляет
        if (Interlocked.CompareExchange(ref _lastRefillTimestamp, now, last) != last)
            return; // уже обновлено другим потоком

        var elapsed = Stopwatch.GetElapsedTime(last, now);
        var tokensToAdd = (int)(elapsed.TotalSeconds * _refillRate * Stopwatch.Frequency);

        if (tokensToAdd > 0)
        {
            var current = Volatile.Read(ref _tokens);
            var desired = Math.Min(current + tokensToAdd, _burstSize);
            Interlocked.Exchange(ref _tokens, desired);
        }
    }
}
