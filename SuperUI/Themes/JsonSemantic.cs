using System.Text.Json.Serialization;

namespace SuperUI.Themes;

/// <summary>JSON representation of <see cref="IThemeSemantic"/> for a single mode.</summary>
public sealed class JsonSemantic : IThemeSemantic
{
    [JsonPropertyName("bg")]           public JsonBgGroup      Bg        { get; set; } = new();
    [JsonPropertyName("surface")]      public JsonSurfaceGroup Surface   { get; set; } = new();
    [JsonPropertyName("fg")]           public JsonFgGroup      Fg        { get; set; } = new();
    [JsonPropertyName("border")]       public JsonBorderGroup  Border    { get; set; } = new();
    [JsonPropertyName("divider")]      public string           Divider   { get; set; } = "";
    [JsonPropertyName("colorPrimary")] public JsonColorGroup   ColorPrimary { get; set; } = new();
    [JsonPropertyName("colorSuccess")] public JsonColorGroup   ColorSuccess { get; set; } = new();
    [JsonPropertyName("colorDanger")]  public JsonColorGroup   ColorDanger  { get; set; } = new();
    [JsonPropertyName("colorWarning")] public JsonColorGroup   ColorWarning { get; set; } = new();
    [JsonPropertyName("colorInfo")]    public JsonColorGroup   ColorInfo    { get; set; } = new();
    [JsonPropertyName("font")]         public JsonFontGroup    Font         { get; set; } = new();
    [JsonPropertyName("text")]         public JsonTextScale    Text         { get; set; } = new();
    [JsonPropertyName("fontWeight")]   public JsonFontWeightGroup FontWeight { get; set; } = new();
    [JsonPropertyName("lineHeight")]   public JsonLineHeightGroup LineHeight { get; set; } = new();
    [JsonPropertyName("shadow")]       public JsonShadowScale  Shadow       { get; set; } = new();
    [JsonPropertyName("radius")]       public JsonSemanticRadius Radius     { get; set; } = new();
    [JsonPropertyName("transition")]   public JsonTransitionGroup Transition { get; set; } = new();
    [JsonPropertyName("focusRing")]    public JsonFocusRingGroup FocusRing   { get; set; } = new();
    [JsonPropertyName("z")]            public JsonZGroup        Z            { get; set; } = new();
    [JsonPropertyName("elevation")]    public JsonElevationScale Elevation    { get; set; } = new();
    [JsonPropertyName("motion")]       public JsonMotionGroup   Motion       { get; set; } = new();
    [JsonPropertyName("density")]      public JsonDensityGroup  Density      { get; set; } = new();
    [JsonPropertyName("measure")]      public JsonMeasureGroup  Measure      { get; set; } = new();

    string IThemeSemantic.BgDefault     => Bg.Default;
    string IThemeSemantic.BgSubtle      => Bg.Subtle;
    string IThemeSemantic.BgMuted       => Bg.Muted;
    string IThemeSemantic.BgEmphasized  => Bg.Emphasized;
    string IThemeSemantic.BgOverlay     => Bg.Overlay;
    string IThemeSemantic.BgGlass       => Bg.Glass;
    string IThemeSemantic.BorderGlass   => Border.Default;
    string IThemeSemantic.BlurGlass     => "10px";

    string IThemeSemantic.Surface         => Surface0.Default;
    string IThemeSemantic.SurfaceRaised   => Surface0.Raised;
    string IThemeSemantic.SurfaceOverlay  => Surface0.Overlay;

    string IThemeSemantic.FgDefault   => Fg.Default;
    string IThemeSemantic.FgSubtle    => Fg.Subtle;
    string IThemeSemantic.FgMuted     => Fg.Muted;
    string IThemeSemantic.FgDisabled  => Fg.Disabled;
    string IThemeSemantic.FgInverse   => Fg.Inverse;
    string IThemeSemantic.FgLink      => Fg.Link;
    string IThemeSemantic.FgLinkHover => Fg.LinkHover;

    string IThemeSemantic.BorderDefault => Border.Default;
    string IThemeSemantic.BorderSubtle  => Border.Subtle;
    string IThemeSemantic.BorderStrong  => Border.Strong;
    string IThemeSemantic.BorderFocus   => Border.Focus;

