// SuperUI/Base/Hooks/PerformanceHook.cs
// ИСПРАВЛЕНО:
// 1. Console.WriteLine → Action<string> output (конфигурируемый, нет зависимости от Console)
// 2. _renderStart: Interlocked.Exchange / Interlocked.Read (thread-safe на Server/ARM)
// 3. Порог (16мс) вынесен в конструктор
using System.Diagnostics;
using SuperUI.Base;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Хук для логирования времени рендера.
/// Вызывает <see cref="Output"/> если рендер превышает порог.
/// </summary>
/// <remarks>
/// По умолчанию порог 16 мс (1 кадр @ 60fps).
/// Для production используйте <see cref="SgPerformanceInterceptor"/> с ILogger.
/// </remarks>
public sealed class PerformanceHook : IAsyncComponentHook, IRenderHook
{
    private long _renderStart;
    private readonly double _thresholdMs;

    /// <summary>Функция вывода. По умолчанию — Debug.WriteLine (нет Console в production).</summary>
    public Action<string> Output { get; set; } = msg => Debug.WriteLine(msg);

    public PerformanceHook(double thresholdMs = 16.0) => _thresholdMs = thresholdMs;

    // IRenderHook — начало замера
    public bool ShouldRender(SgComponentBase c)
    {
        Interlocked.Exchange(ref _renderStart, Stopwatch.GetTimestamp());
        return true;
    }

    // IComponentHook
    public void OnInitialized(SgComponentBase c) { }
    public void OnParametersSet(SgComponentBase c) { }

    public void OnAfterRender(SgComponentBase c, bool firstRender)
    {
        var start = Interlocked.Read(ref _renderStart);
        if (start == 0) return;

        var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        if (elapsed > _thresholdMs)
            Output($"[PERF] {c.ComponentId}: {elapsed:F1}ms");
    }

    // IAsyncComponentHook — default-реализации
    public Task OnInitializedAsync(SgComponentBase c) => Task.CompletedTask;
    public Task OnParametersSetAsync(SgComponentBase c) => Task.CompletedTask;
    public Task OnAfterRenderAsync(SgComponentBase c, bool firstRender) => Task.CompletedTask;
}