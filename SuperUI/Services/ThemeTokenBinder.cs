// ─────────────────────────────────────────────────────────────────
// FILE: Services/ThemeTokenBinder.cs
// Описание: ConfigProvider / ThemeTokenBinder — централизованное
// управление CSS-токенами темы.
// ─────────────────────────────────────────────────────────────────
using System;

namespace SuperUI.Services;

/// <summary>
/// Интерфейс сервиса биндинга токенов темы.
/// </summary>
public interface IThemeTokenBinder
{
    ThemeTokens Current { get; }
    event Action? ThemeChanged;
    void Apply(ThemeTokens tokens);
    void ApplyDark();
    void ApplyLight();
}

/// <summary>
/// Токены дизайн-системы SuperUI (CSS custom properties).
/// </summary>
public sealed class ThemeTokens
{
    public string PrimaryColor    { get; set; } = "#6366f1";
    public string SecondaryColor  { get; set; } = "#8b5cf6";
    public string SuccessColor    { get; set; } = "#22c55e";
    public string DangerColor     { get; set; } = "#ef4444";
    public string WarnColor       { get; set; } = "#f59e0b";
    public string InfoColor       { get; set; } = "#3b82f6";
    public string BgColor         { get; set; } = "#ffffff";
    public string SurfaceColor    { get; set; } = "#f8fafc";
    public string BorderColor     { get; set; } = "#e2e8f0";
    public string TextColor       { get; set; } = "#1e293b";
    public string TextMutedColor  { get; set; } = "#94a3b8";
    public string FontFamily      { get; set; } = "Inter, system-ui, sans-serif";
    public string BorderRadius    { get; set; } = "8px";
    public string ShadowSm        { get; set; } = "0 1px 2px rgba(0,0,0,.05)";
    public string ShadowMd        { get; set; } = "0 4px 6px rgba(0,0,0,.1)";
    public string ShadowLg        { get; set; } = "0 10px 15px rgba(0,0,0,.1)";
    public string TransitionSpeed { get; set; } = "200ms";
    public string TransitionEase  { get; set; } = "cubic-bezier(.4,0,.2,1)";

    /// <summary>Генерирует CSS :root { --sg-*: ...; } строку.</summary>
    public string ToCssVariables() => $"""
        :root {{
            --sg-primary:        {PrimaryColor};
            --sg-secondary:      {SecondaryColor};
            --sg-success:        {SuccessColor};
            --sg-danger:         {DangerColor};
            --sg-warn:           {WarnColor};
            --sg-info:           {InfoColor};
            --sg-bg:             {BgColor};
            --sg-surface:        {SurfaceColor};
            --sg-border:         {BorderColor};
            --sg-text:           {TextColor};
            --sg-text-muted:     {TextMutedColor};
            --sg-font:           {FontFamily};
            --sg-radius:         {BorderRadius};
            --sg-shadow-sm:      {ShadowSm};
            --sg-shadow-md:      {ShadowMd};
            --sg-shadow-lg:      {ShadowLg};
            --sg-transition:     {TransitionSpeed} {TransitionEase};
        }}
        """;
}

/// <summary>
/// Scoped-сервис биндинга токенов темы.
/// Позволяет менять тему в рантайме без перезагрузки.
/// </summary>
public sealed class ThemeTokenBinder : IThemeTokenBinder
{
    public ThemeTokens Current { get; private set; } = new();
    public event Action? ThemeChanged;

    /// <summary>Применяет новые токены и нотифицирует компоненты.</summary>
    public void Apply(ThemeTokens tokens)
    {
        Current = tokens;
        ThemeChanged?.Invoke();
    }

    /// <summary>Применяет тёмную тему.</summary>
    public void ApplyDark() => Apply(new ThemeTokens
    {
        BgColor       = "#0f172a",
        SurfaceColor  = "#1e293b",
        BorderColor   = "#334155",
        TextColor     = "#f1f5f9",
        TextMutedColor= "#64748b",
    });

    public void ApplyLight() => Apply(new ThemeTokens());
}
