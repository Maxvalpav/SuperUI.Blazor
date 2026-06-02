using System.Text.Json.Serialization;

namespace SuperUI.Themes;

/// <summary>JSON representation of <see cref="IThemeComponents"/>.</summary>
public sealed class JsonComponents : IThemeComponents
{
    [JsonPropertyName("button")]   public JsonButtonTokens?   Button   { get; set; }
    [JsonPropertyName("input")]    public JsonInputTokens?    Input    { get; set; }
    [JsonPropertyName("select")]   public JsonSelectTokens?   Select   { get; set; }
    [JsonPropertyName("checkbox")] public JsonCheckboxTokens? Checkbox { get; set; }
    [JsonPropertyName("switch")]   public JsonSwitchTokens?   Switch   { get; set; }
    [JsonPropertyName("card")]     public JsonCardTokens?     Card     { get; set; }
    [JsonPropertyName("modal")]    public JsonModalTokens?    Modal    { get; set; }
    [JsonPropertyName("dropdown")] public JsonDropdownTokens? Dropdown { get; set; }
    [JsonPropertyName("tooltip")]  public JsonTooltipTokens?  Tooltip  { get; set; }
    [JsonPropertyName("tabs")]     public JsonTabsTokens?     Tabs     { get; set; }
    [JsonPropertyName("table")]    public JsonTableTokens?    Table    { get; set; }
    [JsonPropertyName("alert")]    public JsonAlertTokens?    Alert    { get; set; }
    [JsonPropertyName("badge")]    public JsonBadgeTokens?    Badge    { get; set; }
    [JsonPropertyName("chip")]     public JsonChipTokens?     Chip     { get; set; }
    [JsonPropertyName("spinner")]  public JsonSpinnerTokens?  Spinner  { get; set; }
    [JsonPropertyName("progress")] public JsonProgressTokens? Progress { get; set; }
    [JsonPropertyName("header")]   public JsonHeaderTokens?   Header   { get; set; }
    [JsonPropertyName("nav")]      public JsonNavTokens?      Nav      { get; set; }

    [JsonIgnore] public string BtnRadius     => Button?.Radius     ?? "8px";
    [JsonIgnore] public string BtnFontSize   => Button?.FontSize   ?? "0.875rem";
    [JsonIgnore] public string BtnFontWeight => Button?.FontWeight ?? "500";
    [JsonIgnore] public string BtnHeight     => Button?.Height     ?? "36px";
    [JsonIgnore] public string BtnHeightSm   => Button?.HeightSm   ?? "30px";
    [JsonIgnore] public string BtnHeightLg   => Button?.HeightLg   ?? "44px";
    [JsonIgnore] public string BtnPaddingX   => Button?.PaddingX   ?? "16px";
    [JsonIgnore] public string BtnPaddingY   => Button?.PaddingY   ?? "8px";
    [JsonIgnore] public string BtnGap        => Button?.Gap        ?? "8px";
    [JsonIgnore] public string BtnIconSize   => Button?.IconSize   ?? "16px";
    [JsonIgnore] public string BtnMinWidth   => Button?.MinWidth   ?? "88px";

    [JsonIgnore] public string InputRadius       => Input?.Radius       ?? "8px";
    [JsonIgnore] public string InputFontSize     => Input?.FontSize     ?? "0.875rem";
    [JsonIgnore] public string InputHeight       => Input?.Height       ?? "36px";
    [JsonIgnore] public string InputHeightSm     => Input?.HeightSm     ?? "30px";
    [JsonIgnore] public string InputHeightLg     => Input?.HeightLg     ?? "44px";
    [JsonIgnore] public string InputPaddingX     => Input?.PaddingX     ?? "12px";
    [JsonIgnore] public string InputPaddingY     => Input?.PaddingY     ?? "8px";
    [JsonIgnore] public string InputBorderWidth  => Input?.BorderWidth  ?? "1px";
    [JsonIgnore] public string InputIconSize     => Input?.IconSize     ?? "16px";

