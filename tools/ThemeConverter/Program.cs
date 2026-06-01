using System.Text.Json;
using SuperUI.Themes;

namespace ThemeConverter;

internal static class Program
{
    private static int Main(string[] args)
    {
        var outDir = args.Length > 0
            ? args[0]
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "SuperUI", "Themes", "json"));

        Directory.CreateDirectory(outDir);
        Console.WriteLine($"Output directory: {outDir}");

        var themes = new IThemeDefinition[]
        {
            new NaturaTheme(),
            new SolarisTheme(),
            new RoyalTheme(),
            new GraphiteTheme(),
            new ForestTheme(),
            new NeonTheme(),
            new GlassTheme(),
            new SignatureTheme(),
            new ChronoTheme(),
            new InclusTheme(),
            new SylvanTheme(),
            new ReaderTheme(),
            new WaveTheme(),
            new AureaTheme(),
            new CantusTheme(),
            new FractalisTheme(),
            new CosmosTheme(),
            new GordianTheme(),
            new CalyxTheme(),
            new ApexTheme(),
            new MediciTheme(),
            new ZenTheme(),
            new AetherTheme(),
            new OasisTheme(),
            new NeoTheme(),
            new ClarityTheme(),
            new ElementTheme(),
            new RadiusTheme(),
            new FluxTheme(),
            new MuseTheme(),
            new ForgeTheme(),
            new PrismTheme(),
            new ClarityClinicalTheme(),
            new CircadianTheme(),
            new ErgoTheme(),
            new BiofiliaTheme(),
            new LuminaTheme(),
            new GlassLightTheme(),
            new GlassDarkTheme(),
            new GlassTintedTheme(),
            new GlassNeumorphicTheme(),
            new VeiledTheme(),
            new WindowTheme(),
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        var written = 0;
        foreach (var theme in themes)
        {
            var json = ToJson(theme, options);
            var path = Path.Combine(outDir, theme.Id + ".json");
            File.WriteAllText(path, json);
            Console.WriteLine($"  {theme.Id,-24} -> {Path.GetFileName(path)}");
            written++;
        }

        Console.WriteLine($"\nDone. {written} themes written to {outDir}");
        return 0;
    }

    private static string ToJson(IThemeDefinition theme, JsonSerializerOptions options)
    {
        var def = new JsonThemeDefinition
        {
            Id          = theme.Id,
            Name        = theme.Name,
            Description = theme.Description,
            Author      = theme.Author,
            Version     = theme.Version,
            Category    = theme.Category,
            AdditionalCss = theme.AdditionalCss,
            Primitives  = MapPrimitives(theme.Primitives),
            Light       = MapSemantic(theme.Light),
            Dark        = theme.Dark is null ? null : MapSemantic(theme.Dark),
            Components  = theme.Components is null ? null : MapComponents(theme.Components),
            Typography  = theme.Typography is null ? null : MapTypography(theme.Typography),
        };
        return JsonSerializer.Serialize(def, options);
    }

    private static JsonPrimitives MapPrimitives(IThemePrimitives p) => new()
    {
        Neutral = new JsonNeutralScale
        {
            N0   = p.Neutral0,   N50  = p.Neutral50,  N100 = p.Neutral100,
            N200 = p.Neutral200, N300 = p.Neutral300, N400 = p.Neutral400,
            N500 = p.Neutral500, N600 = p.Neutral600, N700 = p.Neutral700,
            N800 = p.Neutral800, N900 = p.Neutral900,
        },
        Primary = new JsonScale10
        {
            N50  = p.Primary50,  N100 = p.Primary100, N200 = p.Primary200,
            N300 = p.Primary300, N400 = p.Primary400, N500 = p.Primary500,
            N600 = p.Primary600, N700 = p.Primary700, N800 = p.Primary800,
            N900 = p.Primary900,
        },
        Success = new JsonScale5  { N50 = p.Success50, N100 = p.Success100, N500 = p.Success500, N600 = p.Success600, N700 = p.Success700 },
        Danger  = new JsonScale5  { N50 = p.Danger50,  N100 = p.Danger100,  N500 = p.Danger500,  N600 = p.Danger600,  N700 = p.Danger700 },
        Warning = new JsonScale4  { N50 = p.Warning50, N100 = p.Warning100, N500 = p.Warning500, N600 = p.Warning600 },
        Info    = new JsonScale4  { N50 = p.Info50,    N100 = p.Info100,    N500 = p.Info500,    N600 = p.Info600 },
        Fonts   = new JsonFonts   { Sans = p.FontSans, Mono = p.FontMono, Serif = p.FontSerif },
        Radius  = new JsonRadiusScale
        {
            None = p.RadiusNone, Xs = p.RadiusXs, Sm = p.RadiusSm, Md = p.RadiusMd,
            Lg = p.RadiusLg, Xl = p.RadiusXl, N2xl = p.Radius2Xl, Full = p.RadiusFull,
        },
    };

    private static JsonSemantic MapSemantic(IThemeSemantic s) => new()
    {
        Bg = new JsonBgGroup
        {
            Default    = s.BgDefault,    Subtle = s.BgSubtle,     Muted = s.BgMuted,
            Emphasized = s.BgEmphasized, Overlay = s.BgOverlay,  Glass = s.BgGlass,
        },
        Surface = new JsonSurfaceGroup
        {
            Default = s.Surface, Raised = s.SurfaceRaised, Overlay = s.SurfaceOverlay,
        },
        Fg = new JsonFgGroup
        {
            Default   = s.FgDefault,   Subtle    = s.FgSubtle,    Muted    = s.FgMuted,
            Disabled  = s.FgDisabled,  Inverse   = s.FgInverse,   Link     = s.FgLink,
            LinkHover = s.FgLinkHover,
        },
        Border = new JsonBorderGroup
        {
            Default = s.BorderDefault, Subtle = s.BorderSubtle,
            Strong  = s.BorderStrong,  Focus  = s.BorderFocus,
        },
        Divider = s.Divider,
        ColorPrimary = new JsonColorGroup { Default = s.ColorPrimary, Subtle = s.ColorPrimarySubtle, Hover = s.ColorPrimaryHover, Fg = s.ColorPrimaryFg },
        ColorSuccess = new JsonColorGroup { Default = s.ColorSuccess, Subtle = s.ColorSuccessSubtle, Hover = s.ColorSuccessHover, Fg = s.ColorSuccessFg },
        ColorDanger  = new JsonColorGroup { Default = s.ColorDanger,  Subtle = s.ColorDangerSubtle,  Hover = s.ColorDangerHover,  Fg = s.ColorDangerFg },
        ColorWarning = new JsonColorGroup { Default = s.ColorWarning, Subtle = s.ColorWarningSubtle, Hover = s.ColorWarningHover, Fg = s.ColorWarningFg },
        ColorInfo    = new JsonColorGroup { Default = s.ColorInfo,    Subtle = s.ColorInfoSubtle,    Hover = s.ColorInfoHover,    Fg = s.ColorInfoFg },
        Font   = new JsonFontGroup { Default = s.Font, Mono = s.FontMono },
        Text   = new JsonTextScale
        {
            Xs = s.TextXs, Sm = s.TextSm, Base = s.TextBase, Lg = s.TextLg,
            Xl = s.TextXl, N2xl = s.Text2Xl, N3xl = s.Text3Xl,
        },
        FontWeight = new JsonFontWeightGroup
        {
            Normal = s.FontWeightNormal,   Medium   = s.FontWeightMedium,
            Semibold = s.FontWeightSemibold, Bold    = s.FontWeightBold,
        },
        LineHeight = new JsonLineHeightGroup { Tight = s.LineHeightTight, Normal = s.LineHeightNormal, Relaxed = s.LineHeightRelaxed },
        Shadow = new JsonShadowScale { Xs = s.ShadowXs, Sm = s.ShadowSm, Md = s.ShadowMd, Lg = s.ShadowLg, Xl = s.ShadowXl },
        Radius = new JsonSemanticRadius { Sm = s.RadiusSm, Md = s.RadiusMd, Lg = s.RadiusLg, Xl = s.RadiusXl, Full = s.RadiusFull },
        Transition = new JsonTransitionGroup { Fast = s.TransitionFast, Base = s.TransitionBase, Slow = s.TransitionSlow },
        FocusRing   = new JsonFocusRingGroup   { Default = s.FocusRing, Danger = s.FocusRingDanger },
        Z           = new JsonZGroup           { Dropdown = s.ZDropdown, Sticky = s.ZSticky, Modal = s.ZModal, Toast = s.ZToast, Tooltip = s.ZTooltip },
    };

    private static JsonComponents MapComponents(IThemeComponents c) => new()
    {
        Button  = new JsonButtonTokens  { Radius = c.BtnRadius, FontSize = c.BtnFontSize, FontWeight = c.BtnFontWeight, Height = c.BtnHeight, HeightSm = c.BtnHeightSm, HeightLg = c.BtnHeightLg },
        Input   = new JsonInputTokens   { Radius = c.InputRadius, FontSize = c.InputFontSize, Height = c.InputHeight, HeightSm = c.InputHeightSm, HeightLg = c.InputHeightLg },
        Card    = new JsonCardTokens    { Radius = c.CardRadius, Padding = c.CardPadding, BorderColor = c.CardBorderColor, Bg = c.CardBg },
        Modal   = new JsonModalTokens   { Radius = c.ModalRadius },
        Table   = new JsonTableTokens   { Radius = c.TableRadius, HeaderFontWeight = c.TableHeaderFontWeight },
        Tabs    = new JsonTabsTokens    { IndicatorHeight = c.TabsIndicatorHeight },
        Tooltip = new JsonTooltipTokens { MaxWidth = c.TooltipMaxWidth },
        Header  = new JsonHeaderTokens  { Bg = c.HeaderBg, Fg = c.HeaderFg },
        Nav     = new JsonNavTokens     { Bg = c.NavBg, Fg = c.NavFg, ActiveBg = c.NavActiveBg, ActiveFg = c.NavActiveFg },
    };

    private static JsonTypography MapTypography(IThemeTypography t)
    {
        var scale = new JsonHeadingScale();
        SetHeading(scale, "h1", t.H1);
        SetHeading(scale, "h2", t.H2);
        SetHeading(scale, "h3", t.H3);
        SetHeading(scale, "h4", t.H4);
        SetHeading(scale, "h5", t.H5);
        SetHeading(scale, "h6", t.H6);

        return new JsonTypography
        {
            GoogleFontsImportUrl = t.GoogleFontsImportUrl,
            EmbedGoogleFontsImport = t.EmbedGoogleFontsImport,
            HeadingFont = t.HeadingFont,
            Headings = scale,
        };
    }

    private static void SetHeading(JsonHeadingScale scale, string which, HeadingSettings h)
    {
        var target = which switch
        {
            "h1" => scale.H1, "h2" => scale.H2, "h3" => scale.H3,
            "h4" => scale.H4, "h5" => scale.H5, _    => scale.H6,
        };
        target.FontSize      = h.FontSize;
        target.FontFamily    = h.FontFamily;
        target.FontWeight    = h.FontWeight;
        target.LineHeight    = h.LineHeight;
        target.LetterSpacing = h.LetterSpacing;
    }
}
