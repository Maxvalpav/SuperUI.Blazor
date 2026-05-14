// SuperUI/Base/Services/SgRouteGuard.cs — НОВЫЙ КЛАСС
// Аналог: Angular CanActivate / CanDeactivate guards
// Поддержка: .NET 8/9/10, InteractiveServer + WASM
// SSR: только на клиенте (NavigationManager.RegisterLocationChangingHandler)

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace SuperUI.Base.Services;

/// <summary>
/// Результат проверки маршрута.
/// </summary>
public sealed record RouteGuardResult(bool CanProceed, string? RedirectTo = null, string? Message = null)
{
    public static RouteGuardResult Allow() => new(true);
    public static RouteGuardResult Deny(string? message = null) => new(false, null, message);
    public static RouteGuardResult Redirect(string url) => new(false, url);
}

/// <summary>
/// Интерфейс для guard'ов (проверка перед активацией маршрута).
/// </summary>
public interface IRouteActivateGuard
{
    Task<RouteGuardResult> CanActivateAsync(string targetUrl, CancellationToken ct);
}

/// <summary>
/// Интерфейс для guard'ов (проверка перед уходом с маршрута — несохранённые данные).
/// </summary>
public interface IRouteDeactivateGuard
{
    Task<RouteGuardResult> CanDeactivateAsync(string currentUrl, string targetUrl, CancellationToken ct);
}

/// <summary>
/// Сервис для регистрации и выполнения route guards.
/// <para>
/// Регистрация: <c>builder.Services.AddScoped&lt;ISgRouteGuardService, SgRouteGuardService&gt;()</c>
/// </para>
/// <para>
/// Использование:
/// <code>
/// [Inject] ISgRouteGuardService GuardService { get; set; } = null!;
///
/// protected override void OnInitialized()
/// {
///     GuardService.AddActivateGuard(new AuthGuard(authService));
///     GuardService.AddDeactivateGuard(new UnsavedChangesGuard(() => HasUnsavedChanges));
/// }
/// </code>
/// </para>
/// </summary>
public interface ISgRouteGuardService : IDisposable
{
    void AddActivateGuard(IRouteActivateGuard guard);
    void RemoveActivateGuard(IRouteActivateGuard guard);
    void AddDeactivateGuard(IRouteDeactivateGuard guard);
    void RemoveDeactivateGuard(IRouteDeactivateGuard guard);
}

public sealed class SgRouteGuardService : ISgRouteGuardService
{
    private readonly NavigationManager _navigation;
    private readonly ISgConfirmService? _confirmService;
    private readonly List<IRouteActivateGuard> _activateGuards = [];
    private readonly List<IRouteDeactivateGuard> _deactivateGuards = [];
    private readonly IDisposable? _locationChangingHandler;

    public SgRouteGuardService(NavigationManager navigation, ISgConfirmService? confirmService = null)
    {
        _navigation = navigation;
        _confirmService = confirmService;

        // Регистрируем обработчик изменения маршрута (.NET 8+)
        _locationChangingHandler = navigation.RegisterLocationChangingHandler(OnLocationChangingAsync);
    }

    public void AddActivateGuard(IRouteActivateGuard guard) => _activateGuards.Add(guard);
    public void RemoveActivateGuard(IRouteActivateGuard guard) => _activateGuards.Remove(guard);
    public void AddDeactivateGuard(IRouteDeactivateGuard guard) => _deactivateGuards.Add(guard);
    public void RemoveDeactivateGuard(IRouteDeactivateGuard guard) => _deactivateGuards.Remove(guard);

    private async ValueTask OnLocationChangingAsync(LocationChangingContext context)
    {
        var currentUrl = _navigation.Uri;
        var targetUrl = context.TargetLocation;

        // Проверяем deactivate guards
        foreach (var guard in _deactivateGuards)
        {
            var result = await guard.CanDeactivateAsync(currentUrl, targetUrl, context.CancellationToken);
            if (!result.CanProceed)
            {
                context.PreventNavigation();
                return;
            }
        }

        // Проверяем activate guards
        foreach (var guard in _activateGuards)
        {
            var result = await guard.CanActivateAsync(targetUrl, context.CancellationToken);
            if (!result.CanProceed)
            {
                context.PreventNavigation();
                if (!string.IsNullOrEmpty(result.RedirectTo))
                    _navigation.NavigateTo(result.RedirectTo);
                return;
            }
        }
    }

    public void Dispose() => _locationChangingHandler?.Dispose();
}

/// <summary>
/// Guard для проверки несохранённых изменений.
/// </summary>
public sealed class UnsavedChangesGuard : IRouteDeactivateGuard
{
    private readonly Func<bool> _hasChanges;
    private readonly ISgConfirmService? _confirmService;
    private readonly string _message;

    public UnsavedChangesGuard(
        Func<bool> hasChanges,
        ISgConfirmService? confirmService = null,
        string message = "У вас есть несохранённые изменения. Покинуть страницу?")
    {
        _hasChanges = hasChanges;
        _confirmService = confirmService;
        _message = message;
    }

    public async Task<RouteGuardResult> CanDeactivateAsync(string currentUrl, string targetUrl, CancellationToken ct)
    {
        if (!_hasChanges()) return RouteGuardResult.Allow();

        if (_confirmService is not null)
        {
            var confirmed = await _confirmService.ConfirmAsync(_message);
            return confirmed ? RouteGuardResult.Allow() : RouteGuardResult.Deny();
        }

        return RouteGuardResult.Deny(_message);
    }
}
