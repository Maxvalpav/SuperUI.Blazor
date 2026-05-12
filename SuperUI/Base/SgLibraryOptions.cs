// SuperUI/Base/SgLibraryOptions.cs

namespace SuperUI.Base;

/// <summary>
/// Глобальные настройки библиотеки SuperUI.
/// Регистрируется через IOptions<SgLibraryOptions>.
/// </summary>
public sealed class SgLibraryOptions
{
    /// <summary>Тема по умолчанию.</summary>
    public SgTheme DefaultTheme { get; set; } = SgTheme.Auto;

    /// <summary>Язык/локаль по умолчанию (BCP 47: "ru-RU", "en-US").</summary>
    public string Locale { get; set; } = "ru-RU";

    /// <summary>RTL режим по умолчанию.</summary>
    public bool RightToLeft { get; set; } = false;

    /// <summary>Размер компонентов по умолчанию.</summary>
    public SgSize DefaultSize { get; set; } = SgSize.Medium;

    /// <summary>Анимации включены.</summary>
    public bool AnimationsEnabled { get; set; } = true;

    /// <summary>Длительность анимаций (мс).</summary>
    public int AnimationDurationMs { get; set; } = 300;

    /// <summary>Префикс CSS-классов (default: "sg-").</summary>
    public string CssPrefix { get; set; } = "sg-";

    /// <summary>Максимальное количество toast на экране.</summary>
    public int MaxToasts { get; set; } = 10;

    /// <summary>Позиция toast по умолчанию.</summary>
    public SgPlacement DefaultToastPlacement { get; set; } = SgPlacement.TopRight;

    /// <summary>Длительность toast по умолчанию (мс).</summary>
    public int DefaultToastDurationMs { get; set; } = 4000;

    /// <summary>
    /// Включить debug-диагностику компонентов (счётчики рендеров, JS вызовов).
    /// В Production всегда false.
    /// </summary>
    public bool EnableDiagnostics { get; set; }

    /// <summary>Z-index базовый уровень (default: 800).</summary>
    public int BaseZIndex { get; set; } = 800;

    /// <summary>Шаг z-index (default: 10).</summary>
    public int ZIndexStep { get; set; } = 10;

    /// <summary>Включить ARIA-атрибуты (accessibility).</summary>
    public bool EnableAria { get; set; } = true;

    /// <summary>Включить focus trap для overlay компонентов.</summary>
    public bool EnableFocusTrap { get; set; } = true;

    /// <summary>Включить блокировку scroll при открытии overlay.</summary>
    public bool LockBodyScrollOnOverlay { get; set; } = true;
}
