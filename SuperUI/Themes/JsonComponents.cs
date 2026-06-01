using System.Text.Json.Serialization;

namespace SuperUI.Themes;

/// <summary>JSON representation of <see cref="IThemeComponents"/>.</summary>
public sealed class JsonComponents : IThemeComponents
{
    [JsonPropertyName("button")]  public JsonButtonTokens? Button  { get; set; }
    [JsonPropertyName("input")]   public JsonInputTokens?  Input   { get; set; }
    [JsonPropertyName("card")]    public JsonCardTokens?   Card    { get; set; }
    [JsonPropertyName("modal")]   public JsonModalTokens?  Modal   { get; set; }
    [JsonPropertyName("table")]   public JsonTableTokens?  Table   { get; set; }
    [JsonPropertyName("tabs")]    public JsonTabsTokens?   Tabs    { get; set; }
    [JsonPropertyName("tooltip")] public JsonTooltipTokens? Tooltip { get; set; }
    [JsonPropertyName("header")]  public JsonHeaderTokens? Header  { get; set; }
    [JsonPropertyName("nav")]     public JsonNavTokens?    Nav     { get; set; }

    [JsonIgnore] public string BtnRadius          => Button?.Radius     ?? "8px";
    [JsonIgnore] public string BtnFontSize        => Button?.FontSize   ?? "0.875rem";
    [JsonIgnore] public string BtnFontWeight      => Button?.FontWeight ?? "500";
    [JsonIgnore] public string BtnHeight          => Button?.Height     ?? "36px";
    [JsonIgnore] public string BtnHeightSm        => Button?.HeightSm   ?? "30px";
    [JsonIgnore] public string BtnHeightLg        => Button?.HeightLg   ?? "44px";

    [JsonIgnore] public string InputRadius        => Input?.Radius   ?? "8px";
    [JsonIgnore] public string InputFontSize      => Input?.FontSize ?? "0.875rem";
    [JsonIgnore] public string InputHeight        => Input?.Height   ?? "36px";
    [JsonIgnore] public string InputHeightSm      => Input?.HeightSm ?? "30px";
    [JsonIgnore] public string InputHeightLg      => Input?.HeightLg ?? "44px";

    [JsonIgnore] public string CardRadius         => Card?.Radius      ?? "12px";
    [JsonIgnore] public string CardPadding        => Card?.Padding     ?? "16px";
    [JsonIgnore] public string CardBorderColor    => Card?.BorderColor ?? "var(--sg-border)";
    [JsonIgnore] public string CardBg             => Card?.Bg          ?? "var(--sg-surface)";

    [JsonIgnore] public string ModalRadius        => Modal?.Radius ?? "12px";

    [JsonIgnore] public string TableRadius          => Table?.Radius          ?? "8px";
    [JsonIgnore] public string TableHeaderFontWeight => Table?.HeaderFontWeight ?? "600";

    [JsonIgnore] public string TabsIndicatorHeight => Tabs?.IndicatorHeight ?? "2px";

    [JsonIgnore] public string TooltipMaxWidth    => Tooltip?.MaxWidth ?? "240px";

    [JsonIgnore] public string HeaderBg    => Header?.Bg ?? "var(--sg-bg)";
    [JsonIgnore] public string HeaderFg    => Header?.Fg ?? "var(--sg-fg)";
    [JsonIgnore] public string NavBg       => Nav?.Bg       ?? "var(--sg-bg-subtle)";
    [JsonIgnore] public string NavFg       => Nav?.Fg       ?? "var(--sg-fg-subtle)";
    [JsonIgnore] public string NavActiveBg => Nav?.ActiveBg ?? "var(--sg-color-primary-subtle)";
    [JsonIgnore] public string NavActiveFg => Nav?.ActiveFg ?? "var(--sg-color-primary)";
}

public sealed class JsonButtonTokens
{
    [JsonPropertyName("radius")]     public string Radius     { get; set; } = "8px";
    [JsonPropertyName("fontSize")]   public string FontSize   { get; set; } = "0.875rem";
    [JsonPropertyName("fontWeight")] public string FontWeight { get; set; } = "500";
    [JsonPropertyName("height")]     public string Height     { get; set; } = "36px";
    [JsonPropertyName("heightSm")]   public string HeightSm   { get; set; } = "30px";
    [JsonPropertyName("heightLg")]   public string HeightLg   { get; set; } = "44px";
}

public sealed class JsonInputTokens
{
    [JsonPropertyName("radius")]   public string Radius   { get; set; } = "8px";
    [JsonPropertyName("fontSize")] public string FontSize { get; set; } = "0.875rem";
    [JsonPropertyName("height")]   public string Height   { get; set; } = "36px";
    [JsonPropertyName("heightSm")] public string HeightSm { get; set; } = "30px";
    [JsonPropertyName("heightLg")] public string HeightLg { get; set; } = "44px";
}

public sealed class JsonCardTokens
{
    [JsonPropertyName("radius")]      public string Radius      { get; set; } = "12px";
    [JsonPropertyName("padding")]     public string Padding     { get; set; } = "16px";
    [JsonPropertyName("borderColor")] public string BorderColor { get; set; } = "var(--sg-border)";
    [JsonPropertyName("bg")]          public string Bg          { get; set; } = "var(--sg-surface)";
}

public sealed class JsonModalTokens
{
    [JsonPropertyName("radius")] public string Radius { get; set; } = "12px";
}

public sealed class JsonTableTokens
{
    [JsonPropertyName("radius")]          public string Radius          { get; set; } = "8px";
    [JsonPropertyName("headerFontWeight")] public string HeaderFontWeight { get; set; } = "600";
}

public sealed class JsonTabsTokens
{
    [JsonPropertyName("indicatorHeight")] public string IndicatorHeight { get; set; } = "2px";
}

public sealed class JsonTooltipTokens
{
    [JsonPropertyName("maxWidth")] public string MaxWidth { get; set; } = "240px";
}

public sealed class JsonHeaderTokens
{
    [JsonPropertyName("bg")] public string Bg { get; set; } = "var(--sg-bg)";
    [JsonPropertyName("fg")] public string Fg { get; set; } = "var(--sg-fg)";
}

public sealed class JsonNavTokens
{
    [JsonPropertyName("bg")]       public string Bg       { get; set; } = "var(--sg-bg-subtle)";
    [JsonPropertyName("fg")]       public string Fg       { get; set; } = "var(--sg-fg-subtle)";
    [JsonPropertyName("activeBg")] public string ActiveBg { get; set; } = "var(--sg-color-primary-subtle)";
    [JsonPropertyName("activeFg")] public string ActiveFg { get; set; } = "var(--sg-color-primary)";
}