    [JsonIgnore] public string SelectRadius    => Select?.Radius    ?? Input?.Radius    ?? "8px";
    [JsonIgnore] public string SelectFontSize  => Select?.FontSize  ?? Input?.FontSize  ?? "0.875rem";
    [JsonIgnore] public string SelectHeight    => Select?.Height    ?? Input?.Height    ?? "36px";
    [JsonIgnore] public string SelectHeightSm  => Select?.HeightSm  ?? Input?.HeightSm  ?? "30px";
    [JsonIgnore] public string SelectHeightLg  => Select?.HeightLg  ?? Input?.HeightLg  ?? "44px";
    [JsonIgnore] public string SelectPaddingX  => Select?.PaddingX  ?? "12px";
    [JsonIgnore] public string SelectIconSize  => Select?.IconSize  ?? "16px";

    [JsonIgnore] public string CheckboxSize        => Checkbox?.Size        ?? "18px";
    [JsonIgnore] public string CheckboxSizeSm      => Checkbox?.SizeSm      ?? "14px";
    [JsonIgnore] public string CheckboxSizeLg      => Checkbox?.SizeLg      ?? "22px";
    [JsonIgnore] public string CheckboxRadius      => Checkbox?.Radius      ?? "4px";
    [JsonIgnore] public string CheckboxIconSize    => Checkbox?.IconSize    ?? "12px";
    [JsonIgnore] public string CheckboxBorderWidth => Checkbox?.BorderWidth ?? "1.5px";

    [JsonIgnore] public string SwitchWidth     => Switch?.Width     ?? "40px";
    [JsonIgnore] public string SwitchHeight    => Switch?.Height    ?? "22px";
    [JsonIgnore] public string SwitchThumbSize => Switch?.ThumbSize ?? "18px";
    [JsonIgnore] public string SwitchRadius    => Switch?.Radius    ?? "9999px";
    [JsonIgnore] public string SwitchPadding   => Switch?.Padding   ?? "2px";

    [JsonIgnore] public string CardRadius            => Card?.Radius            ?? "12px";
    [JsonIgnore] public string CardPadding           => Card?.Padding           ?? "16px";
    [JsonIgnore] public string CardPaddingSm         => Card?.PaddingSm         ?? "12px";
    [JsonIgnore] public string CardPaddingLg         => Card?.PaddingLg         ?? "24px";
    [JsonIgnore] public string CardBorderColor       => Card?.BorderColor       ?? "var(--sg-border)";
    [JsonIgnore] public string CardBg                => Card?.Bg                ?? "var(--sg-surface)";
    [JsonIgnore] public string CardHeaderFontWeight  => Card?.HeaderFontWeight  ?? "600";
    [JsonIgnore] public string CardGap               => Card?.Gap               ?? "12px";

    [JsonIgnore] public string ModalRadius        => Modal?.Radius        ?? "12px";
    [JsonIgnore] public string ModalWidth         => Modal?.Width         ?? "500px";
    [JsonIgnore] public string ModalWidthSm       => Modal?.WidthSm       ?? "360px";
    [JsonIgnore] public string ModalWidthLg       => Modal?.WidthLg       ?? "720px";
    [JsonIgnore] public string ModalWidthXl       => Modal?.WidthXl       ?? "960px";
    [JsonIgnore] public string ModalPadding       => Modal?.Padding       ?? "24px";
    [JsonIgnore] public string ModalBackdropBlur  => Modal?.BackdropBlur  ?? "8px";

    [JsonIgnore] public string DropdownRadius         => Dropdown?.Radius         ?? "10px";
    [JsonIgnore] public string DropdownPadding        => Dropdown?.Padding        ?? "6px";
    [JsonIgnore] public string DropdownItemHeight     => Dropdown?.ItemHeight     ?? "32px";
    [JsonIgnore] public string DropdownItemPaddingX   => Dropdown?.ItemPaddingX   ?? "12px";
    [JsonIgnore] public string DropdownItemPaddingY   => Dropdown?.ItemPaddingY   ?? "0";
    [JsonIgnore] public string DropdownGap            => Dropdown?.Gap            ?? "2px";

    [JsonIgnore] public string TooltipMaxWidth   => Tooltip?.MaxWidth   ?? "240px";
    [JsonIgnore] public string TooltipRadius     => Tooltip?.Radius     ?? "6px";
    [JsonIgnore] public string TooltipPadding    => Tooltip?.Padding    ?? "8px 12px";
    [JsonIgnore] public string TooltipFontSize   => Tooltip?.FontSize   ?? "0.8125rem";
    [JsonIgnore] public string TooltipArrowSize  => Tooltip?.ArrowSize  ?? "6px";

