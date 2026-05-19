namespace SuperUI.Themes;

/// <summary>
/// Primitive values of the theme — color scales, spacing, typography.
/// Not used directly in components.
/// </summary>
public interface IThemePrimitives
{
    string Neutral0 { get; }
    string Neutral50 { get; }
    string Neutral100 { get; }
    string Neutral200 { get; }
    string Neutral300 { get; }
    string Neutral400 { get; }
    string Neutral500 { get; }
    string Neutral600 { get; }
    string Neutral700 { get; }
    string Neutral800 { get; }
    string Neutral900 { get; }

    string Primary50 { get; }
    string Primary100 { get; }
    string Primary200 { get; }
    string Primary300 { get; }
    string Primary400 { get; }
    string Primary500 { get; }
    string Primary600 { get; }
    string Primary700 { get; }
    string Primary800 { get; }
    string Primary900 { get; }

    string Success50 { get; }
    string Success100 { get; }
    string Success500 { get; }
    string Success600 { get; }
    string Success700 { get; }

    string Danger50 { get; }
    string Danger100 { get; }
    string Danger500 { get; }
    string Danger600 { get; }
    string Danger700 { get; }

    string Warning50 { get; }
    string Warning100 { get; }
    string Warning500 { get; }
    string Warning600 { get; }

    string Info50 { get; }
    string Info100 { get; }
    string Info500 { get; }
    string Info600 { get; }

    string FontSans { get; }
    string FontMono { get; }
    string FontSerif { get; }

    string RadiusNone { get; }
    string RadiusXs { get; }
    string RadiusSm { get; }
    string RadiusMd { get; }
    string RadiusLg { get; }
    string RadiusXl { get; }
    string Radius2Xl { get; }
    string RadiusFull { get; }
}
