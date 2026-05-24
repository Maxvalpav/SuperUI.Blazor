using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>Configuration options for 2GIS MapGL.</summary>
public class Sg2GisMapOptions
{
    /// <summary>API key for 2GIS MapGL.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Initial centre longitude. Default 37.618 (Moscow).</summary>
    public double CenterLon { get; set; } = 37.618;

    /// <summary>Initial centre latitude. Default 55.751 (Moscow).</summary>
    public double CenterLat { get; set; } = 55.751;

    /// <summary>Initial zoom level. Default 13.</summary>
    public double Zoom { get; set; } = 13;

    /// <summary>Initial rotation in degrees. Default 0.</summary>
    public double Rotation { get; set; } = 0;

    /// <summary>Initial pitch in degrees (0-45). Default 0.</summary>
    public double Pitch { get; set; } = 0;

    /// <summary>Show scale control. Default <c>true</c>.</summary>
    public bool ShowScaleControl { get; set; } = true;

    /// <summary>Show zoom control. Default <c>true</c>.</summary>
    public bool ShowZoomControl { get; set; } = true;

    /// <summary>Theme for the map. Default "light".</summary>
    public string Theme { get; set; } = "light";

    /// <summary>Language for labels. Default "ru".</summary>
    public string Lang { get; set; } = "ru";
}

/// <summary>Arguments for 2GIS map view changed event.</summary>
public class Sg2GisMapViewChangedEventArgs : SgMapViewChangedEventArgs
{
    public double Rotation { get; set; }
    public double Pitch { get; set; }
}
