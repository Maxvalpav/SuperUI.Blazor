// SgRenderGate.cs — Gate для контроля частоты рендеров 
// Предотвращает слишком частые перерисовки, батчит обновления 
 
using System;
using System.Diagnostics; 
using System.Threading;
using System.Threading.Tasks;

namespace SuperUI.Base.Utilities; 
 
/// <summary> 
/// Контролирует частоту рендеров, ограничивая их заданным интервалом. 
/// Позволяет RequestRenderAsync() вызываться многократно, но фактический рендер 
/// выполняется не чаще чем раз в minInterval. 
/// 
/// Использование: 
/// <code> 
/// private readonly SgRenderGate _renderGate = new(RenderAsync, TimeSpan.FromMilliseconds(16)); 
/// 
/// public async Task RefreshAsync() => await _renderGate.RequestRenderAsync(); 
/// </code> 
/// </summary> 
public sealed class SgRenderGate : IDisposable 
{ 
    private readonly Func<Task> _renderAction; 
    private readonly TimeSpan _minInterval; 
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew(); 
    private bool _isRenderScheduled; 
    private bool _isDisposed; 
    private readonly object _lock = new(); 
    private Timer? _timer; 
 
    /// <summary> 
    /// Время последнего рендера. 
    /// </summary> 
    public DateTime LastRenderTime { get; private set; } = DateTime.MinValue; 
 
    /// <summary> 
    /// Общее количество выполненных рендеров. 
    /// </summary> 
    public long TotalRenders { get; private set; } 
 
    /// <summary> 
    /// Количество пропущенных рендеров (из-за throttling). 
    /// </summary> 
    public long SkippedRenders { get; private set; } 
 
    public SgRenderGate(Func<Task> renderAction, TimeSpan minInterval) 
    { 
        _renderAction = renderAction ?? throw new ArgumentNullException(nameof(renderAction)); 
        _minInterval = minInterval; 
    } 
 
    /// <summary> 
    /// Запрашивает рендер. Если с последнего рендера прошло меньше minInterval — 
    /// рендер откладывается до истечения интервала. 
    /// </summary> 
    public async Task RequestRenderAsync() 
    { 
        if (_isDisposed) return; 
 
        var elapsedSinceLastRender = _stopwatch.Elapsed; 
        var canRenderImmediately = elapsedSinceLastRender >= _minInterval; 
 
        if (canRenderImmediately && !_isRenderScheduled) 
        { 
            await ExecuteRenderAsync(); 
            return; 
        } 
 
        // Планируем отложенный рендер 
        lock (_lock) 
        { 
            if (_isRenderScheduled) return; // Уже запланирован 
 
            SkippedRenders++; 
            _isRenderScheduled = true; 
 
            var delay = _minInterval - _stopwatch.Elapsed; 
            if (delay < TimeSpan.Zero) delay = TimeSpan.Zero; 
 
            _timer?.Dispose(); 
            _timer = new Timer(async _ => 
            { 
                _isRenderScheduled = false; 
                await ExecuteRenderAsync(); 
            }, null, delay, Timeout.InfiniteTimeSpan); 
        } 
    } 
 
    /// <summary> 
    /// Проверяет, можно ли выполнить рендер сейчас. 
    /// </summary> 
    public bool ShouldRender() 
    { 
        if (_isDisposed) return false; 
        return _stopwatch.Elapsed >= _minInterval; 
    } 
 
    private async Task ExecuteRenderAsync() 
    { 
        if (_isDisposed) return; 
 
        lock (_lock) 
        { 
            _stopwatch.Restart(); 
            _isRenderScheduled = false; 
        } 
 
        try 
        { 
            await _renderAction(); 
            LastRenderTime = DateTime.UtcNow; 
            TotalRenders++; 
        } 
        catch (Exception ex) 
        { 
            // Логируем но не пробрасываем — ошибка рендера не должна убивать компонент 
            Debug.WriteLine($"[SgRenderGate] Render error: {ex.Message}"); 
        } 
    } 
 
    /// <summary> 
    /// Принудительно выполняет рендер независимо от таймингов. 
    /// </summary> 
    public async Task ForceRenderAsync() 
    { 
        if (_isDisposed) return; 
        _isRenderScheduled = false; 
        await ExecuteRenderAsync(); 
    } 
 
    public void Dispose() 
    { 
        _isDisposed = true; 
        _timer?.Dispose(); 
        _timer = null; 
    } 
} 