    string IThemeSemantic.ColorPrimary       => ColorPrimary0.Default;
    string IThemeSemantic.ColorPrimarySubtle => ColorPrimary0.Subtle;
    string IThemeSemantic.ColorPrimaryMuted  => ColorPrimary0.Subtle;
    string IThemeSemantic.ColorPrimaryHover  => ColorPrimary0.Hover;
    string IThemeSemantic.ColorPrimaryActive => ColorPrimary0.Hover;
    string IThemeSemantic.ColorPrimaryFg     => ColorPrimary0.Fg;

    string IThemeSemantic.ColorSuccess       => ColorSuccess0.Default;
    string IThemeSemantic.ColorSuccessSubtle => ColorSuccess0.Subtle;
    string IThemeSemantic.ColorSuccessHover  => ColorSuccess0.Hover;
    string IThemeSemantic.ColorSuccessFg     => ColorSuccess0.Fg;

    string IThemeSemantic.ColorDanger        => ColorDanger0.Default;
    string IThemeSemantic.ColorDangerSubtle  => ColorDanger0.Subtle;
    string IThemeSemantic.ColorDangerHover   => ColorDanger0.Hover;
    string IThemeSemantic.ColorDangerFg      => ColorDanger0.Fg;

    string IThemeSemantic.ColorWarning       => ColorWarning0.Default;
    string IThemeSemantic.ColorWarningSubtle => ColorWarning0.Subtle;
    string IThemeSemantic.ColorWarningHover  => ColorWarning0.Hover;
    string IThemeSemantic.ColorWarningFg     => ColorWarning0.Fg;

    string IThemeSemantic.ColorInfo          => ColorInfo0.Default;
    string IThemeSemantic.ColorInfoSubtle    => ColorInfo0.Subtle;
    string IThemeSemantic.ColorInfoHover     => ColorInfo0.Hover;
    string IThemeSemantic.ColorInfoFg        => ColorInfo0.Fg;

    string IThemeSemantic.Font     => Font0.Default;
    string IThemeSemantic.FontMono => Font0.Mono;

    string IThemeSemantic.TextXs   => Text0.Xs;
    string IThemeSemantic.TextSm   => Text0.Sm;
    string IThemeSemantic.TextBase => Text0.Base;
    string IThemeSemantic.TextLg   => Text0.Lg;
    string IThemeSemantic.TextXl   => Text0.Xl;
    string IThemeSemantic.Text2Xl  => Text0.N2xl;
    string IThemeSemantic.Text3Xl  => Text0.N3xl;

    string IThemeSemantic.FontWeightNormal   => FontWeight0.Normal;
    string IThemeSemantic.FontWeightMedium   => FontWeight0.Medium;
    string IThemeSemantic.FontWeightSemibold => FontWeight0.Semibold;
    string IThemeSemantic.FontWeightBold     => FontWeight0.Bold;

    string IThemeSemantic.LineHeightTight   => LineHeight0.Tight;
    string IThemeSemantic.LineHeightNormal  => LineHeight0.Normal;
    string IThemeSemantic.LineHeightRelaxed => LineHeight0.Relaxed;

    string IThemeSemantic.ShadowXs => Shadow0.Xs;
    string IThemeSemantic.ShadowSm => Shadow0.Sm;
    string IThemeSemantic.ShadowMd => Shadow0.Md;
    string IThemeSemantic.ShadowLg => Shadow0.Lg;
    string IThemeSemantic.ShadowXl => Shadow0.Xl;

    string IThemeSemantic.RadiusSm   => Radius0.Sm;
    string IThemeSemantic.RadiusMd   => Radius0.Md;
    string IThemeSemantic.RadiusLg   => Radius0.Lg;
    string IThemeSemantic.RadiusXl   => Radius0.Xl;
    string IThemeSemantic.RadiusFull => Radius0.Full;

    string IThemeSemantic.TransitionFast => Transition0.Fast;
    string IThemeSemantic.TransitionBase => Transition0.Base;
    string IThemeSemantic.TransitionSlow => Transition0.Slow;

    string IThemeSemantic.FocusRing       => FocusRing0.Default;
    string IThemeSemantic.FocusRingDanger => FocusRing0.Danger;

