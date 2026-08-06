// SuperUI/Base/ComponentBases/SgComponentBase.cs
// Базовый класс для всех компонентов SuperUI. Точка входа для:
//   * Id generation / CssBuilder / StyleBuilder helpers
//   * Theme/Locale change subscription (auto re-render)
//   * Scoped logger (с именем типа)
//   * CancellationToken, отменяемый при Dispose
//   * Bunit-friendly: никаких обращений к IJSRuntime/Js-Interop здесь
//
// Поддерживает WASM и Server-Side Blazor (.NET 8/9/10). SSR-безопасен.

using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SuperUI.Base.Builders;
using SuperUI.Base.Utilities;
using SuperUI.Localization;
using SuperUI.Services;
using SuperUI.Themes;

namespace SuperUI.Base.ComponentBases;

/// <summary>
/// Базовый класс для ВСЕХ компонентов SuperUI. По умолчанию подписывается на
/// изменения темы и локали и форсирует <see cref="ComponentBase.StateHasChanged"/>.
/// </summary>
/// <remarks>
/// <para>Все компоненты SuperUI наследуют от <c>SgComponentBase</c> напрямую или
/// через <c>SgJsComponentBase</c> / <c>SgOverlayComponentBase</c>.</para>
/// <para>Lifecycle:</para>
/// <list type="number">
///   <item>Конструктор — derived classes инициализируют параметры и state.</item>
///   <item><c>OnInitialized</c> — подписки на сервисы, ленивая загрузка данных.</item>
///   <item>Каждый рендер — <c>Css()</c> / <c>Styles()</c> / <c>ResolvedId</c>.</item>
///   <item><c>Dispose</c> — отписки + отмена <see cref="ComponentLifetime"/>.</item>
/// </list>
/// </remarks>
public abstract class SgComponentBase : ComponentBase, IDisposable, IAsyncDisposable
{
    private string? _autoId;
    private ILogger? _logger;
    private Action<IThemeDefinition, string>? _themeChangedHandler;
    private Action? _localeChangedHandler;
    private CancellationTokenSource? _lifetimeCts;

    /// <summary>CSS class parameter (Blazor-attribute).</summary>
    [Parameter] public string? CssClass { get; set; }
    /// <summary>Inline style parameter (Blazor-attribute).</summary>
    [Parameter] public string? Style { get; set; }
    /// <summary>Optional explicit id. If not set, <see cref="ResolvedId"/> uses an auto-generated stable id.</summary>
    [Parameter] public string? Id { get; set; }
    /// <summary>All additional HTML attributes splat.</summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Hosting environment context (theme tag, prefers-reduced-motion). Cascaded from layout.</summary>
    [CascadingParameter] protected HostEnvironmentContext? Host { get; set; }

    [Inject] protected ILoggerFactory? LoggerFactory { get; set; }
    [Inject] protected SgThemeService ThemeService { get; set; } = default!;
    [Inject] protected ISuperUILocalizer Localizer { get; set; } = default!;

    /// <summary>Current theme mode: <c>"light"</c> | <c>"dark"</c> | <c>"auto"</c>.</summary>
    protected string CurrentMode => ThemeService.CurrentMode;
    /// <summary>True, если в данный момент активна тёмная тема.</summary>
    protected bool IsDark => ThemeService.IsDark;
    /// <summary>Текущая тема.</summary>
    protected IThemeDefinition CurrentTheme => ThemeService.CurrentTheme;

    /// <summary>Root element reference (set in the razor template via <c>@ref="RootRef"</c>).</summary>
    protected ElementReference RootRef;

    /// <summary>
    /// Resolved id: explicit <see cref="Id"/> if set, else auto-generated.
    /// Auto-id is stable per instance and uses <see cref="IdPrefix"/>.
    /// </summary>
    protected string ResolvedId => Id ?? (_autoId ??= SgIdGenerator.StableIdFor(this, IdPrefix));

    /// <summary>Prefix used for auto-generated ids. Override to specialize (e.g. "sg-modal").</summary>
    protected virtual string IdPrefix => "sg";

    /// <summary>
    /// Cancellation token that is cancelled on <see cref="DisposeAsync"/>.
    /// Use for any in-flight async work (debouncer, fetch, JS callback).
    /// </summary>
    protected CancellationToken ComponentLifetime
    {
        get
        {
            _lifetimeCts ??= new CancellationTokenSource();
            return _lifetimeCts.Token;
        }
    }

