// SuperUI/Base/Services/ComponentOptionsService.cs
//
// ИСПРАВЛЕНИЯ (CS0101, CS0535):
// 1. CS0101 FIX: Все типы — в ОДНОМ файле, папка Options/IComponentOptionsService.cs УДАЛЕНА.
// 2. CS0535 FIX: NullComponentOptionsService реализует ВСЕ члены IComponentOptionsService.
// 3. Единый дизайн: конкретные свойства (Button/Input/DataGrid/Overlay/Toast) +
//    generic-расширения через extension methods.
// 4. WASM/Server safe: ComponentOptionsService — Singleton, thread-safe через readonly.
// 5. NullComponentOptionsService — Null Object Pattern с defaults (не выбрасывает исключений).
//
// Thread safety:
// - ComponentOptionsService: readonly поля → полностью thread-safe (WASM + Server).
// - NullComponentOptionsService: stateless → полностью thread-safe.

using Microsoft.Extensions.Options;

namespace SuperUI.Base.Services;

// ═══════════════════════════════════════════════════════════════════════════
// Опции отдельных компонентов
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Настройки компонента SgButton по умолчанию.</summary>
public sealed record SgButtonOptions
{
    public Base.SgVariant DefaultVariant { get; init; } = Base.SgVariant.Primary;
    public Base.SgSize    DefaultSize    { get; init; } = Base.SgSize.Medium;
    public bool           ShowRipple     { get; init; } = true;
}

/// <summary>Настройки компонентов ввода (SgTextBox, SgNumberEdit и др.) по умолчанию.</summary>
public sealed record SgInputOptions
{
    public int                  DefaultDebounceMs { get; init; } = 300;
    public bool                 ShowClearButton   { get; init; } = true;
    public Base.SgInputVariant  DefaultVariant    { get; init; } = Base.SgInputVariant.Outlined;
}

/// <summary>Настройки SgDataGrid по умолчанию.</summary>
public sealed record SgDataGridOptions
{
    public int  DefaultPageSize      { get; init; } = 25;
    public bool DefaultVirtualization { get; init; } = true;
    public bool DefaultShowSearch     { get; init; } = false;
}

/// <summary>Настройки overlay-компонентов (Modal, Drawer, Popover) по умолчанию.</summary>
public sealed record SgOverlayOptions
{
    public int  DefaultAnimationMs    { get; init; } = 300;
    public bool DefaultCloseOnEscape  { get; init; } = true;
    public bool DefaultTrapFocus      { get; init; } = true;
}

/// <summary>Настройки SgToast по умолчанию.</summary>
public sealed record SgToastOptions
{
    public int              DefaultDurationMs { get; init; } = 4000;
    public Base.SgPlacement DefaultPlacement  { get; init; } = Base.SgPlacement.TopRight;
}

// ═══════════════════════════════════════════════════════════════════════════
// Корневые опции библиотеки (передаются через IOptions<SgLibraryOptions>)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Корневые опции SuperUI — передаются в DI через AddSuperUI(options => ...).
/// </summary>
public sealed record SgLibraryOptions
{
    public SgButtonOptions?  Button  { get; init; }
    public SgInputOptions?   Input   { get; init; }
    public SgDataGridOptions? DataGrid { get; init; }
    public SgOverlayOptions? Overlay { get; init; }
    public SgToastOptions?   Toast   { get; init; }
    
    /// <summary>Тема по умолчанию: "light" | "dark" | "auto".</summary>
    public string DefaultTheme { get; init; } = "auto";
    
    /// <summary>Культура по умолчанию: "en-US" | "ru-RU".</summary>
    public string DefaultCulture { get; init; } = "en-US";
    
    /// <summary>Длительность toast-уведомлений в мс.</summary>
    public int DefaultToastDurationMs { get; init; } = 4000;
}

// ═══════════════════════════════════════════════════════════════════════════
// Интерфейс сервиса настроек
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Централизованные настройки компонентов SuperUI.
/// Позволяет задать defaults для всей библиотеки через DI.
/// </summary>
/// <remarks>
/// ДИЗАЙН: конкретные свойства (не generic) — для полного IntelliSense.
/// Generic-доступ реализован через extension-методы <see cref="ComponentOptionsServiceExtensions"/>.
/// </remarks>
public interface IComponentOptionsService
{
    /// <summary>Настройки SgButton.</summary>
    SgButtonOptions Button { get; }

    /// <summary>Настройки компонентов ввода.</summary>
    SgInputOptions Input { get; }

    /// <summary>Настройки SgDataGrid.</summary>
    SgDataGridOptions DataGrid { get; }

