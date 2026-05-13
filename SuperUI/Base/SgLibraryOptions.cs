// SuperUI/Base/SgLibraryOptions.cs
// ИСПРАВЛЕНО:
// ✅ CS0234: using SuperUI.Services → using SuperUI.Base.Services
// ✅ ThemeService тип изменён с SgThemeService на ISgThemeService (принцип инверсии)
// ✅ DefaultRenderMode, EnableWasmCryptoOptimization добавлены

using SuperUI.Base.Services; // ✅ FIX: был using SuperUI.Services — НЕ СУЩЕСТВУЕТ

namespace SuperUI.Base;

/// <summary>
/// Global configuration options for the SuperUI library.
/// Fluent API with sensible defaults. Configurable per-render-mode.
/// </summary>
public class SgLibraryOptions
{
    // --- Component sizes ---
    public SgComponentSize DefaultSize { get; set; } = SgComponentSize.Medium;

    // --- Animation ---
    public bool AnimationsEnabled { get; set; } = true;
    public int AnimationDurationMs { get; set; } = 200;

    // --- Accessibility ---
    public bool EnableAria { get; set; } = true;

    // --- Localization ---
    public string Locale { get; set; } = "en-US";

    // --- Z-Index ---
    public int BaseZIndex { get; set; } = 1000;
    public int ZIndexStep { get; set; } = 100;

    // --- CSS ---
    public string CssPrefix { get; set; } = "sg-";

    // --- RTL ---
    public bool RightToLeft { get; set; }

    // --- Theme ---
    public Dictionary<string, string> ThemeVariables { get; set; } = new();

    // --- Debug / Diagnostics ---
    public bool EnableDiagnostics { get; set; }
    public bool EnableRenderTracking { get; set; }

    // --- Services (lazy-initialized) ---
    public Services.ISgComponentTypeRegistry? ComponentRegistry { get; set; }
    public IComponentFactory? ComponentFactory { get; set; }

    // ✅ FIX: используем интерфейс, а не реализацию
    public ISgThemeService? ThemeService { get; set; }
    public IFocusTrapService? FocusTrapService { get; set; }

    // --- Throttling ---
    public int RenderThrottleMs { get; set; } = 0;
    public int BatchSize { get; set; } = 10;

    // --- SSR (.NET 8+) ---
    public bool EnableSsrStreaming { get; set; } = true;
    public int SsrStreamingChunkSizeBytes { get; set; } = 4096;

    // --- Render Mode (.NET 8+) ---
    /// <summary>
    /// Default render mode when not specified per-component.
    /// SgRenderMode.Unknown = auto-detect.
    /// </summary>
    public SgRenderMode DefaultRenderMode { get; set; } = SgRenderMode.Unknown;

    /// <summary>
    /// Forced render mode overrides auto-detection.
    /// Valid: "Server", "WebAssembly", "Auto", "StaticSSR", null.
    /// </summary>
    public string? ForcedRenderMode { get; set; }

    // --- Reconnection (Server-side) ---
    public int ReconnectionRetryMs { get; set; } = 2000;
    public int ReconnectionMaxRetries { get; set; } = 8;

    // --- WASM ---
    public bool EnableWasmLazyLoading { get; set; } = true;

    // --- WASM Crypto optimization (.NET 8+) ---
    public bool EnableWasmCryptoOptimization { get; set; } = true;

    /// <summary>Compute z-index for a given layer (0-based).</summary>
    public int GetZIndex(int layer = 0) => BaseZIndex + (layer * ZIndexStep);

    /// <summary>Clone these options.</summary>
    public SgLibraryOptions Clone() => new()
    {
        DefaultSize = DefaultSize,
        AnimationsEnabled = AnimationsEnabled,
        AnimationDurationMs = AnimationDurationMs,
        EnableAria = EnableAria,
        Locale = Locale,
        BaseZIndex = BaseZIndex,
        ZIndexStep = ZIndexStep,
        CssPrefix = CssPrefix,
        RightToLeft = RightToLeft,
        ThemeVariables = new Dictionary<string, string>(ThemeVariables),
        EnableDiagnostics = EnableDiagnostics,
        EnableRenderTracking = EnableRenderTracking,
        ComponentRegistry = ComponentRegistry,
        ComponentFactory = ComponentFactory,
        ThemeService = ThemeService,
        FocusTrapService = FocusTrapService,
        RenderThrottleMs = RenderThrottleMs,
        BatchSize = BatchSize,
        EnableSsrStreaming = EnableSsrStreaming,
        SsrStreamingChunkSizeBytes = SsrStreamingChunkSizeBytes,
        DefaultRenderMode = DefaultRenderMode,
        ForcedRenderMode = ForcedRenderMode,
        ReconnectionRetryMs = ReconnectionRetryMs,
        ReconnectionMaxRetries = ReconnectionMaxRetries,
        EnableWasmLazyLoading = EnableWasmLazyLoading,
        EnableWasmCryptoOptimization = EnableWasmCryptoOptimization
    };
}

public enum SgComponentSize { ExtraSmall, Small, Medium, Large, ExtraLarge }
