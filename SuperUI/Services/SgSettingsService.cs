using Microsoft.JSInterop;
using SuperUI.Services.Llm;
using SuperUI.Localization;
using SuperUI.Services;

namespace SuperUI.Services;

public class SgSettingsService
{
    private readonly IJSRuntime _js;
    private readonly SgThemeService _themeService;
    private readonly ILlmService _llmService;
    private readonly ISuperUILocalizer _localizer;

    public SgSettingsService(
        IJSRuntime js, 
        SgThemeService themeService, 
        ILlmService llmService,
        ISuperUILocalizer localizer)
    {
        _js = js;
        _themeService = themeService;
        _llmService = llmService;
        _localizer = localizer;
    }

    public string DateFormat { get; private set; } = "DD.MM.YYYY";
    public string NumberSeparator { get; private set; } = "space"; // "space", "comma", "none"
    public bool IsTabbedMode { get; private set; }

    public event Action? OnSettingsChanged;

    public async Task InitializeAsync()
    {
        DateFormat = await _js.InvokeAsync<string>("localStorage.getItem", "sui-date-format") ?? "DD.MM.YYYY";
        NumberSeparator = await _js.InvokeAsync<string>("localStorage.getItem", "sui-number-separator") ?? "space";
        IsTabbedMode = await _js.InvokeAsync<string>("localStorage.getItem", "sui-tabbed-mode") == "true";
    }

    public async Task SetTabbedModeAsync(bool enabled)
    {
        IsTabbedMode = enabled;
        await _js.InvokeVoidAsync("localStorage.setItem", "sui-tabbed-mode", enabled.ToString().ToLower());
        OnSettingsChanged?.Invoke();
    }

    public async Task SetDateFormatAsync(string format)
    {
        DateFormat = format;
        await _js.InvokeVoidAsync("localStorage.setItem", "sui-date-format", format);
        OnSettingsChanged?.Invoke();
    }

    public async Task SetNumberSeparatorAsync(string separator)
    {
        NumberSeparator = separator;
        await _js.InvokeVoidAsync("localStorage.setItem", "sui-number-separator", separator);
        OnSettingsChanged?.Invoke();
    }

    public async Task ResetAllSettingsAsync()
    {
        await _js.InvokeVoidAsync("localStorage.clear");
        await _js.InvokeVoidAsync("location.reload");
    }

    public async Task ClearCacheAsync()
    {
        // For now, clear localStorage, but could be extended to Cache API
        await _js.InvokeVoidAsync("localStorage.clear");
    }

    public async Task SetLanguageAsync(string culture, bool reload = true)
    {
        _localizer.SetLanguage(culture);
        await _js.InvokeVoidAsync("localStorage.setItem", "sui-language", culture);
    }

    public async Task<string> GetLanguageAsync()
    {
        return await _js.InvokeAsync<string>("localStorage.getItem", "sui-language") ?? "en";
    }

    public SgLlmConfig GetDefaultLlmConfig()
    {
        return _llmService.CurrentConfig ?? new SgLlmConfig();
    }

    public async Task<SgLlmConfig?> LoadDefaultLlmConfigAsync()
    {
        if (_llmService is SgLlmService impl)
        {
            return await impl.GetGlobalConfigAsync();
        }
        return _llmService.CurrentConfig;
    }

    public async Task SaveDefaultLlmConfigAsync(SgLlmConfig config)
    {
        // Persist to localStorage so the choice survives reloads.
        if (_llmService is SgLlmService impl)
        {
            await impl.SaveGlobalConfigAsync(config);
        }
        // Initialize the engine for immediate use across the app.
        await _llmService.InitializeAsync(config);
    }
}
