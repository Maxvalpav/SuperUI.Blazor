using System.Text.Json.Serialization;

namespace SuperUI.Themes;

/// <summary>JSON representation of <see cref="IThemePrimitives"/>.</summary>
public sealed class JsonPrimitives : IThemePrimitives
{
    [JsonPropertyName("neutral")] public JsonNeutralScale Neutral { get; set; } = new();
    [JsonPropertyName("primary")] public JsonScale10 Primary { get; set; } = new();
    [JsonPropertyName("success")] public JsonScale5  Success { get; set; } = new();
    [JsonPropertyName("danger")]  public JsonScale5  Danger  { get; set; } = new();
    [JsonPropertyName("warning")] public JsonScale4  Warning { get; set; } = new();
    [JsonPropertyName("info")]    public JsonScale4  Info    { get; set; } = new();
    [JsonPropertyName("fonts")]   public JsonFonts   Fonts   { get; set; } = new();
    [JsonPropertyName("radius")]  public JsonRadiusScale Radius { get; set; } = new();

    [JsonIgnore] public string Neutral0   => Neutral.N0;
    [JsonIgnore] public string Neutral50  => Neutral.N50;
    [JsonIgnore] public string Neutral100 => Neutral.N100;
    [JsonIgnore] public string Neutral200 => Neutral.N200;
    [JsonIgnore] public string Neutral300 => Neutral.N300;
    [JsonIgnore] public string Neutral400 => Neutral.N400;
    [JsonIgnore] public string Neutral500 => Neutral.N500;
    [JsonIgnore] public string Neutral600 => Neutral.N600;
    [JsonIgnore] public string Neutral700 => Neutral.N700;
    [JsonIgnore] public string Neutral800 => Neutral.N800;
    [JsonIgnore] public string Neutral900 => Neutral.N900;

    [JsonIgnore] public string Primary50  => Primary.N50;
    [JsonIgnore] public string Primary100 => Primary.N100;
    [JsonIgnore] public string Primary200 => Primary.N200;
    [JsonIgnore] public string Primary300 => Primary.N300;
    [JsonIgnore] public string Primary400 => Primary.N400;
    [JsonIgnore] public string Primary500 => Primary.N500;
    [JsonIgnore] public string Primary600 => Primary.N600;
    [JsonIgnore] public string Primary700 => Primary.N700;
    [JsonIgnore] public string Primary800 => Primary.N800;
    [JsonIgnore] public string Primary900 => Primary.N900;

    [JsonIgnore] public string Success50  => Success.N50;
    [JsonIgnore] public string Success100 => Success.N100;
    [JsonIgnore] public string Success500 => Success.N500;
    [JsonIgnore] public string Success600 => Success.N600;
    [JsonIgnore] public string Success700 => Success.N700;

    [JsonIgnore] public string Danger50  => Danger.N50;
    [JsonIgnore] public string Danger100 => Danger.N100;
    [JsonIgnore] public string Danger500 => Danger.N500;
    [JsonIgnore] public string Danger600 => Danger.N600;
    [JsonIgnore] public string Danger700 => Danger.N700;

    [JsonIgnore] public string Warning50  => Warning.N50;
    [JsonIgnore] public string Warning100 => Warning.N100;
    [JsonIgnore] public string Warning500 => Warning.N500;
    [JsonIgnore] public string Warning600 => Warning.N600;

    [JsonIgnore] public string Info50  => Info.N50;
    [JsonIgnore] public string Info100 => Info.N100;
    [JsonIgnore] public string Info500 => Info.N500;
    [JsonIgnore] public string Info600 => Info.N600;

    [JsonIgnore] public string FontSans  => Fonts.Sans;
    [JsonIgnore] public string FontMono  => Fonts.Mono;
    [JsonIgnore] public string FontSerif => Fonts.Serif ?? Fonts.Sans;