    /// <summary>Component-scoped logger. Cheap (lazy) and disposed with the component.</summary>
    protected ILogger Logger => _logger ??= LoggerFactory?.CreateLogger(GetType()) ?? NullLogger.Instance;

    /// <summary>True once <see cref="Dispose()"/> or <see cref="DisposeAsync"/> has run.</summary>
    protected bool IsDisposed => _disposed;

    /// <summary>Starts a CssBuilder chain with the given root class and merges <see cref="CssClass"/> + <see cref="AdditionalAttributes"/>.</summary>
    protected CssBuilder Css(string? rootClass = null) =>
        CssBuilder.Default(rootClass)
            .AddClass(CssClass)
            .AddClassFromAttributes(AdditionalAttributes);

    /// <summary>Starts a StyleBuilder chain with <see cref="Style"/> + <see cref="AdditionalAttributes"/>.</summary>
    protected StyleBuilder Styles() =>
        StyleBuilder.Default(Style)
            .AddStyleFromAttributes(AdditionalAttributes);

    /// <summary>
    /// Returns <see cref="AdditionalAttributes"/> minus <c>class</c> and <c>style</c>
    /// (use as <c>@attributes="AttributesWithoutClassAndStyle"</c> in razor).
    /// </summary>
    protected Dictionary<string, object>? AttributesWithoutClassAndStyle
    {
        get
        {
            if (AdditionalAttributes is null) return null;
            if (!AdditionalAttributes.ContainsKey("class") && !AdditionalAttributes.ContainsKey("style"))
                return AdditionalAttributes;
            var dict = new Dictionary<string, object>(AdditionalAttributes.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in AdditionalAttributes)
            {
                if (kv.Key.Equals("class", StringComparison.OrdinalIgnoreCase) ||
                    kv.Key.Equals("style", StringComparison.OrdinalIgnoreCase)) continue;
                dict[kv.Key] = kv.Value;
            }
            return dict;
        }
    }

    /// <summary>Combines multiple tokens with space (skip null/whitespace).</summary>
    protected static string CombineCss(params string?[] tokens) =>
        string.Join(" ", tokens.Where(t => !string.IsNullOrWhiteSpace(t))!);

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        SubscribeToTheme();
        SubscribeToLocale();
    }

    private void SubscribeToTheme()
    {
        _themeChangedHandler = (_, _) =>
        {
            if (Volatile.Read(ref _disposed)) return;
            try { InvokeAsync(StateHasChanged); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
            catch (TaskCanceledException) { }
        };
        ThemeService.ThemeChanged += _themeChangedHandler;
    }

    private void SubscribeToLocale()
    {
        _localeChangedHandler = () =>
        {
            if (Volatile.Read(ref _disposed)) return;
            try { InvokeAsync(StateHasChanged); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
            catch (TaskCanceledException) { }
        };
        Localizer.OnLocaleChanged += _localeChangedHandler;
    }

    /// <summary>True, если компонент уже Dispose()нут. Доступен наследникам.</summary>
    protected bool Disposed => _disposed;

    /// <summary>Поле состояния Dispose. Доступно наследникам (для упрощения миграции).</summary>
    protected bool _disposed;

    /// <inheritdoc/>
    public virtual void Dispose()
    {
        if (_disposed) return;
        Volatile.Write(ref _disposed, true);

        if (ThemeService is not null && _themeChangedHandler is not null)
            ThemeService.ThemeChanged -= _themeChangedHandler;
        if (Localizer is not null && _localeChangedHandler is not null)
            Localizer.OnLocaleChanged -= _localeChangedHandler;

        try { _lifetimeCts?.Cancel(); } catch (ObjectDisposedException) { }
        _lifetimeCts?.Dispose();
        _lifetimeCts = null;
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    public virtual ValueTask DisposeAsync()
    {
        Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

/// <summary>Cascading host environment context (theme tag, prefers-reduced-motion flag).</summary>
public sealed class HostEnvironmentContext
{
    /// <summary>Active theme tag (e.g. "default", "neo").</summary>
    public string? ThemeTag { get; init; }
    /// <summary>True, если пользователь предпочитает reduced motion.</summary>
    public bool PrefersReducedMotion { get; init; }
}