    int IThemeSemantic.ZDropdown => Z0.Dropdown;
    int IThemeSemantic.ZSticky   => Z0.Sticky;
    int IThemeSemantic.ZModal    => Z0.Modal;
    int IThemeSemantic.ZToast    => Z0.Toast;
    int IThemeSemantic.ZTooltip  => Z0.Tooltip;

    // v2.0 state tokens (sane defaults derived from existing tokens).
    string IThemeSemantic.ColorPrimaryActiveBg   => ColorPrimary0.Hover;
    string IThemeSemantic.ColorPrimaryDisabled   => Fg.Disabled;
    string IThemeSemantic.ColorPrimaryDisabledBg => Bg.Muted;
    string IThemeSemantic.ColorPrimarySelectedBg => ColorPrimary0.Subtle;
    string IThemeSemantic.ColorSuccessActiveBg   => ColorSuccess0.Hover;
    string IThemeSemantic.ColorSuccessDisabled   => Fg.Disabled;
    string IThemeSemantic.ColorDangerActiveBg    => ColorDanger0.Hover;
    string IThemeSemantic.ColorDangerDisabled    => Fg.Disabled;
    string IThemeSemantic.ColorWarningActiveBg   => ColorWarning0.Hover;
    string IThemeSemantic.ColorWarningDisabled   => Fg.Disabled;
    string IThemeSemantic.ColorInfoActiveBg      => ColorInfo0.Hover;
    string IThemeSemantic.ColorInfoDisabled      => Fg.Disabled;

    string IThemeSemantic.FgPlaceholder   => Fg.Muted;
    string IThemeSemantic.SurfaceHover    => Bg.Subtle;
    string IThemeSemantic.SurfaceActive   => Bg.Muted;
    string IThemeSemantic.SurfaceSelected => ColorPrimary0.Subtle;
    string IThemeSemantic.BorderHover     => Border.Strong;

    string IThemeSemantic.Elevation1 => Shadow0.Xs;
    string IThemeSemantic.Elevation2 => Shadow0.Sm;
    string IThemeSemantic.Elevation3 => Shadow0.Md;
    string IThemeSemantic.Elevation4 => Shadow0.Lg;
    string IThemeSemantic.Elevation5 => Shadow0.Xl;

    string IThemeSemantic.MotionInstant => Motion.Instant;
    string IThemeSemantic.MotionFast    => Motion.Fast;
    string IThemeSemantic.MotionBase    => Motion.Base;
    string IThemeSemantic.MotionSlow    => Motion.Slow;
    string IThemeSemantic.MotionSlower  => Motion.Slower;
    string IThemeSemantic.EasingStandard => Motion.EasingStandard;
    string IThemeSemantic.EasingEmphasis => Motion.EasingEmphasis;
    string IThemeSemantic.EasingDecel    => Motion.EasingDecel;

    string IThemeSemantic.DensityCompact      => Density.Compact;
    string IThemeSemantic.DensityComfortable  => Density.Comfortable;
    string IThemeSemantic.DensitySpacious     => Density.Spacious;

    string IThemeSemantic.MeasureNarrow  => Measure.Narrow;
    string IThemeSemantic.MeasureOptimal => Measure.Optimal;
    string IThemeSemantic.MeasureWide    => Measure.Wide;

    // Backing fields for the explicit interface implementations above.
    private JsonSurfaceGroup Surface0    => Surface;
    private JsonColorGroup   ColorPrimary0 => ColorPrimary;
    private JsonColorGroup   ColorSuccess0 => ColorSuccess;
    private JsonColorGroup   ColorDanger0  => ColorDanger;
    private JsonColorGroup   ColorWarning0 => ColorWarning;
    private JsonColorGroup   ColorInfo0    => ColorInfo;
    private JsonFontGroup    Font0         => Font;
    private JsonTextScale    Text0         => Text;
    private JsonFontWeightGroup FontWeight0 => FontWeight;
    private JsonLineHeightGroup LineHeight0 => LineHeight;
    private JsonShadowScale  Shadow0       => Shadow;
    private JsonSemanticRadius Radius0     => Radius;
    private JsonTransitionGroup Transition0 => Transition;
    private JsonFocusRingGroup FocusRing0   => FocusRing;
    private JsonZGroup        Z0            => Z;
}

