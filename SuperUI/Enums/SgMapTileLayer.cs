namespace SuperUI.Enums;

/// <summary>Standard base-tile layer for the generic SgMap.</summary>
public enum SgMapTileLayer
{
    /// <summary>OpenStreetMap standard tiles.</summary>
    OpenStreetMap = 0,
    /// <summary>OpenStreetMap hot / humanitarian style.</summary>
    OpenStreetMapHot = 1,
    /// <summary>Satellite imagery (Esri Maxar).</summary>
    EsriSatellite = 2,
    /// <summary>Topographical / relief contours.</summary>
    EsriTopographic = 3,
    /// <summary>Custom tile URL defined by the user.</summary>
    Custom = 4,
    /// <summary>CartoDB Positron (light, minimal).</summary>
    CartoPositron = 5,
    /// <summary>CartoDB Dark Matter (dark).</summary>
    CartoDarkMatter = 6,
    /// <summary>Stamen Toner — high-contrast B&amp;W.</summary>
    StamenToner = 7
}
