// SuperUI/Base/Services/SgEnhancedNavigationService.cs
// НОВЫЙ: Enhanced Navigation Service (.NET 8+)
//
// Позволяет компонентам подписываться на события навигации
// без полной перезагрузки страницы (enhanced navigation).

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис для работы с Enhanced Navigation (.NET 8+).
/// Позволяет компонентам подписываться на события навигации
/// без полной перезагрузки страницы.
/// </summary>
public interface IEnhancedNavigationService
{
    /// <summary>Подписаться на событие "начало навигации".</summary>
    IDisposable OnNavigating(Func<LocationChangingContext, Task> handler);

    /// <summary>Подписаться на событие "навигация завершена".</summary>
    IDisposable OnNavigated(Action<LocationChangedEventArgs> handler);

    /// <summary>Включена ли Enhanced Navigation.</summary>
    bool IsEnhancedNavigationEnabled { get; }

    /// <summary>
    /// Обновить страницу (аналог NavigationManager.Refresh для enhanced navigation).
    /// </summary>
    Task RefreshAsync(bool forceReload = false);
}

public sealed class SgEnhancedNavigationService : IEnhancedNavigationService, IDisposable
{
    private readonly NavigationManager _navigationManager;
    private readonly List<Func<LocationChangingContext, Task>> _navigatingHandlers = new();
    private readonly List<Action<LocationChangedEventArgs>> _navigatedHandlers = new();
    private IDisposable? _locationChangingSubscription;
    private IDisposable? _locationChangedSubscription;

    public bool IsEnhancedNavigationEnabled { get; private set; }

    public SgEnhancedNavigationService(NavigationManager navigationManager)
    {
        _navigationManager = navigationManager;

        // Регистрируемся на события NavigationManager (.NET 8+)
        try
        {
            _locationChangingSubscription =
                _navigationManager.RegisterLocationChangingHandler(OnLocationChanging);
            IsEnhancedNavigationEnabled = true;
        }
        catch (InvalidOperationException)
        {
            // Enhanced navigation не поддерживается (например, при SSR)
            IsEnhancedNavigationEnabled = false;
        }

        _navigationManager.LocationChanged += OnLocationChanged;
    }

    public IDisposable OnNavigating(Func<LocationChangingContext, Task> handler)
    {
        _navigatingHandlers.Add(handler);
        return new UnsubscribeDisposable(() => _navigatingHandlers.Remove(handler));
    }

    public IDisposable OnNavigated(Action<LocationChangedEventArgs> handler)
    {
        _navigatedHandlers.Add(handler);
        return new UnsubscribeDisposable(() => _navigatedHandlers.Remove(handler));
    }

    public Task RefreshAsync(bool forceReload = false)
    {
        _navigationManager.NavigateTo(_navigationManager.Uri, forceLoad: forceReload);
        return Task.CompletedTask;
    }

    private async Task OnLocationChanging(LocationChangingContext context)
    {
        foreach (var handler in _navigatingHandlers)
            await handler(context);
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        foreach (var handler in _navigatedHandlers)
            handler(e);
    }

    public void Dispose()
    {
        _navigationManager.LocationChanged -= OnLocationChanged;
        _locationChangingSubscription?.Dispose();
        _locationChangedSubscription?.Dispose();
    }

    private sealed class UnsubscribeDisposable : IDisposable
    {
        private readonly Action _unsubscribe;
        public UnsubscribeDisposable(Action unsubscribe) => _unsubscribe = unsubscribe;
        public void Dispose() => _unsubscribe();
    }
}