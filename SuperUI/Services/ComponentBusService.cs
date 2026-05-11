// Файл: Services/ComponentBusService.cs
// ИННОВАЦИЯ: Type-safe message bus для компонентов (аналог MediatR для UI)

namespace SuperUI.Services;

/// <summary>
/// Type-safe шина сообщений для межкомпонентного взаимодействия.
/// 
/// ПРОБЛЕМА: EventCallback не работает между несвязанными компонентами.
/// CascadingValue слишком тяжёлый для простых уведомлений.
/// 
/// РЕШЕНИЕ: легковесный publish/subscribe.
/// 
/// ИННОВАЦИЯ: Нет ни у одной Blazor библиотеки как встроенная часть.
/// 
/// ИСПОЛЬЗОВАНИЕ:
/// // Подписчик:
/// Bus.Subscribe<ThemeChangedMessage>(OnThemeChanged);
/// 
/// // Издатель:
/// await Bus.PublishAsync(new ThemeChangedMessage("dark"));
/// </summary>
public interface IComponentBus
{
    IDisposable Subscribe<TMessage>(Func<TMessage, ValueTask> handler)
        where TMessage : class;
    ValueTask PublishAsync<TMessage>(TMessage message, CancellationToken ct = default)
        where TMessage : class;
}

public sealed class ComponentBus : IComponentBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly object _lock = new();

    public IDisposable Subscribe<TMessage>(Func<TMessage, ValueTask> handler)
        where TMessage : class
    {
        lock (_lock)
        {
            var type = typeof(TMessage);
            if (!_handlers.ContainsKey(type))
                _handlers[type] = new();
            _handlers[type].Add(handler);
        }

        return new Subscription(() =>
        {
            lock (_lock)
            {
                if (_handlers.TryGetValue(typeof(TMessage), out var list))
                    list.Remove(handler);
            }
        });
    }

    public async ValueTask PublishAsync<TMessage>(TMessage message, CancellationToken ct = default)
        where TMessage : class
    {
        List<Delegate>? handlers;
        lock (_lock)
        {
            if (!_handlers.TryGetValue(typeof(TMessage), out handlers))
                return;
            handlers = new List<Delegate>(handlers); // копия для thread-safety
        }

        foreach (var handler in handlers)
        {
            ct.ThrowIfCancellationRequested();
            if (handler is Func<TMessage, ValueTask> typed)
                await typed(message);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly Action _unsubscribe;
        public Subscription(Action unsubscribe) => _unsubscribe = unsubscribe;
        public void Dispose() => _unsubscribe();
    }
}

// Встроенные сообщения SuperUI:
public sealed record ThemeChangedMessage(string ThemeName);
public sealed record CultureChangedMessage(System.Globalization.CultureInfo Culture);
public sealed record RtlChangedMessage(bool IsRtl);
public sealed record ComponentFocusedMessage(string ComponentId);
