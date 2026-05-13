// SgRenderModeResolver.cs — Сервис разрешения RenderMode для InteractiveAuto и глобальных настроек 
 
using Microsoft.AspNetCore.Components; 
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options; 
 
namespace SuperUI.Base.Services; 
 
/// <summary> 
/// Разрешает конкретный RenderMode на основе конфигурации и окружения. 
/// Используется SgRenderModeDetector'ом. 
/// </summary> 
public class SgRenderModeResolver 
{ 
    private readonly IOptions<SgLibraryOptions> _options; 
    private readonly IServiceProvider _serviceProvider; 
    private static readonly Type? s_circuitHandlerType;

    static SgRenderModeResolver()
    {
        // Пытаемся найти тип CircuitHandler без жесткой зависимости
        try
        {
            s_circuitHandlerType = Type.GetType("Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler, Microsoft.AspNetCore.Components.Server");
        }
        catch { }
    }

    public SgRenderModeResolver( 
        IOptions<SgLibraryOptions> options, 
        IServiceProvider serviceProvider) 
    { 
        _options = options; 
        _serviceProvider = serviceProvider; 
    } 
 
    /// <summary> 
    /// Разрешает текущий RenderMode. 
    /// </summary> 
    public SgRenderMode ResolveRenderMode() 
    { 
        var options = _options.Value; 
 
        // Если явно указан режим в опциях — используем его 
        if (options.DefaultRenderMode != SgRenderMode.Unknown) 
            return options.DefaultRenderMode; 
 
        // Определяем по окружению 
        var isWasm = OperatingSystem.IsBrowser(); 
        var hasHttpContext = _serviceProvider.GetService<IHttpContextAccessor>()?.HttpContext != null; 
 
        if (isWasm) 
        { 
            return SgRenderMode.InteractiveWebAssembly; 
        } 
 
        if (hasHttpContext) 
        { 
            // Проверяем, установлен ли SignalR circuit через рефлексию
            if (s_circuitHandlerType != null)
            {
                var circuitHandler = _serviceProvider.GetService(s_circuitHandlerType); 
                if (circuitHandler != null) 
                    return SgRenderMode.InteractiveServer; 
            }
 
            return SgRenderMode.StaticSSR; 
        } 
 
        return SgRenderMode.InteractiveServer; // По умолчанию 
    } 
} 
 
/// <summary> 
/// Методы расширения для RenderHandle. 
/// </summary> 
public static class RenderHandleExtensions 
{ 
    private static readonly System.Reflection.FieldInfo? s_isRenderingInteractiveField; 
 
    static RenderHandleExtensions() 
    { 
        // Пытаемся получить доступ к внутреннему полю RenderHandle 
        try 
        { 
             s_isRenderingInteractiveField = typeof(RenderHandle) 
                 .GetField("_isRenderingInteractive", 
                     System.Reflection.BindingFlags.NonPublic | 
                     System.Reflection.BindingFlags.Instance); 
        } 
        catch 
        { 
            // В некоторых версиях может не работать 
        } 
    } 
 
    /// <summary> 
    /// Определяет, находится ли компонент в интерактивном режиме рендеринга. 
    /// </summary> 
    public static bool IsRenderingInteractive(this RenderHandle handle) 
    { 
        if (s_isRenderingInteractiveField != null) 
        { 
            try 
            { 
                return (bool)s_isRenderingInteractiveField.GetValue(handle)!; 
            } 
            catch 
            { 
                // Fallback 
            } 
        } 
 
        // Fallback: проверяем Dispatcher 
        try 
        { 
            var dispatcher = handle.Dispatcher; 
            return dispatcher != null; 
        } 
        catch 
        { 
            return false; 
        } 
    } 
} 
