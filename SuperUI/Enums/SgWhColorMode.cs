namespace SuperUI.Enums;

/// <summary>Visual colour mode for the warehouse grid.</summary>
public enum SgWhColorMode
{
    /// <summary>Colour by cell status.</summary>
    Status = 0,
    /// <summary>Colour by ABC classification.</summary>
    Abc = 1,
    /// <summary>Uniform colour (one colour for all cells).</summary>
    Uniform = 2,
    /// <summary>Colour by % occupancy (0–100 %).</summary>
    Occupancy = 3,
    /// <summary>Colour by turnover rate (30-day turns).</summary>
    Turnover = 4,
    /// <summary>Colour by temperature regime.</summary>
    Temperature = 5,
    /// <summary>Colour by zone type.</summary>
    Zone = 6
}