    [JsonIgnore] public string TabsIndicatorHeight => Tabs?.IndicatorHeight ?? "2px";
    [JsonIgnore] public string TabsRadius          => Tabs?.Radius          ?? "6px";
    [JsonIgnore] public string TabsHeight          => Tabs?.Height          ?? "40px";
    [JsonIgnore] public string TabsPaddingX        => Tabs?.PaddingX        ?? "12px";
    [JsonIgnore] public string TabsPaddingY        => Tabs?.PaddingY        ?? "0";
    [JsonIgnore] public string TabsGap             => Tabs?.Gap             ?? "4px";

    [JsonIgnore] public string TableRadius             => Table?.Radius             ?? "8px";
    [JsonIgnore] public string TableHeaderFontWeight   => Table?.HeaderFontWeight   ?? "600";
    [JsonIgnore] public string TableRowHeight          => Table?.RowHeight          ?? "44px";
    [JsonIgnore] public string TableRowHeightSm        => Table?.RowHeightSm        ?? "32px";
    [JsonIgnore] public string TableHeaderHeight       => Table?.HeaderHeight       ?? "40px";
    [JsonIgnore] public string TableCellPaddingX       => Table?.CellPaddingX       ?? "12px";
    [JsonIgnore] public string TableCellPaddingY       => Table?.CellPaddingY       ?? "0";
    [JsonIgnore] public string TableBorderWidth        => Table?.BorderWidth        ?? "1px";

    [JsonIgnore] public string AlertRadius     => Alert?.Radius     ?? "10px";
    [JsonIgnore] public string AlertPadding    => Alert?.Padding    ?? "12px 16px";
    [JsonIgnore] public string AlertPaddingSm  => Alert?.PaddingSm  ?? "8px 12px";
    [JsonIgnore] public string AlertIconSize   => Alert?.IconSize   ?? "20px";
    [JsonIgnore] public string AlertGap        => Alert?.Gap        ?? "12px";

    [JsonIgnore] public string BadgeRadius     => Badge?.Radius     ?? "9999px";
    [JsonIgnore] public string BadgeHeight     => Badge?.Height     ?? "20px";
    [JsonIgnore] public string BadgeHeightSm   => Badge?.HeightSm   ?? "16px";
    [JsonIgnore] public string BadgeHeightLg   => Badge?.HeightLg   ?? "24px";
    [JsonIgnore] public string BadgePaddingX   => Badge?.PaddingX   ?? "8px";
    [JsonIgnore] public string BadgeFontSize   => Badge?.FontSize   ?? "0.75rem";
    [JsonIgnore] public string BadgeFontWeight => Badge?.FontWeight ?? "600";

    [JsonIgnore] public string ChipRadius     => Chip?.Radius     ?? "9999px";
    [JsonIgnore] public string ChipHeight     => Chip?.Height     ?? "28px";
    [JsonIgnore] public string ChipHeightSm   => Chip?.HeightSm   ?? "22px";
    [JsonIgnore] public string ChipHeightLg   => Chip?.HeightLg   ?? "34px";
    [JsonIgnore] public string ChipPaddingX   => Chip?.PaddingX   ?? "10px";
    [JsonIgnore] public string ChipGap        => Chip?.Gap        ?? "6px";
    [JsonIgnore] public string ChipIconSize   => Chip?.IconSize   ?? "14px";

    [JsonIgnore] public string SpinnerSize        => Spinner?.Size        ?? "20px";
    [JsonIgnore] public string SpinnerSizeSm      => Spinner?.SizeSm      ?? "14px";
    [JsonIgnore] public string SpinnerSizeLg      => Spinner?.SizeLg      ?? "32px";
    [JsonIgnore] public string SpinnerBorderWidth => Spinner?.BorderWidth ?? "2px";
    [JsonIgnore] public string SpinnerTrackOpacity=> Spinner?.TrackOpacity?? "0.2";

    [JsonIgnore] public string ProgressHeight          => Progress?.Height          ?? "8px";
    [JsonIgnore] public string ProgressHeightSm        => Progress?.HeightSm        ?? "4px";
    [JsonIgnore] public string ProgressHeightLg        => Progress?.HeightLg        ?? "12px";
    [JsonIgnore] public string ProgressRadius          => Progress?.Radius          ?? "9999px";
    [JsonIgnore] public string ProgressIndicatorRadius => Progress?.IndicatorRadius ?? "9999px";

