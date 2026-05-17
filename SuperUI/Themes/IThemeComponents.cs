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

    // Input
    string InputRadius { get; }
    string InputFontSize { get; }
    string InputHeight { get; }
    string InputHeightSm { get; }
    string InputHeightLg { get; }

    // Card
    string CardRadius { get; }
    string CardPadding { get; }
    string CardBorderColor { get; }
    string CardBg { get; }

    // Modal
    string ModalRadius { get; }

    // Table
    string TableRadius { get; }
    string TableHeaderFontWeight { get; }

    // Tabs
    string TabsIndicatorHeight { get; }

    // Tooltip
    string TooltipMaxWidth { get; }

    // Header & Navigation
    string HeaderBg { get; }
    string HeaderFg { get; }
    string NavBg { get; }
    string NavFg { get; }
    string NavActiveBg { get; }
    string NavActiveFg { get; }
}
