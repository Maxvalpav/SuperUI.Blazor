namespace SuperUI.Enums;

/// <summary>Standard base-tile layer for Leaflet maps.</summary>
public enum SgLeafletTileLayer
{
    /// <summary>OpenStreetMap standard tiles.</summary>
    OpenStreetMap = 0,
    /// <summary>OpenStreetMap hot / humanitarian style.</summary>
    OpenStreetMapHot = 1,
    /// <summary>Stamen Terrain (powered by Stamen and Mapbox).</summary>
    StamenTerrain = 2,
    /// <summary>Stamen Toner — high-contrast B&amp;W.</summary>
    StamenToner = 3,
    /// <summary>Stamen Watercolor — artistic look.</summary>
    StamenWatercolor = 4,
    /// <summary>Mapbox Light v10.</summary>
    MapboxLight = 5,
    /// <summary>Mapbox Dark.</summary>
    MapboxDark = 6,
    /// <summary>Satellite imagery (Esri Maxar).</summary>
    EsriSatellite = 7,
    /// <summary>Topographical / relief contours.</summary>
    EsriTopographic = 8,
    /// <summary>CartoDB Positron (light, minimal).</summary>
    CartoDB_Positron = 9,
    /// <summary>CartoDB Dark Matter (dark theme).</summary>
    CartoDB_DarkMatter = 10,
    /// <summary>Esri World Imagery satellite.</summary>
    Esri_WorldImagery = 11,
    /// <summary>Stamen Toner — high-contrast B&amp;W.</summary>
    Stamen_Toner = 12,
    /// <summary>Custom tile URL defined by the user.</summary>
    Custom = 13
}