    [JsonIgnore] public string RadiusNone => Radius.None;
    [JsonIgnore] public string RadiusXs   => Radius.Xs;
    [JsonIgnore] public string RadiusSm   => Radius.Sm;
    [JsonIgnore] public string RadiusMd   => Radius.Md;
    [JsonIgnore] public string RadiusLg   => Radius.Lg;
    [JsonIgnore] public string RadiusXl   => Radius.Xl;
    [JsonIgnore] public string Radius2Xl  => Radius.N2xl;
    [JsonIgnore] public string RadiusFull => Radius.Full;
}

public sealed class JsonNeutralScale
{
    [JsonPropertyName("0")]   public string N0   { get; set; } = "#fff";
    [JsonPropertyName("50")]  public string N50  { get; set; } = "#f8fafc";
    [JsonPropertyName("100")] public string N100 { get; set; } = "#f1f5f9";
    [JsonPropertyName("200")] public string N200 { get; set; } = "#e2e8f0";
    [JsonPropertyName("300")] public string N300 { get; set; } = "#cbd5e1";
    [JsonPropertyName("400")] public string N400 { get; set; } = "#94a3b8";
    [JsonPropertyName("500")] public string N500 { get; set; } = "#64748b";
    [JsonPropertyName("600")] public string N600 { get; set; } = "#475569";
    [JsonPropertyName("700")] public string N700 { get; set; } = "#334155";
    [JsonPropertyName("800")] public string N800 { get; set; } = "#1e293b";
    [JsonPropertyName("900")] public string N900 { get; set; } = "#0f172a";
}

public sealed class JsonScale10
{
    [JsonPropertyName("50")]  public string N50  { get; set; } = "";
    [JsonPropertyName("100")] public string N100 { get; set; } = "";
    [JsonPropertyName("200")] public string N200 { get; set; } = "";
    [JsonPropertyName("300")] public string N300 { get; set; } = "";
    [JsonPropertyName("400")] public string N400 { get; set; } = "";
    [JsonPropertyName("500")] public string N500 { get; set; } = "";
    [JsonPropertyName("600")] public string N600 { get; set; } = "";
    [JsonPropertyName("700")] public string N700 { get; set; } = "";
    [JsonPropertyName("800")] public string N800 { get; set; } = "";
    [JsonPropertyName("900")] public string N900 { get; set; } = "";
}

public sealed class JsonScale5
{
    [JsonPropertyName("50")]  public string N50  { get; set; } = "";
    [JsonPropertyName("100")] public string N100 { get; set; } = "";
    [JsonPropertyName("500")] public string N500 { get; set; } = "";
    [JsonPropertyName("600")] public string N600 { get; set; } = "";
    [JsonPropertyName("700")] public string N700 { get; set; } = "";
}

public sealed class JsonScale4
{
    [JsonPropertyName("50")]  public string N50  { get; set; } = "";
    [JsonPropertyName("100")] public string N100 { get; set; } = "";
    [JsonPropertyName("500")] public string N500 { get; set; } = "";
    [JsonPropertyName("600")] public string N600 { get; set; } = "";
}

public sealed class JsonFonts
{
    [JsonPropertyName("sans")]  public string Sans { get; set; } = "system-ui, sans-serif";
    [JsonPropertyName("mono")]  public string Mono { get; set; } = "ui-monospace, monospace";
    [JsonPropertyName("serif")] public string? Serif { get; set; }
}

public sealed class JsonRadiusScale
{
    [JsonPropertyName("none")] public string None { get; set; } = "0";
    [JsonPropertyName("xs")]   public string Xs   { get; set; } = "2px";
    [JsonPropertyName("sm")]   public string Sm   { get; set; } = "4px";
    [JsonPropertyName("md")]   public string Md   { get; set; } = "8px";
    [JsonPropertyName("lg")]   public string Lg   { get; set; } = "16px";
    [JsonPropertyName("xl")]   public string Xl   { get; set; } = "24px";
    [JsonPropertyName("2xl")]  public string N2xl { get; set; } = "32px";
    [JsonPropertyName("full")] public string Full { get; set; } = "9999px";
}
