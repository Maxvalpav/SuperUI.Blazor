// SuperUI/Base/Utilities/SgRenderMode.cs
// Определение текущего режима рендеринга Blazor-компонента.
// Обеспечивает SSR-безопасность: JS-интероп вызывается ТОЛЬКО когда
// IsInteractive == true.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;

namespace SuperUI.Base.Utilities;

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
/// <para>Используйте совместно с <c>[StreamRendering(true)]</c> и
/// <c>PersistentComponentState</c> для Streaming Rendering.</para>
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
    // Кешируем делегат доступа к RendererInfo по типу компонента.
    // ConditionalWeakTable<Type, ...> — weak keys по типу НЕ нужны
    // (Type не собирается GC в типичных сценариях), используем ConcurrentDictionary.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Func<ComponentBase, bool>>
        _isInteractiveCache = new();

    /// <summary>
    /// Возвращает <c>true</c>, если компонент работает в интерактивном режиме
    /// (SignalR или WASM runtime активны).
    /// </summary>
    /// <param name="component">Экземпляр компонента (<c>this</c>).</param>
    public static bool IsInteractive(IComponent component)
    {
        if (component is not ComponentBase cb) return true; // fallback — не блокируем

        var getter = _isInteractiveCache.GetOrAdd(cb.GetType(), BuildGetter);
        return getter(cb);
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
    /// после завершения prerender (т.е. имеет атрибут <c>@rendermode</c>).
    /// </summary>
    /// <remarks>
    /// Текущая реализация эквивалентна <c>IsPrerendering</c>.
    /// В будущих версиях .NET может быть уточнена через RendererInfo.
    /// </remarks>
    public static bool WillBecomeInteractive(IComponent component) =>
        IsPrerendering(component);

    // Строим делегат через Reflection один раз на тип.
    // На .NET 8/9/10: ComponentBase.RendererInfo — protected свойство.
    // Используем Expression tree для производительного доступа.
    private static Func<ComponentBase, bool> BuildGetter(Type componentType)
    {
        // Ищем RendererInfo в иерархии (protected, поэтому через Reflection)
        var rendererInfoProp = typeof(ComponentBase)
            .GetProperty("RendererInfo",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);

        if (rendererInfoProp is null)
        {
            // .NET версия без RendererInfo — fallback true
            return _ => true;
        }

        var isInteractiveProp = rendererInfoProp.PropertyType
            .GetProperty("IsInteractive",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public);

        if (isInteractiveProp is null)
        {
            return _ => true;
        }

        // Строим lambda: (ComponentBase cb) => cb.RendererInfo.IsInteractive
        var param = System.Linq.Expressions.Expression.Parameter(typeof(ComponentBase), "cb");
        var rendererInfoAccess = System.Linq.Expressions.Expression.Property(param, rendererInfoProp);
        var isInteractiveAccess = System.Linq.Expressions.Expression.Property(rendererInfoAccess, isInteractiveProp);
        var lambda = System.Linq.Expressions.Expression.Lambda<Func<ComponentBase, bool>>(
            isInteractiveAccess, param);

        return lambda.Compile();
    }
}