    [JsonIgnore] public string HeaderBg    => Header?.Bg ?? "var(--sg-bg)";
    [JsonIgnore] public string HeaderFg    => Header?.Fg ?? "var(--sg-fg)";
    [JsonIgnore] public string NavBg       => Nav?.Bg       ?? "var(--sg-bg-subtle)";
    [JsonIgnore] public string NavFg       => Nav?.Fg       ?? "var(--sg-fg-subtle)";
    [JsonIgnore] public string NavActiveBg => Nav?.ActiveBg ?? "var(--sg-color-primary-subtle)";
    [JsonIgnore] public string NavActiveFg => Nav?.ActiveFg ?? "var(--sg-color-primary)";
    [JsonIgnore] public string NavItemHeight   => Nav?.ItemHeight   ?? "36px";
    [JsonIgnore] public string NavItemPaddingX => Nav?.ItemPaddingX ?? "12px";
}

public sealed class JsonButtonTokens
{
    [JsonPropertyName("radius")]     public string Radius     { get; set; } = "8px";
    [JsonPropertyName("fontSize")]   public string FontSize   { get; set; } = "0.875rem";
    [JsonPropertyName("fontWeight")] public string FontWeight { get; set; } = "500";
    [JsonPropertyName("height")]     public string Height     { get; set; } = "36px";
    [JsonPropertyName("heightSm")]   public string HeightSm   { get; set; } = "30px";
    [JsonPropertyName("heightLg")]   public string HeightLg   { get; set; } = "44px";
    [JsonPropertyName("paddingX")]   public string PaddingX   { get; set; } = "16px";
    [JsonPropertyName("paddingY")]   public string PaddingY   { get; set; } = "8px";
    [JsonPropertyName("gap")]        public string Gap        { get; set; } = "8px";
    [JsonPropertyName("iconSize")]   public string IconSize   { get; set; } = "16px";
    [JsonPropertyName("minWidth")]   public string MinWidth   { get; set; } = "88px";
}

public sealed class JsonInputTokens
{
    [JsonPropertyName("radius")]      public string Radius      { get; set; } = "8px";
    [JsonPropertyName("fontSize")]    public string FontSize    { get; set; } = "0.875rem";
    [JsonPropertyName("height")]      public string Height      { get; set; } = "36px";
    [JsonPropertyName("heightSm")]    public string HeightSm    { get; set; } = "30px";
    [JsonPropertyName("heightLg")]    public string HeightLg    { get; set; } = "44px";
    [JsonPropertyName("paddingX")]    public string PaddingX    { get; set; } = "12px";
    [JsonPropertyName("paddingY")]    public string PaddingY    { get; set; } = "8px";
    [JsonPropertyName("borderWidth")] public string BorderWidth { get; set; } = "1px";
    [JsonPropertyName("iconSize")]    public string IconSize    { get; set; } = "16px";
}

public sealed class JsonSelectTokens
{
    [JsonPropertyName("radius")]   public string Radius   { get; set; } = "8px";
    [JsonPropertyName("fontSize")] public string FontSize { get; set; } = "0.875rem";
    [JsonPropertyName("height")]   public string Height   { get; set; } = "36px";
    [JsonPropertyName("heightSm")] public string HeightSm { get; set; } = "30px";
    [JsonPropertyName("heightLg")] public string HeightLg { get; set; } = "44px";
    [JsonPropertyName("paddingX")] public string PaddingX { get; set; } = "12px";
    [JsonPropertyName("iconSize")] public string IconSize { get; set; } = "16px";
}

public sealed class JsonCheckboxTokens
{
    [JsonPropertyName("size")]        public string Size        { get; set; } = "18px";
    [JsonPropertyName("sizeSm")]      public string SizeSm      { get; set; } = "14px";
    [JsonPropertyName("sizeLg")]      public string SizeLg      { get; set; } = "22px";
    [JsonPropertyName("radius")]      public string Radius      { get; set; } = "4px";
    [JsonPropertyName("iconSize")]    public string IconSize    { get; set; } = "12px";
    [JsonPropertyName("borderWidth")] public string BorderWidth { get; set; } = "1.5px";
}