public sealed class JsonBgGroup
{
    [JsonPropertyName("default")]    public string Default    { get; set; } = "";
    [JsonPropertyName("subtle")]     public string Subtle     { get; set; } = "";
    [JsonPropertyName("muted")]      public string Muted      { get; set; } = "";
    [JsonPropertyName("emphasized")] public string Emphasized { get; set; } = "";
    [JsonPropertyName("overlay")]    public string Overlay    { get; set; } = "";
    [JsonPropertyName("glass")]      public string Glass      { get; set; } = "";
}

public sealed class JsonSurfaceGroup
{
    [JsonPropertyName("default")] public string Default { get; set; } = "";
    [JsonPropertyName("raised")]  public string Raised  { get; set; } = "";
    [JsonPropertyName("overlay")] public string Overlay { get; set; } = "";
}

public sealed class JsonFgGroup
{
    [JsonPropertyName("default")]   public string Default   { get; set; } = "";
    [JsonPropertyName("subtle")]    public string Subtle    { get; set; } = "";
    [JsonPropertyName("muted")]     public string Muted     { get; set; } = "";
    [JsonPropertyName("disabled")]  public string Disabled  { get; set; } = "";
    [JsonPropertyName("inverse")]   public string Inverse   { get; set; } = "";
    [JsonPropertyName("link")]      public string Link      { get; set; } = "";
    [JsonPropertyName("linkHover")] public string LinkHover { get; set; } = "";
}

public sealed class JsonBorderGroup
{
    [JsonPropertyName("default")] public string Default { get; set; } = "";
    [JsonPropertyName("subtle")]  public string Subtle  { get; set; } = "";
    [JsonPropertyName("strong")]  public string Strong  { get; set; } = "";
    [JsonPropertyName("focus")]   public string Focus   { get; set; } = "";
}

public sealed class JsonColorGroup
{
    [JsonPropertyName("default")] public string Default { get; set; } = "";
    [JsonPropertyName("subtle")]  public string Subtle  { get; set; } = "";
    [JsonPropertyName("hover")]   public string Hover   { get; set; } = "";
    [JsonPropertyName("fg")]      public string Fg      { get; set; } = "";
}

public sealed class JsonFontGroup
{
    [JsonPropertyName("default")] public string Default { get; set; } = "";
    [JsonPropertyName("mono")]    public string Mono    { get; set; } = "";
}

public sealed class JsonTextScale
{
    [JsonPropertyName("xs")]   public string Xs   { get; set; } = "0.75rem";
    [JsonPropertyName("sm")]   public string Sm   { get; set; } = "0.875rem";
    [JsonPropertyName("base")] public string Base { get; set; } = "1rem";
    [JsonPropertyName("lg")]   public string Lg   { get; set; } = "1.125rem";
    [JsonPropertyName("xl")]   public string Xl   { get; set; } = "1.25rem";
    [JsonPropertyName("2xl")]  public string N2xl { get; set; } = "1.5rem";
    [JsonPropertyName("3xl")]  public string N3xl { get; set; } = "1.875rem";
}

public sealed class JsonFontWeightGroup
{
    [JsonPropertyName("normal")]   public string Normal   { get; set; } = "400";
    [JsonPropertyName("medium")]   public string Medium   { get; set; } = "500";
    [JsonPropertyName("semibold")] public string Semibold { get; set; } = "600";
    [JsonPropertyName("bold")]     public string Bold     { get; set; } = "700";
}

public sealed class JsonLineHeightGroup
{
    [JsonPropertyName("tight")]   public string Tight   { get; set; } = "1.25";
    [JsonPropertyName("normal")]  public string Normal  { get; set; } = "1.5";
    [JsonPropertyName("relaxed")] public string Relaxed { get; set; } = "1.75";
}

public sealed class JsonShadowScale
{
    [JsonPropertyName("xs")] public string Xs { get; set; } = "none";
    [JsonPropertyName("sm")] public string Sm { get; set; } = "none";
    [JsonPropertyName("md")] public string Md { get; set; } = "none";
    [JsonPropertyName("lg")] public string Lg { get; set; } = "none";
    [JsonPropertyName("xl")] public string Xl { get; set; } = "none";
}

