using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SuperUI.Base.Builders;
using SuperUI.Base.Utilities;
using SuperUI.Localization;
using SuperUI.Services;
using SuperUI.Themes;

namespace SuperUI.Base.ComponentBases;

public abstract class SgComponentBase : ComponentBase, IDisposable, IAsyncDisposable
{
    private string? _autoId;
    private ILogger? _logger;
    private bool _disposed;
    private Action<IThemeDefinition, string>? _themeChangedHandler;
    private Action? _localeChangedHandler;

    [Parameter] public string? CssClass { get; set; }
    [Parameter] public string? Style { get; set; }
    [Parameter] public string? Id { get; set; }
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    [CascadingParameter] protected HostEnvironmentContext? Host { get; set; }

    [Inject] protected ILoggerFactory? LoggerFactory { get; set; }
    [Inject] protected SgThemeService ThemeService { get; set; } = default!;
    [Inject] protected ISuperUILocalizer Localizer { get; set; } = default!;

    protected string CurrentMode => ThemeService.CurrentMode;
    protected bool IsDark => ThemeService.IsDark;
    protected IThemeDefinition CurrentTheme => ThemeService.CurrentTheme;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _themeChangedHandler = (_, _) =>
        {
            if (_disposed) return;
            try { InvokeAsync(StateHasChanged); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
            catch (TaskCanceledException) { }
        };
        ThemeService.ThemeChanged += _themeChangedHandler;

        _localeChangedHandler = () =>
        {
            if (_disposed) return;
            try { InvokeAsync(StateHasChanged); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
            catch (TaskCanceledException) { }
        };
        Localizer.OnLocaleChanged += _localeChangedHandler;
    }

    public virtual void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (ThemeService is not null && _themeChangedHandler is not null)
            ThemeService.ThemeChanged -= _themeChangedHandler;
        if (Localizer is not null && _localeChangedHandler is not null)
            Localizer.OnLocaleChanged -= _localeChangedHandler;
    }

    public virtual ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    protected ElementReference RootRef;
    protected string ResolvedId => Id ?? (_autoId ??= SgIdGenerator.StableIdFor(this, IdPrefix));
    protected virtual string IdPrefix => "sg";

    protected ILogger Logger => _logger ??= LoggerFactory?.CreateLogger(GetType()) ?? NullLogger.Instance;

    protected CssBuilder Css(string? rootClass = null) => CssBuilder.Default(rootClass).AddClass(CssClass).AddClassFromAttributes(AdditionalAttributes);
    protected StyleBuilder Styles() => StyleBuilder.Default(Style).AddStyleFromAttributes(AdditionalAttributes);

    protected IReadOnlyDictionary<string, object>? AttributesWithoutClassAndStyle
    {
        get
        {
            if (AdditionalAttributes is null) return null;
            if (!AdditionalAttributes.ContainsKey("class") && !AdditionalAttributes.ContainsKey("style")) return AdditionalAttributes;
            var dict = new Dictionary<string, object>(AdditionalAttributes.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in AdditionalAttributes)
            {
                if (kv.Key.Equals("class", StringComparison.OrdinalIgnoreCase) || kv.Key.Equals("style", StringComparison.OrdinalIgnoreCase)) continue;
                dict[kv.Key] = kv.Value;
            }
            return dict;
        }
    }

    protected static string CombineCss(params string?[] tokens) => string.Join(" ", tokens.Where(t => !string.IsNullOrWhiteSpace(t))!);
}

public sealed class HostEnvironmentContext
{
    public string? ThemeTag { get; init; }
    public bool PrefersReducedMotion { get; init; }
}