public sealed class JsonSwitchTokens
{
    [JsonPropertyName("width")]     public string Width     { get; set; } = "40px";
    [JsonPropertyName("height")]    public string Height    { get; set; } = "22px";
    [JsonPropertyName("thumbSize")] public string ThumbSize { get; set; } = "18px";
    [JsonPropertyName("radius")]    public string Radius    { get; set; } = "9999px";
    [JsonPropertyName("padding")]   public string Padding   { get; set; } = "2px";
}

public sealed class JsonCardTokens
{
    [JsonPropertyName("radius")]          public string Radius          { get; set; } = "12px";
    [JsonPropertyName("padding")]         public string Padding         { get; set; } = "16px";
    [JsonPropertyName("paddingSm")]       public string PaddingSm       { get; set; } = "12px";
    [JsonPropertyName("paddingLg")]       public string PaddingLg       { get; set; } = "24px";
    [JsonPropertyName("borderColor")]     public string BorderColor     { get; set; } = "var(--sg-border)";
    [JsonPropertyName("bg")]              public string Bg              { get; set; } = "var(--sg-surface)";
    [JsonPropertyName("headerFontWeight")]public string HeaderFontWeight{ get; set; } = "600";
    [JsonPropertyName("gap")]             public string Gap             { get; set; } = "12px";
}

public sealed class JsonModalTokens
{
    [JsonPropertyName("radius")]       public string Radius       { get; set; } = "12px";
    [JsonPropertyName("width")]        public string Width        { get; set; } = "500px";
    [JsonPropertyName("widthSm")]      public string WidthSm      { get; set; } = "360px";
    [JsonPropertyName("widthLg")]      public string WidthLg      { get; set; } = "720px";
    [JsonPropertyName("widthXl")]      public string WidthXl      { get; set; } = "960px";
    [JsonPropertyName("padding")]      public string Padding      { get; set; } = "24px";
    [JsonPropertyName("backdropBlur")] public string BackdropBlur { get; set; } = "8px";
}

public sealed class JsonDropdownTokens
{
    [JsonPropertyName("radius")]       public string Radius       { get; set; } = "10px";
    [JsonPropertyName("padding")]      public string Padding      { get; set; } = "6px";
    [JsonPropertyName("itemHeight")]   public string ItemHeight   { get; set; } = "32px";
    [JsonPropertyName("itemPaddingX")] public string ItemPaddingX { get; set; } = "12px";
    [JsonPropertyName("itemPaddingY")] public string ItemPaddingY { get; set; } = "0";
    [JsonPropertyName("gap")]          public string Gap          { get; set; } = "2px";
}

public sealed class JsonTooltipTokens
{
    [JsonPropertyName("maxWidth")]  public string MaxWidth  { get; set; } = "240px";
    [JsonPropertyName("radius")]    public string Radius    { get; set; } = "6px";
    [JsonPropertyName("padding")]   public string Padding   { get; set; } = "8px 12px";
    [JsonPropertyName("fontSize")]  public string FontSize  { get; set; } = "0.8125rem";
    [JsonPropertyName("arrowSize")] public string ArrowSize { get; set; } = "6px";
}

public sealed class JsonTableTokens
{
    [JsonPropertyName("radius")]          public string Radius          { get; set; } = "8px";
    [JsonPropertyName("headerFontWeight")]public string HeaderFontWeight{ get; set; } = "600";
    [JsonPropertyName("rowHeight")]       public string RowHeight       { get; set; } = "44px";
    [JsonPropertyName("rowHeightSm")]     public string RowHeightSm     { get; set; } = "32px";
    [JsonPropertyName("headerHeight")]    public string HeaderHeight    { get; set; } = "40px";
    [JsonPropertyName("cellPaddingX")]    public string CellPaddingX    { get; set; } = "12px";
    [JsonPropertyName("cellPaddingY")]    public string CellPaddingY    { get; set; } = "0";
    [JsonPropertyName("borderWidth")]     public string BorderWidth     { get; set; } = "1px";
}

