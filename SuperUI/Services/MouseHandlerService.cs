// Файл: Services/MouseHandlerService.cs

using Microsoft.AspNetCore.Components.Web;
using SuperUI.Components.Base;

namespace SuperUI.Services;

/// <summary>
/// Централизованный сервис регистрации глобальных mouse событий.
/// Один JS listener на документ вместо N на каждый компонент.
/// </summary>
public interface IMouseHandlerService
{
    void Register(MouseEventRegistration registration);
    void Unregister(MouseEventRegistration registration);
}

public sealed class MouseHandlerService : IMouseHandlerService, IAsyncDisposable
{
    private readonly List<MouseEventRegistration> _registrations = new();
    private readonly object _lock = new();

    public void Register(MouseEventRegistration registration)
    {
        lock (_lock)
            _registrations.Add(registration);
    }

    public void Unregister(MouseEventRegistration registration)
    {
        lock (_lock)
            _registrations.Remove(registration);
    }

    /// <summary>
    /// Вызывается из JS при событии мыши.
    /// </summary>
    public async Task HandleMouseEventAsync(MouseEventType eventType, MouseEventArgs e)
    {
        MouseEventRegistration? handler = null;
        lock (_lock)
        {
            handler = _registrations.FirstOrDefault(r => r.EventType == eventType);
        }

        if (handler is not null)
            await handler.Handler(e);
    }

    public ValueTask DisposeAsync()
    {
        lock (_lock) _registrations.Clear();
        return ValueTask.CompletedTask;
    }
}
