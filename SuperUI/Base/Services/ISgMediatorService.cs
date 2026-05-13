// SuperUI/Base/Services/ISgMediatorService.cs
// ✅ NEW: типизированный EventAggregator (аналог MudBlazor MediatorService)
// ✅ Слабые ссылки — не удерживает компоненты от GC
// ✅ NET8+: потокобезопасен для Server и WASM

namespace SuperUI.Base.Services;

/// <summary>Маркерный интерфейс для сообщений медиатора.</summary>
public interface ISgMessage { }

/// <summary>
/// Типизированный EventAggregator для слабосвязанного общения между компонентами.
/// </summary>
public interface ISgMediatorService
{
    /// <summary>Подписаться на сообщение типа TMessage.</summary>
    IDisposable Subscribe<TMessage>(Action<TMessage> handler) where TMessage : ISgMessage;

    /// <summary>Подписаться асинхронно.</summary>
    IDisposable Subscribe<TMessage>(Func<TMessage, Task> handler) where TMessage : ISgMessage;

    /// <summary>Опубликовать сообщение всем подписчикам.</summary>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : ISgMessage;

    /// <summary>Опубликовать синхронно (для простых случаев).</summary>
    void Publish<TMessage>(TMessage message) where TMessage : ISgMessage;
}
