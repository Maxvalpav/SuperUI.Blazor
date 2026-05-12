namespace SuperUI.Base;

/// <summary>
/// Глобальная конфигурация SuperUI — каскадный параметр от SgThemeProvider.
/// Используется компонентами через [CascadingParameter] SgConfigContext?.
/// </summary>
public sealed record SgConfigContext
{
    /// <summary>Тема: "light" | "dark" | "auto".</summary>
    public string Theme { get; init; } = "auto";

    /// <summary>Культура: "en-US" | "ru-RU".</summary>
    public string Culture { get; init; } = "en-US";

    /// <summary>Направление текста RTL.</summary>
    public bool IsRtl { get; init; } = false;

    /// <summary>Компактный режим отображения.</summary>
    public bool Compact { get; init; } = false;

    /// <summary>Анимации включены.</summary>
    public bool AnimationsEnabled { get; init; } = true;

    /// <summary>Префикс CSS-классов (для кастомизации).</summary>
    public string CssPrefix { get; init; } = "sg";
}

/// <summary>
/// Контекст темы — каскадный параметр от SgThemeProvider.
/// </summary>
public sealed record SgThemeContext
{
    public string ThemeName { get; init; } = "light";
    public bool IsDark { get; init; } = false;
    public string PrimaryColor { get; init; } = "#4f6af5";
}

// ─────────────────────────────────────────────────────────────────────────────
// FIX CS0246: SgConfig — DI-класс конфигурации (не каскадный параметр!)
// Используется в ServiceCollectionExtensions: services.Configure<SgConfig>(...)
// АЛЬТЕРНАТИВНО: используйте SgLibraryOptions (из ComponentOptionsService.cs)
// Они дублируют друг друга — рекомендуется унификация на SgLibraryOptions.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// DI-конфигурация SuperUI (регистрируется через AddSuperUI(options => ...)).
/// </summary>
/// <remarks>
/// FIX CS0246: этот класс был ожидаем в ServiceCollectionExtensions.cs но отсутствовал.
/// РЕКОМЕНДАЦИЯ: в следующей версии объединить с <see cref="SgLibraryOptions"/>.
/// </remarks>
public sealed class SgConfig
{
    /// <summary>Тема по умолчанию: "light" | "dark" | "auto".</summary>
    public string DefaultTheme { get; set; } = "auto";

    /// <summary>Культура по умолчанию: "en-US" | "ru-RU".</summary>
    public string DefaultCulture { get; set; } = "en-US";

    /// <summary>Длительность toast-уведомлений в мс.</summary>
    public int DefaultToastDurationMs { get; set; } = 4000;

    /// <summary>Анимации включены глобально.</summary>
    public bool AnimationsEnabled { get; set; } = true;

    /// <summary>Компактный режим по умолчанию.</summary>
    public bool Compact { get; set; } = false;

    /// <summary>RTL режим по умолчанию.</summary>
    public bool IsRtl { get; set; } = false;

    /// <summary>Максимальный z-index (база для IZIndexService).</summary>
    public int MaxZIndex { get; set; } = 9999;

    /// <summary>Длительность анимаций в мс.</summary>
    public int DefaultAnimationMs { get; set; } = 300;
}
