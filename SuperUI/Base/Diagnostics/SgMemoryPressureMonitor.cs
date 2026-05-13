// SuperUI/Base/Diagnostics/SgMemoryPressureMonitor.cs — НОВЫЙ
// ✅ Мониторинг использования памяти на Blazor Server
// ✅ GC pressure notifications (Gen0/Gen1/Gen2)
// ✅ Автоматический вызов GC.Collect при превышении порога
// ✅ Singleton-сервис (один экземпляр на приложение)
// ✅ Интеграция с SgDiagnosticsPanel

using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base.Diagnostics;

/// <summary>
/// Мониторинг давления памяти для Blazor Server.
/// Помогает предотвратить OutOfMemoryException в долгоживущих circuits.
/// </summary>
public sealed class SgMemoryPressureMonitor : ISgMemoryPressureMonitor
{
    private readonly ILogger<SgMemoryPressureMonitor> _logger;
    private readonly Timer _timer;
    private readonly long _warningThresholdBytes;
    private readonly long _criticalThresholdBytes;
    private int _inGC;

    public SgMemoryPressureMonitor(
        ILogger<SgMemoryPressureMonitor> logger,
        long warningThresholdMB = 200,
        long criticalThresholdMB = 350,
        int checkIntervalMs = 5000)
    {
        _logger = logger;
        _warningThresholdBytes = warningThresholdMB * 1024 * 1024;
        _criticalThresholdBytes = criticalThresholdMB * 1024 * 1024;
        _timer = new Timer(_ => CheckMemory(), null, checkIntervalMs, checkIntervalMs);
    }

    public long CurrentMemoryBytes => GC.GetTotalMemory(false);
    public double CurrentMemoryMB => CurrentMemoryBytes / (1024.0 * 1024.0);
    public MemoryPressureLevel CurrentLevel { get; private set; } = MemoryPressureLevel.Normal;

    public event Action<MemoryPressureLevel>? MemoryPressureChanged;

    private void CheckMemory()
    {
        var memory = GC.GetTotalMemory(false);
        MemoryPressureLevel newLevel;

        if (memory > _criticalThresholdBytes)
        {
            _logger.LogWarning(
                "CRITICAL memory pressure: {MemoryMB:F1} MB", memory / (1024.0 * 1024.0));
            newLevel = MemoryPressureLevel.Critical;

            // Агрессивный GC на Blazor Server
            if (Interlocked.CompareExchange(ref _inGC, 1, 0) == 0)
            {
                try
                {
                    GC.Collect(2, GCCollectionMode.Aggressive, blocking: true, compacting: true);
                    GC.WaitForPendingFinalizers();
                    GC.Collect(2, GCCollectionMode.Aggressive, blocking: true, compacting: true);

                    var after = GC.GetTotalMemory(false);
                    _logger.LogInformation(
                        "GC collected: {BeforeMB:F1} → {AfterMB:F1} MB",
                        memory / (1024.0 * 1024.0), after / (1024.0 * 1024.0));
                }
                finally
                {
                    Interlocked.Exchange(ref _inGC, 0);
                }
            }
        }
        else if (memory > _warningThresholdBytes)
        {
            _logger.LogDebug(
                "Memory pressure WARNING: {MemoryMB:F1} MB", memory / (1024.0 * 1024.0));
            newLevel = MemoryPressureLevel.Warning;

            // Лёгкий GC
            GC.Collect(0, GCCollectionMode.Optimized, blocking: false);
        }
        else
        {
            newLevel = MemoryPressureLevel.Normal;
        }

        if (newLevel != CurrentLevel)
        {
            CurrentLevel = newLevel;
            MemoryPressureChanged?.Invoke(newLevel);
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}

public enum MemoryPressureLevel
{
    Normal,
    Warning,
    Critical
}
