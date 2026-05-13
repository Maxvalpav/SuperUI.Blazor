// SuperUI/Base/Reactive/SgSignalRouter.cs
// УНИКАЛЬНЫЙ КЛАСС — реактивный роутер на сигналах.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Реактивный роутер, управляемый сигналами.
/// Автоматически синхронизирует URL ↔ SgSignal.
/// Поддержка: .NET 8/9/10, InteractiveServer + WASM.
/// 
/// Использование:
/// <code>
/// var router = new SgSignalRouter(navigationManager, "page", "home");
/// router.Route("/home", "home")
///       .Route("/about", "about")
///       .Route("/users/{id:int}", "user-detail");
/// 
/// // В рендере:
/// @switch (router.CurrentRoute)
/// { ... }
/// </code>
/// </summary>
public sealed class SgSignalRouter : IDisposable
{
    private readonly NavigationManager _nav;
    private readonly SgSignal<string> _currentRoute;
    private readonly SgSignal<Dictionary<string, object>?> _routeParams;
    private readonly Dictionary<string, (string Name, string? Pattern)> _routes = new();
    private readonly List<IDisposable> _subscriptions = new();

    public IReadOnlySignal<string> CurrentRoute => _currentRoute;
    public IReadOnlySignal<Dictionary<string, object>?> RouteParams => _routeParams;
    public string CurrentUrl => _nav.Uri;
    public string CurrentPath => new Uri(_nav.Uri).AbsolutePath;

    public SgSignalRouter(
        NavigationManager navigationManager,
        string signalDebugName = "route",
        string defaultRoute = "/")
    {
        _nav = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        _currentRoute = new SgSignal<string>(defaultRoute, $"{signalDebugName}-name");
        _routeParams = new SgSignal<Dictionary<string, object>?>(null, $"{signalDebugName}-params");
        _nav.LocationChanged += OnLocationChanged;
    }

    /// <summary>Зарегистрировать маршрут.</summary>
    public SgSignalRouter Route(string pattern, string name)
    {
        _routes[name] = (name, pattern);
        return this;
    }

    /// <summary>Навигация по имени маршрута.</summary>
    public void NavigateTo(string routeName, object? parameters = null)
    {
        if (!_routes.TryGetValue(routeName, out var route))
            throw new InvalidOperationException($"Route '{routeName}' not found.");

        var url = route.Pattern ?? "/";

        // Замена параметров
        if (parameters is not null)
        {
            foreach (var prop in parameters.GetType().GetProperties())
            {
                var value = prop.GetValue(parameters)?.ToString() ?? "";
                url = url.Replace($"{{{prop.Name}}}", value)
                    .Replace($"{{{prop.Name}:int}}", value)
                    .Replace($"{{{prop.Name}:guid}}", value)
                    .Replace($"{{{prop.Name}:string}}", value);
            }
        }

        _nav.NavigateTo(url);
        _currentRoute.Set(routeName);
    }

    /// <summary>Навигация по URL.</summary>
    public void NavigateToUrl(string url)
    {
        _nav.NavigateTo(url);
    }

    /// <summary>Сопоставить текущий URL с именем маршрута.</summary>
    public string? MatchRoute(string path)
    {
        foreach (var (name, (_, pattern)) in _routes)
        {
            if (MatchPattern(path, pattern, out var parameters))
            {
                _routeParams.Set(parameters);
                return name;
            }
        }

        _routeParams.Set(null);
        return null;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        var path = new Uri(e.Location).AbsolutePath;
        var matched = MatchRoute(path);
        if (matched is not null)
            _currentRoute.Set(matched);
    }

    private static bool MatchPattern(
        string path,
        string? pattern,
        out Dictionary<string, object>? parameters)
    {
        parameters = null;
        if (string.IsNullOrEmpty(pattern)) return false;

        var patternParts = pattern.Trim('/').Split('/');
        var pathParts = path.Trim('/').Split('/');

        if (patternParts.Length != pathParts.Length) return false;

        parameters = new Dictionary<string, object>();

        for (int i = 0; i < patternParts.Length; i++)
        {
            var pp = patternParts[i];
            var ppath = pathParts[i];

            if (pp.StartsWith('{') && pp.EndsWith('}'))
            {
                // Параметр
                var paramDef = pp.Trim('{', '}');
                var colonIdx = paramDef.IndexOf(':');
                var paramName = colonIdx > 0 ? paramDef.Substring(0, colonIdx) : paramDef;
                var paramType = colonIdx > 0 ? paramDef.Substring(colonIdx + 1) : "string";

                parameters[paramName] = paramType switch
                {
                    "int" => int.TryParse(ppath, out var iv) ? iv : 0,
                    "guid" => Guid.TryParse(ppath, out var gv) ? gv : Guid.Empty,
                    "long" => long.TryParse(ppath, out var lv) ? lv : 0L,
                    "bool" => bool.TryParse(ppath, out var bv) && bv,
                    _ => ppath
                };
            }
            else if (!string.Equals(pp, ppath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    public void Dispose()
    {
        _nav.LocationChanged -= OnLocationChanged;
        _currentRoute.Dispose();
        _routeParams.Dispose();
        foreach (var s in _subscriptions) s.Dispose();
        _subscriptions.Clear();
    }
}
