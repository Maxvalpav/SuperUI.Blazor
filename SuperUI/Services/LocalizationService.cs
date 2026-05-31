using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SuperUI.Localization;

public sealed class LocalizationService : ISuperUILocalizer
{
    private readonly ConcurrentDictionary<string, string> _translations = new();
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
        _supportedLanguages = opts?.SupportedLanguages ?? ["en", "ru"];
        _currentLang = _defaultLanguage;
        _assembly = typeof(LocalizationService).Assembly;
        LoadLanguage(_currentLang);
    }

    public string CurrentLanguage => _currentLang;

    public IEnumerable<string> SupportedLanguages => _supportedLanguages;

    public string this[string key]
    {
        get
        {
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
                    target.TryAdd(kv.Key, kv.Value);
                }
            }
        }
    }
}
