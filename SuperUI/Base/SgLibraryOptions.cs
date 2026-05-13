// SuperUI/Base/SgLibraryOptions.cs
using System;
using System.Collections.Generic;
using SuperUI.Base.Services;

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
    public SgComponentRegistry? ComponentRegistry { get; set; }
    public SgComponentFactory? ComponentFactory { get; set; }
    public SgThemeService? ThemeService { get; set; }
    public FocusTrapService? FocusTrapService { get; set; }

    // --- Throttling ---
    public int RenderThrottleMs { get; set; } = 0; // 0 = no throttling
    public int BatchSize { get; set; } = 10;

    // --- SSR ---
    public bool EnableSsrStreaming { get; set; } = true;
    public int SsrStreamingChunkSizeBytes { get; set; } = 4096;

    // --- Reconnection (Server-side) ---
    public int ReconnectionRetryMs { get; set; } = 2000;
    public int ReconnectionMaxRetries { get; set; } = 8;

    // --- WASM ---
    public bool EnableWasmLazyLoading { get; set; } = true;

    // --- New: Render mode aware ---
    public string? ForcedRenderMode { get; set; }

    /// <summary>Compute z-index for a given layer (0-based).</summary>
    public int GetZIndex(int layer = 0) => BaseZIndex + (layer * ZIndexStep);

    /// <summary>Clone these options.</summary>
    public SgLibraryOptions Clone()
    {
        return new SgLibraryOptions
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
            ReconnectionRetryMs = ReconnectionRetryMs,
            ReconnectionMaxRetries = ReconnectionMaxRetries,
            EnableWasmLazyLoading = EnableWasmLazyLoading,
            ForcedRenderMode = ForcedRenderMode
        };
    }
}

public enum SgComponentSize
{
    ExtraSmall,
    Small,
    Medium,
    Large,
    ExtraLarge
}
