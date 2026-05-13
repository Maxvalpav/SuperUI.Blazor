// SuperUI/Base/Hooks/StructuredLifecycleHook.cs
// ИСПРАВЛЕНО:
// 1. Stopwatch заменён на Stopwatch.GetTimestamp() + long (thread-safe)
// 2. Interlocked для _initStart
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SuperUI.Base;
using SuperUI.Base.Hooks;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Хук для структурированного логирования жизненного цикла компонента.
/// Логирует Initialize и FirstRender с временными метками.
/// </summary>
public sealed class StructuredLifecycleHook : IComponentHook, IRenderHook
{
    private readonly ILogger _logger;
    // ИСПРАВЛЕНО: timestamp вместо Stopwatch (thread-safe)
    private long _initStart;

    public StructuredLifecycleHook(ILogger logger) => _logger = logger;

    // IComponentHook
    public void OnInitialized(SgComponentBase c)
    {
        Interlocked.Exchange(ref _initStart, Stopwatch.GetTimestamp());
        _logger.LogDebug(
            "Component {Type} {Id} initialized",
            c.GetType().Name, c.ComponentId);
    }

    public void OnParametersSet(SgComponentBase c) { }
    public void OnAfterRender(SgComponentBase c, bool firstRender) { }

    // IAsyncComponentHook
    public Task OnInitializedAsync(SgComponentBase c) => Task.CompletedTask;
    public Task OnParametersSetAsync(SgComponentBase c) => Task.CompletedTask;

    public Task OnAfterRenderAsync(SgComponentBase c, bool firstRender)
    {
        if (firstRender)
        {
            var start = Interlocked.Read(ref _initStart);
            var elapsed = start > 0
                ? Stopwatch.GetElapsedTime(start).TotalMilliseconds
                : 0;

            _logger.LogDebug(
                "Component {Type} {Id} first render: {Ms:F1}ms",
                c.GetType().Name, c.ComponentId, elapsed);
        }
        return Task.CompletedTask;
    }

    // IRenderHook
    public bool ShouldRender(SgComponentBase c) => true;
}