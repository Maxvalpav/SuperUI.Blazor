// SuperUI/Base/Diagnostics/PerformanceBudget.cs
using System;
using System.Diagnostics;

namespace SuperUI.Base.Diagnostics;

/// <summary>
/// Defines performance budgets and monitors adherence.
/// Helps catch performance regressions in development and testing.
/// </summary>
public class PerformanceBudget
{
    /// <summary>Maximum render time per component (ms).</summary>
    public double MaxRenderTimeMs { get; init; } = 16.0; // ~60fps budget

    /// <summary>Maximum renders per second allowed for a component.</summary>
    public int MaxRendersPerSecond { get; init; } = 60;

    /// <summary>Maximum total components on screen at once.</summary>
    public int MaxConcurrentVisibleComponents { get; init; } = 500;

    /// <summary>Maximum memory estimate per component (bytes, rough).</summary>
    public long MaxMemoryPerComponentBytes { get; init; } = 50_000;

    /// <summary>Maximum JS interop calls per second.</summary>
    public int MaxJsInteropCallsPerSecond { get; init; } = 30;

    /// <summary>Maximum initial bundle size for WASM (bytes).</summary>
    public long MaxWasmBundleSizeBytes { get; init; } = 5_000_000; // 5 MB

    private readonly Stopwatch _windowTimer = Stopwatch.StartNew();
    private int _currentWindowRenderCount;
    private int _currentWindowJsInteropCount;
    private long _lastWindowStartTicks;

    /// <summary>Check if a render time exceeds the budget.</summary>
    public bool IsRenderTimeExceeded(double renderTimeMs, out string? warning)
    {
        if (renderTimeMs > MaxRenderTimeMs)
        {
            warning = $"Render time {renderTimeMs:F2}ms exceeds budget of {MaxRenderTimeMs}ms";
            return true;
        }
        warning = null;
        return false;
    }

    /// <summary>Track a render for rate limiting.</summary>
    public bool IsRenderRateExceeded(out string? warning)
    {
        var elapsed = _windowTimer.Elapsed;
        if (elapsed.TotalSeconds >= 1.0)
        {
            _currentWindowRenderCount = 0;
            _currentWindowJsInteropCount = 0;
            _windowTimer.Restart();
        }
        _currentWindowRenderCount++;
        if (_currentWindowRenderCount > MaxRendersPerSecond)
        {
            warning = $"Render rate {_currentWindowRenderCount}/s exceeds budget of {MaxRendersPerSecond}/s";
            return true;
        }
        warning = null;
        return false;
    }

    /// <summary>Track a JS interop call for rate limiting.</summary>
    public bool IsJsInteropRateExceeded(out string? warning)
    {
        if (_windowTimer.Elapsed.TotalSeconds >= 1.0)
        {
            _currentWindowJsInteropCount = 0;
        }
        _currentWindowJsInteropCount++;
        if (_currentWindowJsInteropCount > MaxJsInteropCallsPerSecond)
        {
            warning = $"JS interop rate {_currentWindowJsInteropCount}/s exceeds budget of {MaxJsInteropCallsPerSecond}/s";
            return true;
        }
        warning = null;
        return false;
    }

    /// <summary>Create a default budget suitable for WASM (stricter).</summary>
    public static PerformanceBudget CreateWasmBudget() => new()
    {
        MaxRenderTimeMs = 20.0,
        MaxRendersPerSecond = 30,
        MaxConcurrentVisibleComponents = 200,
        MaxJsInteropCallsPerSecond = 15,
        MaxMemoryPerComponentBytes = 30_000,
        MaxWasmBundleSizeBytes = 3_000_000
    };

    /// <summary>Create a default budget suitable for Server-side (more lenient).</summary>
    public static PerformanceBudget CreateServerBudget() => new()
    {
        MaxRenderTimeMs = 10.0,
        MaxRendersPerSecond = 120,
        MaxConcurrentVisibleComponents = 500,
        MaxJsInteropCallsPerSecond = 60,
        MaxMemoryPerComponentBytes = 50_000
    };
}
