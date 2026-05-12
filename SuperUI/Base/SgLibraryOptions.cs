// SuperUI/Base/SgLibraryOptions.cs

using SuperUI.Components;

namespace SuperUI.Base;

/// <summary>
/// Глобальные настройки библиотеки SuperUI.
/// Регистрируется через IOptions<SgLibraryOptions>.
/// Конфигурируется в Program.cs: builder.Services.AddSuperUI(opt => { ... }).
/// </summary>
public sealed class SgLibraryOptions
{
    // ── Тема ──────────────────────────────────────────────────────────────────

    /// <summary>Тема по умолчанию (Auto = следовать системной).</summary>
    public SgTheme DefaultTheme { get; set; } = SgTheme.Auto;

    /// <summary>Язык/локаль по умолчанию (BCP 47: "ru-RU", "en-US").</summary>
    public string Locale { get; set; } = "ru-RU";

    /// <summary>RTL режим по умолчанию.</summary>
    public bool RightToLeft { get; set; }

    // ── Компоненты ────────────────────────────────────────────────────────────

    /// <summary>Размер компонентов по умолчанию.</summary>
    public SgSize DefaultSize { get; set; } = SgSize.Md;

    /// <summary>Анимации включены.</summary>
    public bool AnimationsEnabled { get; set; } = true;

    /// <summary>Длительность анимаций в миллисекундах.</summary>
    public int AnimationDurationMs { get; set; } = 300;

    /// <summary>Префикс CSS-классов (default: "sg-").</summary>
    public string CssPrefix { get; set; } = "sg-";

    // ── Toast / Notification ──────────────────────────────────────────────────

    /// <summary>Максимальное количество toast-уведомлений на экране.</summary>
    public int MaxToasts { get; set; } = 10;

    /// <summary>Позиция toast по умолчанию.</summary>
    public SgPlacement DefaultToastPlacement { get; set; } = SgPlacement.TopRight;

    /// <summary>Длительность toast по умолчанию в миллисекундах (0 = бесконечно).</summary>
    public int DefaultToastDurationMs { get; set; } = 4000;

    // ── Z-Index ───────────────────────────────────────────────────────────────

    /// <summary>Базовый z-index для overlay-компонентов.</summary>
    public int BaseZIndex { get; set; } = 800;

    /// <summary>Шаг z-index между слоями.</summary>
    public int ZIndexStep { get; set; } = 10;

    // ── Accessibility ─────────────────────────────────────────────────────────

    /// <summary>Включить ARIA-атрибуты (accessibility).</summary>
    public bool EnableAria { get; set; } = true;

    /// <summary>Включить focus trap для overlay-компонентов (Modal, Drawer).</summary>
    public bool EnableFocusTrap { get; set; } = true;

    /// <summary>Блокировать прокрутку body при открытии overlay.</summary>
    public bool LockBodyScrollOnOverlay { get; set; } = true;

    // ── Диагностика ───────────────────────────────────────────────────────────

    /// <summary>
    /// Включить диагностику компонентов (счётчики рендеров, JS-вызовов).
    /// Автоматически false в Production.
    /// </summary>
    public bool EnableDiagnostics { get; set; }

    // ── Дополнительные CSS-переменные темы ──────────────────────────────────

    /// <summary>Дополнительные CSS-переменные для темы (--var-name: value).</summary>
    public Dictionary<string, string> ThemeVariables { get; } = [];

    // ── DataGrid ──────────────────────────────────────────────────────────────

    /// <summary>Размер страницы DataGrid по умолчанию.</summary>
    public int DefaultPageSize { get; set; } = 25;

    /// <summary>Включить виртуализацию в DataGrid по умолчанию.</summary>
    public bool EnableVirtualizationByDefault { get; set; }

    // ── Form ──────────────────────────────────────────────────────────────────

    /// <summary>Показывать валидацию при изменении поля (не только при submit).</summary>
    public bool ValidateOnChange { get; set; } = true;

    /// <summary>Позиция метки в формах по умолчанию: "top" | "left".</summary>
    public string DefaultLabelPosition { get; set; } = "top";
}
