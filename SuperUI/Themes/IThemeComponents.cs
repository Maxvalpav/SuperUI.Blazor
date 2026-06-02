namespace SuperUI.Themes;

/// <summary>
/// Component-specific tokens — fine-grained control for each component.
/// </summary>
public interface IThemeComponents
{
    // Button
    string BtnRadius { get; }
    string BtnFontSize { get; }
    string BtnFontWeight { get; }
    string BtnHeight { get; }
    string BtnHeightSm { get; }
    string BtnHeightLg { get; }
    string BtnPaddingX { get; }
    string BtnPaddingY { get; }
    string BtnGap { get; }
    string BtnIconSize { get; }
    string BtnMinWidth { get; }

    // Input
    string InputRadius { get; }
    string InputFontSize { get; }
    string InputHeight { get; }
    string InputHeightSm { get; }
    string InputHeightLg { get; }
    string InputPaddingX { get; }
    string InputPaddingY { get; }
    string InputBorderWidth { get; }
    string InputIconSize { get; }

    // Select (shares many with Input).
    string SelectRadius { get; }
    string SelectFontSize { get; }
    string SelectHeight { get; }
    string SelectHeightSm { get; }
    string SelectHeightLg { get; }
    string SelectPaddingX { get; }
    string SelectIconSize { get; }

    // Checkbox
    string CheckboxSize { get; }
    string CheckboxSizeSm { get; }
    string CheckboxSizeLg { get; }
    string CheckboxRadius { get; }
    string CheckboxIconSize { get; }
    string CheckboxBorderWidth { get; }

    // Switch
    string SwitchWidth { get; }
    string SwitchHeight { get; }
    string SwitchThumbSize { get; }
    string SwitchRadius { get; }
    string SwitchPadding { get; }

    // Card
    string CardRadius { get; }
    string CardPadding { get; }
    string CardPaddingSm { get; }
    string CardPaddingLg { get; }
    string CardBorderColor { get; }
    string CardBg { get; }
    string CardHeaderFontWeight { get; }
    string CardGap { get; }

    // Modal
    string ModalRadius { get; }
    string ModalWidth { get; }
    string ModalWidthSm { get; }
    string ModalWidthLg { get; }
    string ModalWidthXl { get; }
    string ModalPadding { get; }
    string ModalBackdropBlur { get; }

    // Dropdown
    string DropdownRadius { get; }
    string DropdownPadding { get; }
    string DropdownItemHeight { get; }
    string DropdownItemPaddingX { get; }
    string DropdownItemPaddingY { get; }
    string DropdownGap { get; }

    // Tooltip
    string TooltipMaxWidth { get; }
    string TooltipRadius { get; }
    string TooltipPadding { get; }
    string TooltipFontSize { get; }
    string TooltipArrowSize { get; }

    // Tabs
    string TabsIndicatorHeight { get; }
    string TabsRadius { get; }
    string TabsHeight { get; }
    string TabsPaddingX { get; }
    string TabsPaddingY { get; }
    string TabsGap { get; }

    // Table
    string TableRadius { get; }
    string TableHeaderFontWeight { get; }
    string TableRowHeight { get; }
    string TableRowHeightSm { get; }
    string TableHeaderHeight { get; }
    string TableCellPaddingX { get; }
    string TableCellPaddingY { get; }
    string TableBorderWidth { get; }

    // Alert
    string AlertRadius { get; }
    string AlertPadding { get; }
    string AlertPaddingSm { get; }
    string AlertIconSize { get; }
    string AlertGap { get; }

    // Badge
    string BadgeRadius { get; }
    string BadgeHeight { get; }
    string BadgeHeightSm { get; }
    string BadgeHeightLg { get; }
    string BadgePaddingX { get; }
    string BadgeFontSize { get; }
    string BadgeFontWeight { get; }

    // Chip
    string ChipRadius { get; }
    string ChipHeight { get; }
    string ChipHeightSm { get; }
    string ChipHeightLg { get; }
    string ChipPaddingX { get; }
    string ChipGap { get; }
    string ChipIconSize { get; }

    // Spinner
    string SpinnerSize { get; }
    string SpinnerSizeSm { get; }
    string SpinnerSizeLg { get; }
    string SpinnerBorderWidth { get; }
    string SpinnerTrackOpacity { get; }

    // Progress
    string ProgressHeight { get; }
    string ProgressHeightSm { get; }
    string ProgressHeightLg { get; }
    string ProgressRadius { get; }
    string ProgressIndicatorRadius { get; }

    // Header & Navigation
    string HeaderBg { get; }
    string HeaderFg { get; }
    string NavBg { get; }
    string NavFg { get; }
    string NavActiveBg { get; }
    string NavActiveFg { get; }
    string NavItemHeight { get; }
    string NavItemPaddingX { get; }
}
