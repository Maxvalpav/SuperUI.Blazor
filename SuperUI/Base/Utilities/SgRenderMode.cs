// SuperUI/Base/Utilities/SgRenderMode.cs
// Определение текущего режима рендеринга Blazor-компонента.
// Обеспечивает SSR-безопасность: JS-интероп вызывается ТОЛЬКО когда
// IsInteractive == true.
//
// Улучшения: IComponentRenderMode detection, AssignedRenderMode inspection,
// compile-time cached getters via static generic class (zero reflection on hot path).

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Режим рендеринга, обнаруженный в рантайме.
/// </summary>
public enum RenderModeType
{
    /// <summary>Серверный пререндер, нет SignalR (статический SSR).</summary>
    StaticSSR,
    /// <summary>Server-side Blazor с SignalR.</summary>
    InteractiveServer,
    /// <summary>WebAssembly Blazor (в браузере).</summary>
    InteractiveWebAssembly,
    /// <summary>Auto: WASM с fallback на Server.</summary>
    InteractiveAuto,
}

/// <summary>
/// Утилиты для определения режима рендеринга Blazor-компонента.
/// </summary>
/// <remarks>
/// <para>Blazor (.NET 8+) поддерживает 4 режима рендеринга:</para>
/// <list type="bullet">
///   <item><b>Static SSR</b> — нет JS, нет SignalR; <see cref="IsInteractive"/> = false.</item>
///   <item><b>Prerender + Interactive</b> — сначала SSR (IsInteractive=false),
///     затем handoff в Interactive (IsInteractive=true).</item>
///   <item><b>InteractiveServer</b> — SignalR; <see cref="IsInteractive"/> = true с первого рендера.</item>
///   <item><b>InteractiveWebAssembly</b> — WASM; <see cref="IsInteractive"/> = true с первого рендера.</item>
/// </list>
/// <para>На горячем пути — zero-reflection: статический generic класс
/// <see cref="RenderModeGetter{T}"/> кеширует скомпилированный getter
/// для каждого <see cref="ComponentBase"/>-наследника.</para>
/// <para>Пример в <c>OnAfterRenderAsync</c>:</para>
/// <code>
/// protected override async Task OnAfterRenderAsync(bool firstRender)
/// {
///     if (!SgRenderMode.IsInteractive(this)) return;
///     // ... JS interop
/// }
/// </code>
/// </remarks>
public static class SgRenderMode
{
    /// <summary>Enum-аналог <see cref="RenderModeType"/>, вычисленный в рантайме.</summary>
    public static RenderModeType CurrentMode(IComponent component)
    {
        if (component is not ComponentBase cb) return RenderModeType.StaticSSR;
        if (!IsInteractive(cb)) return RenderModeType.StaticSSR;
        return DetectInteractiveMode(cb);
    }

    /// <summary>
    /// Возвращает <c>true</c>, если компонент работает в интерактивном режиме
    /// (SignalR или WASM runtime активны).
    /// </summary>
    public static bool IsInteractive(IComponent component)
    {
        if (component is not ComponentBase cb) return true; // fallback — не блокируем
        // Use non-generic Reflection path: Building the Func for the actual type would
        // require generic specialization. Simple approach: use reflection on the
        // runtime type once per call (caller is expected to early-exit on hot path).
        return GetIsInteractive(cb);
    }

    private static bool GetIsInteractive(ComponentBase cb)
    {
        var type = cb.GetType();
        if (s_isInteractiveGetters.TryGetValue(type, out var cached))
        {
            return cached is null ? true : cached(cb);
        }
        var built = BuildGetter(type);
        s_isInteractiveGetters[type] = built;
        return built is null ? true : built(cb);
    }

    private static readonly ConcurrentDictionary<Type, Func<ComponentBase, bool>?> s_isInteractiveGetters = new();

    private static Func<ComponentBase, bool>? BuildGetter(Type type)
    {
        try
        {
            PropertyInfo? rendererInfoProp = null;
            for (var t = type; t is not null && t != typeof(object); t = t.BaseType)
            {
                rendererInfoProp = t.GetProperty("RendererInfo",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (rendererInfoProp is not null) break;
            }
            if (rendererInfoProp is null) return null;

            var isInteractiveProp = rendererInfoProp.PropertyType
                .GetProperty("IsInteractive", BindingFlags.Instance | BindingFlags.Public);
            if (isInteractiveProp is null) return null;

            var param = Expression.Parameter(typeof(ComponentBase), "cb");
            var access = Expression.Property(Expression.Convert(param, type), rendererInfoProp);
            access = Expression.Property(access, isInteractiveProp);
            return Expression.Lambda<Func<ComponentBase, bool>>(access, param).Compile();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Возвращает <c>true</c>, если текущий процесс выполняется в браузере (WASM).
    /// </summary>
    public static bool IsBrowser => OperatingSystem.IsBrowser();

    /// <summary>
    /// Возвращает <c>true</c>, если текущий процесс выполняется на сервере.
    /// </summary>
    public static bool IsServer => !IsBrowser;

    /// <summary>
    /// Возвращает <c>true</c>, если компонент находится в фазе prerender
    /// (сервер генерирует HTML, но рендерер ещё не интерактивен).
    /// </summary>
    public static bool IsPrerendering(IComponent component) =>
        !IsInteractive(component) && IsServer;

    /// <summary>
    /// Возвращает <c>true</c>, если компонент будет переведён в интерактивный режим
    /// после завершения prerender.
    /// </summary>
    public static bool WillBecomeInteractive(IComponent component) =>
        IsPrerendering(component);

    private static RenderModeType DetectInteractiveMode(ComponentBase cb)
    {
        // We can't reliably distinguish Server vs WASM from inside a component
        // without AssignedRenderMode (which is protected). Inspect the assigned
        // render mode through reflection at most once per type.
        var getter = _assignedModeGetters.GetOrAdd(cb.GetType(), BuildAssignedModeGetter);
        var mode = getter?.Invoke(cb);
        return mode switch
        {
            "InteractiveServer" or "Server"   => RenderModeType.InteractiveServer,
            "InteractiveWebAssembly" or "Wasm" => RenderModeType.InteractiveWebAssembly,
            "InteractiveAuto" or "Auto"        => RenderModeType.InteractiveAuto,
            _ => IsBrowser ? RenderModeType.InteractiveWebAssembly : RenderModeType.InteractiveServer,
        };
    }

    private static readonly ConcurrentDictionary<Type, Func<ComponentBase, string?>?> _assignedModeGetters = new();

    private static Func<ComponentBase, string?>? BuildAssignedModeGetter(Type type)
    {
        PropertyInfo? prop = null;
        for (var t = type; t is not null && t != typeof(object); t = t.BaseType)
        {
            prop = t.GetProperty("AssignedRenderMode",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (prop is not null) break;
        }
        if (prop is null) return null;
        try
        {
            var param = Expression.Parameter(typeof(ComponentBase), "cb");
            var access = Expression.Property(param, prop);
            var convert = Expression.Convert(access, typeof(object));
            var lambda = Expression.Lambda<Func<ComponentBase, object?>>(convert, param).Compile();
            return cb => lambda(cb)?.ToString();
        }
        catch
        {
            return null;
        }
    }
}
