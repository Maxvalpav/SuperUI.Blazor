// ─────────────────────────────────────────────────────────────────
// FILE: Base/AI/SgAIBase.cs
// ИННОВАЦИЯ: AI-assisted компоненты.
// Встроенная поддержка AI-генерации контента, auto-complete,
// streaming responses через IAsyncEnumerable.
// ─────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SuperUI.Utilities;

namespace SuperUI.Base.AI;

/// <summary>
/// Интерфейс AI-провайдера для компонентов.
/// Реализуется пользователем через OpenAI, Azure AI, Ollama...
/// </summary>
public interface IAIProvider
{
    IAsyncEnumerable<string> StreamAsync(string prompt, CancellationToken ct = default);
    Task<string> CompleteAsync(string prompt, CancellationToken ct = default);
    Task<IReadOnlyList<string>> SuggestAsync(string input, int count = 5, CancellationToken ct = default);
}

/// <summary>
/// Уровень 4 (инновация). Базовый класс для AI-assisted компонентов.
/// Поддерживает streaming через IAsyncEnumerable без блокировки UI.
/// </summary>
public abstract class SgAIBase : Components.Base.SgInteractiveBase
{
    [Microsoft.AspNetCore.Components.Inject] protected IAIProvider? AIProvider { get; private set; }

    protected string StreamingBuffer { get; private set; } = string.Empty;
    protected bool   IsStreaming     { get; private set; }

    /// <summary>
    /// Запускает AI-стриминг и обновляет компонент на каждый чанк.
    /// Используйте в кнопке "Генерировать".
    /// </summary>
    protected async Task StreamAIResponseAsync(string prompt)
    {
        if (AIProvider is null) return;

        IsStreaming    = true;
        StreamingBuffer = string.Empty;
        await RequestStateUpdateAsync();

        try
        {
            await foreach (var chunk in AIProvider.StreamAsync(prompt, _lifecycleToken.Current))
            {
                StreamingBuffer += chunk;
                // Throttle: обновляем UI не чаще чем раз в 50ms
                await ThrottleAsync(RequestStateUpdateAsync, 50, "ai-stream");
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsStreaming = false;
            await RequestStateUpdateAsync();
        }
    }

    // Helper for throttling render updates during streaming
    private readonly Dictionary<string, SgThrottler> _throttlers = new();
    
    private ValueTask ThrottleAsync(Func<Task> action, int intervalMs, string key)
    {
        if (!_throttlers.TryGetValue(key, out var throttler))
        {
            throttler = new SgThrottler(intervalMs);
            _throttlers[key] = throttler;
        }

        if (throttler.Throttle(() => new ValueTask(action())))
            return ValueTask.CompletedTask;
        
        return ValueTask.CompletedTask;
    }

    protected override async ValueTask OnComponentDisposeAsync()
    {
        foreach (var t in _throttlers.Values)
            t.Dispose();
        _throttlers.Clear();
        
        await base.OnComponentDisposeAsync();
    }
}