public sealed class JsonSemanticRadius
{
    [JsonPropertyName("sm")]   public string Sm   { get; set; } = "4px";
    [JsonPropertyName("md")]   public string Md   { get; set; } = "8px";
    [JsonPropertyName("lg")]   public string Lg   { get; set; } = "16px";
    [JsonPropertyName("xl")]   public string Xl   { get; set; } = "24px";
    [JsonPropertyName("full")] public string Full { get; set; } = "9999px";
}

public sealed class JsonTransitionGroup
{
    [JsonPropertyName("fast")] public string Fast { get; set; } = "120ms ease";
    [JsonPropertyName("base")] public string Base { get; set; } = "200ms ease";
    [JsonPropertyName("slow")] public string Slow { get; set; } = "350ms ease";
}

public sealed class JsonFocusRingGroup
{
    [JsonPropertyName("default")] public string Default { get; set; } = "0 0 0 2px #fff, 0 0 0 4px currentColor";
    [JsonPropertyName("danger")]  public string Danger  { get; set; } = "0 0 0 2px #fff, 0 0 0 4px red";
}

public sealed class JsonZGroup
{
    [JsonPropertyName("dropdown")] public int Dropdown { get; set; } = 1000;
    [JsonPropertyName("sticky")]   public int Sticky   { get; set; } = 1020;
    [JsonPropertyName("modal")]    public int Modal    { get; set; } = 1050;
    [JsonPropertyName("toast")]    public int Toast    { get; set; } = 1070;
    [JsonPropertyName("tooltip")]  public int Tooltip  { get; set; } = 1100;
}

public sealed class JsonElevationScale
{
    [JsonPropertyName("1")] public string N1 { get; set; } = "0 1px 1px 0 rgb(0 0 0 / 0.04)";
    [JsonPropertyName("2")] public string N2 { get; set; } = "0 1px 2px 0 rgb(0 0 0 / 0.05), 0 1px 1px -1px rgb(0 0 0 / 0.04)";
    [JsonPropertyName("3")] public string N3 { get; set; } = "0 2px 4px -1px rgb(0 0 0 / 0.06), 0 1px 2px -1px rgb(0 0 0 / 0.04)";
    [JsonPropertyName("4")] public string N4 { get; set; } = "0 8px 16px -4px rgb(0 0 0 / 0.07), 0 2px 4px -2px rgb(0 0 0 / 0.04)";
    [JsonPropertyName("5")] public string N5 { get; set; } = "0 16px 32px -8px rgb(0 0 0 / 0.10), 0 4px 8px -4px rgb(0 0 0 / 0.06)";
}

public sealed class JsonMotionGroup
{
    [JsonPropertyName("instant")]       public string Instant       { get; set; } = "89ms";
    [JsonPropertyName("fast")]          public string Fast          { get; set; } = "144ms";
    [JsonPropertyName("base")]          public string Base          { get; set; } = "233ms";
    [JsonPropertyName("slow")]          public string Slow          { get; set; } = "377ms";
    [JsonPropertyName("slower")]        public string Slower        { get; set; } = "610ms";
    [JsonPropertyName("easingStandard")] public string EasingStandard { get; set; } = "cubic-bezier(0.4, 0, 0.2, 1)";
    [JsonPropertyName("easingEmphasis")] public string EasingEmphasis { get; set; } = "cubic-bezier(0.2, 0, 0, 1)";
    [JsonPropertyName("easingDecel")]    public string EasingDecel    { get; set; } = "cubic-bezier(0, 0, 0.2, 1)";
}

public sealed class JsonDensityGroup
{
    [JsonPropertyName("compact")]     public string Compact     { get; set; } = "-2px";
    [JsonPropertyName("comfortable")] public string Comfortable { get; set; } = "0px";
    [JsonPropertyName("spacious")]    public string Spacious    { get; set; } = "+2px";
}

public sealed class JsonMeasureGroup
{
    [JsonPropertyName("narrow")]  public string Narrow  { get; set; } = "45ch";
    [JsonPropertyName("optimal")] public string Optimal { get; set; } = "66ch";
    [JsonPropertyName("wide")]    public string Wide    { get; set; } = "75ch";
}
