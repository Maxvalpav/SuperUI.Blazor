using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SuperUI.Localization;

public sealed class LocalizationService : ISuperUILocalizer
{
    private static readonly string[] s_localeResources = typeof(LocalizationService).Assembly
        .GetManifestResourceNames().Where(r => r.StartsWith("SuperUI.Resources.locales.") && r.EndsWith(".json")).ToArray();

    private readonly ConcurrentDictionary<string, string> _translations = new();
    private readonly ConcurrentDictionary<string, string> _overrides = new();
    private readonly Dictionary<string, LocaleEntry> _catalog = new();
    private readonly Assembly _assembly;
    private readonly string _defaultLanguage;
    private readonly string _fallbackLanguage;
    private readonly string[] _supportedLanguages;
    private string _currentLang;

    public event Action? OnLocaleChanged;

    public LocalizationService() : this(null) { }

    public LocalizationService(IOptions<SuperUiOptions>? options)
    {
        var opts = options?.Value;
        _defaultLanguage = opts?.DefaultLanguage ?? "en";
        _fallbackLanguage = opts?.FallbackLanguage ?? "en";
        _supportedLanguages = (opts?.SupportedLanguages?.ToArray()) ?? ["en", "ru"];
        _currentLang = _defaultLanguage;
        _assembly = typeof(LocalizationService).Assembly;
        BuildCatalog();
        LoadLanguage(_currentLang);
    }

    public string CurrentLanguage => _currentLang;

    public IEnumerable<string> SupportedLanguages => _supportedLanguages;

    public string this[string key]
    {
        get
        {
            if (_overrides.TryGetValue(key, out var overrideVal))
                return overrideVal;
            if (_translations.TryGetValue(key, out var val))
                return val;
            return key;
        }
    }

    public string GetString(string key, params object[] args)
    {
        var format = this[key];
        if (args.Length > 0)
        {
            try
            {
                return string.Format(CultureInfo.CurrentCulture, format, args);
            }
            catch (Exception ex)
            {
                Debug.Fail($"[SuperUI] string.Format failed for key '{key}': {ex.Message}");
                return format;
            }
        }
        return format;
    }

    public void SetLanguage(string lang)
    {
        lang = NormalizeLang(lang);
        if (string.IsNullOrEmpty(lang) || _currentLang == lang)
            return;
        LoadLanguage(lang);
        _currentLang = lang;
    }

    public void SetOverride(string key, string value)
    {
        _overrides[key] = value;
        OnLocaleChanged?.Invoke();
    }

    public void RemoveOverride(string key)
    {
        _overrides.TryRemove(key, out _);
        OnLocaleChanged?.Invoke();
    }

    public void ClearOverrides()
    {
        _overrides.Clear();
        OnLocaleChanged?.Invoke();
    }

    public Dictionary<string, string> GetOverrides() => new(_overrides);

    public void LoadOverrides(Dictionary<string, string> overrides)
    {
        _overrides.Clear();
        foreach (var kv in overrides)
            _overrides[kv.Key] = kv.Value;
    }

    public IReadOnlyDictionary<string, LocaleEntry> GetCatalog() => _catalog;

    public string GetOriginalValue(string key) =>
        _catalog.TryGetValue(key, out var entry) ? entry.OriginalValue : key;

    public string GetOverrideOrNull(string key) =>
        _overrides.TryGetValue(key, out var val) ? val : null;

    private void BuildCatalog()
    {
        _catalog.Clear();
        var resources = s_localeResources.Where(r => r.StartsWith("SuperUI.Resources.locales.") && r.EndsWith(".json"));

        foreach (var resource in resources)
        {
            var domain = ExtractDomain(resource);
            using var stream = _assembly.GetManifestResourceStream(resource);
            if (stream is null) continue;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var domainKeys = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (domainKeys is null) continue;
            foreach (var kv in domainKeys)
            {
                _catalog.TryAdd(kv.Key, new LocaleEntry(kv.Key, domain, kv.Value));
            }
        }
    }

    private static string ExtractDomain(string resourceName)
    {
        var parts = resourceName.Split('.');
        var filtered = parts.Where(p => p != "json").ToArray();
        if (filtered.Length >= 3)
        {
            var domainPart = filtered[^1];
            return domainPart;
        }
        return "Unknown";
    }

    private static string NormalizeLang(string lang)
    {
        if (string.IsNullOrEmpty(lang)) return "en";
        var parts = lang.Split('-');
        var baseLang = parts[0].ToLowerInvariant();
        return baseLang switch
        {
            "en" => "en",
            "ru" => "ru",
            _ => "en"
        };
    }

    private void LoadLanguage(string lang)
    {
        var result = new Dictionary<string, string>();

        if (lang != _fallbackLanguage)
        {
            LoadLanguageInto(result, _fallbackLanguage);
        }

        LoadLanguageInto(result, lang);

        _translations.Clear();
        foreach (var kv in result)
        {
            _translations[kv.Key] = kv.Value;
        }

        OnLocaleChanged?.Invoke();
    }

    private void LoadLanguageInto(Dictionary<string, string> target, string lang)
    {
        var resourcePrefix = $"SuperUI.Resources.locales.{lang}.";
        var resources = _assembly.GetManifestResourceNames()
            .Where(r => r.StartsWith(resourcePrefix) && r.EndsWith(".json"));

        foreach (var resource in resources)
        {
            using var stream = _assembly.GetManifestResourceStream(resource);
            if (stream is null) continue;
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var domainKeys = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (domainKeys is not null)
            {
                foreach (var kv in domainKeys)
                {
                    target[kv.Key] = kv.Value;
                }
            }
        }
    }
}

public readonly record struct LocaleEntry(string Key, string Domain, string OriginalValue);
