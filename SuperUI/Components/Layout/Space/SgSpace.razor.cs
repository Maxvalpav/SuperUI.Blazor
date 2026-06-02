using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Base.ComponentBases;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Set components spacing. Arranges children with consistent gaps, optional separators,
/// and supports both horizontal and vertical orientations.
/// </summary>
public partial class SgSpace : SgComponentBase
{
    /// <summary>Items to be spaced.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Space orientation. Default is <see cref="SgOrientation.Horizontal"/>.</summary>
    [Parameter] public SgOrientation Orientation { get; set; } = SgOrientation.Horizontal;

    /// <summary>
    /// Space size as a magic-string for back-compat. Allowed: <c>"small"</c> (8px),
    /// <c>"middle"</c> (16px), <c>"large"</c> (24px), or any CSS length (e.g. <c>"16px"</c>).
    /// Default <c>"small"</c>.
    /// </summary>
    [Obsolete("Use the strongly-typed Space parameter (SgSize?) for type-safe spacing. " +
              "For custom CSS lengths use a custom style on a parent wrapper.")]
    [Parameter] public string Size { get; set; } = "small";

    /// <summary>
    /// Space size from design system (theme token). Overrides <see cref="Size"/>.
    /// </summary>
    [Parameter] public SgSize? Space { get; set; }

    /// <summary>Visual density. Scales internal gap via theme tokens.</summary>
    [Parameter] public SgDensity Density { get; set; } = SgDensity.Default;

    /// <summary>Alignment of items. Default is <see cref="SgAlignItems.Center"/>.</summary>
    [Parameter] public SgAlignItems Align { get; set; } = SgAlignItems.Center;

    /// <summary>Main-axis distribution.</summary>
    [Parameter] public SgJustifyContent Justify { get; set; } = SgJustifyContent.Start;

    /// <summary>Whether to wrap lines. Only works in horizontal orientation.</summary>
    [Parameter] public bool Wrap { get; set; }

    /// <summary>If <c>true</c>, forces all children to stretch across the cross axis (overrides <see cref="Align"/>).</summary>
    [Parameter] public bool Stretch { get; set; }

    /// <summary>
    /// Optional separator rendered between every child item (e.g. <c>"|"</c>, <c>"•"</c>).
    /// Implemented via CSS pseudo-element — no markup duplication needed.
    /// </summary>
    [Parameter] public string? Separator { get; set; }

    /// <summary>
    /// Legacy: optional separator fragment. Accepted for back-compat with
    /// <c>&lt;Split&gt;…&lt;/Split&gt;</c> named child content pattern.
    /// Renders as a single trailing marker — for proper per-item separators use <see cref="Separator"/>.
    /// </summary>
    [Parameter] public RenderFragment? Split { get; set; }

    /// <summary>If true, the space fills the full width of its parent.</summary>
    [Parameter] public bool FullWidth { get; set; }

    /// <summary>If true, renders as inline-flex instead of flex.</summary>
    [Parameter] public bool Inline { get; set; }

    /// <summary>If <c>true</c>, the container lifts on hover.</summary>
    [Parameter] public bool Hoverable { get; set; }

    /// <summary>Click event on the space root.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    private bool hasOnClick => OnClick.HasDelegate;

    private bool IsVertical => Orientation == SgOrientation.Vertical;

    private string? ResolveMagicSize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return raw.Trim().ToLowerInvariant() switch
        {
            "small"  => "8px",
            "middle" => "16px",
            "large"  => "24px",
            _ => raw.Contains("px") || raw.Contains("rem") || raw.Contains("var") || raw.Contains("em") || raw.Contains("%")
                ? raw
                : $"{raw}px"
        };
    }

    private double DensityScale => Density switch
    {
        SgDensity.Compact      => 0.5,
        SgDensity.Comfortable  => 1.5,
        _                      => 1.0
    };

    private string? ResolvedGap
    {
        get
        {
            string? baseGap = Space switch
            {
                SgSize.Sm     => "var(--sg-space-4)",
                SgSize.Md     => "var(--sg-space-8)",
                SgSize.Lg     => "var(--sg-space-16)",
                SgSize.Xl     => "var(--sg-space-24)",
                SgSize.FibMd  => "var(--sg-space-fib-4)",
                SgSize.FibLg  => "var(--sg-space-fib-5)",
                SgSize.FibXl  => "var(--sg-space-fib-6)",
                SgSize.FibXxl => "var(--sg-space-fib-7)",
                _ => null
            };

#pragma warning disable CS0618
            baseGap ??= ResolveMagicSize(Size);
#pragma warning restore CS0618

            if (baseGap is null) return null;
            if (Density == SgDensity.Default) return baseGap;

            if (baseGap.EndsWith("px", StringComparison.OrdinalIgnoreCase) &&
                double.TryParse(baseGap.AsSpan(0, baseGap.Length - 2), NumberStyles.Any, CultureInfo.InvariantCulture, out var n))
            {
                return (n * DensityScale).ToString("0.##", CultureInfo.InvariantCulture) + "px";
            }
            return baseGap;
        }
    }

    private string AlignCss
    {
        get
        {
            if (Stretch) return "stretch";
            return Align switch
            {
                SgAlignItems.Start    => "flex-start",
                SgAlignItems.Center   => "center",
                SgAlignItems.End      => "flex-end",
                SgAlignItems.Baseline => "baseline",
                _                     => "stretch"
            };
        }
    }

    private string JustifyCss => Justify switch
    {
        SgJustifyContent.Center       => "center",
        SgJustifyContent.End          => "flex-end",
        SgJustifyContent.SpaceBetween => "space-between",
        SgJustifyContent.SpaceAround  => "space-around",
        SgJustifyContent.SpaceEvenly  => "space-evenly",
        _                             => "flex-start"
    };

    private string ComputedClass => Css("sgc-space sg-space")
        .AddClass("sg-space-vertical",   IsVertical)
        .AddClass("sg-space-horizontal", !IsVertical)
        .AddClass("sg-space-wrap",       Wrap)
        .AddClass("sg-space-dividers",   !string.IsNullOrEmpty(Separator))
        .AddClass("sg-space-stretch",    Stretch)
        .AddClass("sg-space-inline",     Inline)
        .AddClass("sg-space-full",       FullWidth)
        .AddClass("sg-row-hoverable",    Hoverable)
        .Build();

    private string ComputedStyle => Styles()
        .AddStyle("display",         (FullWidth && !Inline) ? "flex" : "inline-flex")
        .AddStyle("flex-direction",  IsVertical ? "column" : "row")
        .AddStyle("gap",             ResolvedGap, ResolvedGap != null)
        .AddStyle("align-items",     AlignCss)
        .AddStyle("justify-content", JustifyCss)
        .AddStyle("flex-wrap",       Wrap ? "wrap" : "nowrap")
        .AddStyle("width",           "100%", FullWidth)
        .AddStyle("--sg-space-split", Separator, !string.IsNullOrEmpty(Separator))
        .Build();

    private Task HandleClick(MouseEventArgs args) =>
        OnClick.HasDelegate ? OnClick.InvokeAsync(args) : Task.CompletedTask;
}