    /// <summary>Настройки overlay-компонентов.</summary>
    SgOverlayOptions Overlay { get; }

    /// <summary>Настройки SgToast.</summary>
    SgToastOptions Toast { get; }
}

// ═══════════════════════════════════════════════════════════════════════════
// Реализация — читает из IOptions<SgLibraryOptions>
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Реализация <see cref="IComponentOptionsService"/> через DI (<see cref="IOptions{TOptions}"/>).
/// </summary>
/// <remarks>
/// Singleton: все поля readonly → полностью thread-safe на Blazor Server и WASM.
/// </remarks>
public sealed class ComponentOptionsService : IComponentOptionsService
{
    /// <inheritdoc />
    public SgButtonOptions  Button  { get; }

    /// <inheritdoc />
    public SgInputOptions   Input   { get; }

    /// <inheritdoc />
    public SgDataGridOptions DataGrid { get; }

    /// <inheritdoc />
    public SgOverlayOptions Overlay { get; }

    /// <inheritdoc />
    public SgToastOptions   Toast   { get; }

    public ComponentOptionsService(IOptions<SgLibraryOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var o = options.Value;
        Button   = o.Button   ?? new();
        Input    = o.Input    ?? new();
        DataGrid = o.DataGrid ?? new();
        Overlay  = o.Overlay  ?? new();
        Toast    = o.Toast    ?? new();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Null Object — fallback если DI не зарегистрировал ComponentOptionsService
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Null-реализация <see cref="IComponentOptionsService"/>.
/// Возвращает defaults для всех свойств.
/// Используется как fallback если DI не настроен (тестирование, изолированные компоненты).
/// </summary>
/// <remarks>
/// CS0535 FIX: реализует ВСЕ члены интерфейса, включая Button/Input/DataGrid/Overlay/Toast.
/// Stateless → полностью thread-safe.
/// </remarks>
public sealed class NullComponentOptionsService : IComponentOptionsService
{
    /// <summary>Singleton-экземпляр (stateless — безопасно использовать везде).</summary>
    public static readonly NullComponentOptionsService Instance = new();

    // CS0535 FIX: реализованы все 5 свойств интерфейса
    /// <inheritdoc />
    public SgButtonOptions  Button  { get; } = new();

    /// <inheritdoc />
    public SgInputOptions   Input   { get; } = new();

    /// <inheritdoc />
    public SgDataGridOptions DataGrid { get; } = new();

    /// <inheritdoc />
    public SgOverlayOptions Overlay { get; } = new();

    /// <inheritdoc />
    public SgToastOptions   Toast   { get; } = new();

    private NullComponentOptionsService() { }
}

// ═══════════════════════════════════════════════════════════════════════════
// Extension methods для generic-доступа (обратная совместимость)
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Расширения для <see cref="IComponentOptionsService"/> — generic-доступ к опциям.
/// Позволяет хранить кастомные опции для сторонних компонентов без изменения интерфейса.
/// </summary>
public static class ComponentOptionsServiceExtensions
{
    // Кастомные опции хранятся отдельно, per-service-instance
    // (через ConditionalWeakTable → нет утечек памяти)
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<
        IComponentOptionsService,
        System.Collections.Concurrent.ConcurrentDictionary<Type, object>
    > _customOptions = new();

    /// <summary>
    /// Получить кастомные опции для компонента. Возвращает null если не зарегистрированы.
    /// </summary>
    public static TOptions? GetOptions<TComponent, TOptions>(
        this IComponentOptionsService service)
        where TOptions : class
    {
        if (!_customOptions.TryGetValue(service, out var dict)) return null;
        var key = typeof((TComponent, TOptions));
        return dict.TryGetValue(key, out var val) ? (TOptions)val : null;
    }

    /// <summary>
    /// Получить кастомные опции или создать default через фабрику.
    /// </summary>
    public static TOptions GetOrDefault<TComponent, TOptions>(
        this IComponentOptionsService service,
        Func<TOptions> defaultFactory)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(defaultFactory);
        var dict = _customOptions.GetOrCreateValue(service);
        var key  = typeof((TComponent, TOptions));
        return (TOptions)dict.GetOrAdd(key, _ => defaultFactory());
    }

    /// <summary>
    /// Зарегистрировать кастомные опции для компонента.
    /// </summary>
    public static IComponentOptionsService Register<TComponent, TOptions>(
        this IComponentOptionsService service,
        TOptions options)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(options);
        var dict = _customOptions.GetOrCreateValue(service);
        dict[typeof((TComponent, TOptions))] = options;
        return service;
    }
}