public sealed class JsonTabsTokens
{
    [JsonPropertyName("indicatorHeight")] public string IndicatorHeight { get; set; } = "2px";
    [JsonPropertyName("radius")]          public string Radius          { get; set; } = "6px";
    [JsonPropertyName("height")]          public string Height          { get; set; } = "40px";
    [JsonPropertyName("paddingX")]        public string PaddingX        { get; set; } = "12px";
    [JsonPropertyName("paddingY")]        public string PaddingY        { get; set; } = "0";
    [JsonPropertyName("gap")]             public string Gap             { get; set; } = "4px";
}

public sealed class JsonAlertTokens
{
    [JsonPropertyName("radius")]    public string Radius    { get; set; } = "10px";
    [JsonPropertyName("padding")]   public string Padding   { get; set; } = "12px 16px";
    [JsonPropertyName("paddingSm")] public string PaddingSm { get; set; } = "8px 12px";
    [JsonPropertyName("iconSize")]  public string IconSize  { get; set; } = "20px";
    [JsonPropertyName("gap")]       public string Gap       { get; set; } = "12px";
}

public sealed class JsonBadgeTokens
{
    [JsonPropertyName("radius")]     public string Radius     { get; set; } = "9999px";
    [JsonPropertyName("height")]     public string Height     { get; set; } = "20px";
    [JsonPropertyName("heightSm")]   public string HeightSm   { get; set; } = "16px";
    [JsonPropertyName("heightLg")]   public string HeightLg   { get; set; } = "24px";
    [JsonPropertyName("paddingX")]   public string PaddingX   { get; set; } = "8px";
    [JsonPropertyName("fontSize")]   public string FontSize   { get; set; } = "0.75rem";
    [JsonPropertyName("fontWeight")] public string FontWeight { get; set; } = "600";
}

public sealed class JsonChipTokens
{
    [JsonPropertyName("radius")]   public string Radius   { get; set; } = "9999px";
    [JsonPropertyName("height")]   public string Height   { get; set; } = "28px";
    [JsonPropertyName("heightSm")] public string HeightSm { get; set; } = "22px";
    [JsonPropertyName("heightLg")] public string HeightLg { get; set; } = "34px";
    [JsonPropertyName("paddingX")] public string PaddingX { get; set; } = "10px";
    [JsonPropertyName("gap")]      public string Gap      { get; set; } = "6px";
    [JsonPropertyName("iconSize")] public string IconSize { get; set; } = "14px";
}

public sealed class JsonSpinnerTokens
{
    [JsonPropertyName("size")]         public string Size         { get; set; } = "20px";
    [JsonPropertyName("sizeSm")]       public string SizeSm       { get; set; } = "14px";
    [JsonPropertyName("sizeLg")]       public string SizeLg       { get; set; } = "32px";
    [JsonPropertyName("borderWidth")]  public string BorderWidth  { get; set; } = "2px";
    [JsonPropertyName("trackOpacity")] public string TrackOpacity { get; set; } = "0.2";
}

public sealed class JsonProgressTokens
{
    [JsonPropertyName("height")]          public string Height          { get; set; } = "8px";
    [JsonPropertyName("heightSm")]        public string HeightSm        { get; set; } = "4px";
    [JsonPropertyName("heightLg")]        public string HeightLg        { get; set; } = "12px";
    [JsonPropertyName("radius")]          public string Radius          { get; set; } = "9999px";
    [JsonPropertyName("indicatorRadius")] public string IndicatorRadius { get; set; } = "9999px";
}

public sealed class JsonHeaderTokens
{
    [JsonPropertyName("bg")] public string Bg { get; set; } = "var(--sg-bg)";
    [JsonPropertyName("fg")] public string Fg { get; set; } = "var(--sg-fg)";
}

public sealed class JsonNavTokens
{
    [JsonPropertyName("bg")]          public string Bg          { get; set; } = "var(--sg-bg-subtle)";
    [JsonPropertyName("fg")]          public string Fg          { get; set; } = "var(--sg-fg-subtle)";
    [JsonPropertyName("activeBg")]    public string ActiveBg    { get; set; } = "var(--sg-color-primary-subtle)";
    [JsonPropertyName("activeFg")]    public string ActiveFg    { get; set; } = "var(--sg-color-primary)";
    [JsonPropertyName("itemHeight")]  public string ItemHeight  { get; set; } = "36px";
    [JsonPropertyName("itemPaddingX")]public string ItemPaddingX{ get; set; } = "12px";
}
