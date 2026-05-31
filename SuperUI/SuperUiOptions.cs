namespace SuperUI;

public sealed class SuperUiOptions
{
    public int DefaultToastDurationMs { get; set; } = 4000;
    public int MaxVisibleToasts { get; set; } = 5;

    /// <summary>Default UI language code (e.g. "en", "ru").</summary>
    public string DefaultLanguage { get; set; } = "en";

    /// <summary>Fallback language when a key is missing in the current locale.</summary>
    public string FallbackLanguage { get; set; } = "en";

    /// <summary>List of available language codes.</summary>
    public string[] SupportedLanguages { get; set; } = ["en", "ru"];
}
