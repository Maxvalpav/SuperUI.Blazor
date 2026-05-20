using SuperUI.Enums;

namespace SuperUI.Components;

// ── Tile layer type ───────────────────────────────────────────────────────────
// SgLeafletTileLayer — moved to SuperUI.Enums.SgLeafletTileLayer

// ── Options ───────────────────────────────────────────────────────────────────

/// <summary>Configuration options for <see cref="SgLeaflet"/>.</summary>
public class SgLeafletOptions
{
    /// <summary>Initial centre latitude. Default 55.751 (Moscow).</summary>
    public double CenterLat { get; set; } = 55.751;

    /// <summary>Initial centre longitude. Default 37.618 (Moscow).</summary>
    public double CenterLon { get; set; } = 37.618;

    /// <summary>Initial zoom level (1–20). Default 10.</summary>
    public int Zoom { get; set; } = 10;

    /// <summary>Minimum zoom level. Default 2.</summary>
    public int MinZoom { get; set; } = 2;

    /// <summary>Maximum zoom level. Default 19.</summary>
    public int MaxZoom { get; set; } = 19;

    /// <summary>Base tile layer. Default <see cref="SgLeafletTileLayer.OpenStreetMap"/>.</summary>
    public SgLeafletTileLayer TileLayer { get; set; } = SgLeafletTileLayer.OpenStreetMap;

    /// <summary>Custom XYZ tile URL template (used when <see cref="TileLayer"/> is <see cref="SgLeafletTileLayer.Custom"/>).</summary>
    public string? CustomTileUrl { get; set; }

    /// <summary>Show zoom controls. Default <c>true</c>.</summary>
    public bool ShowZoomControl { get; set; } = true;

    /// <summary>Show scale control. Default <c>true</c>.</summary>
    public bool ShowScaleControl { get; set; } = true;

    /// <summary>Show attribution. Default <c>true</c>.</summary>
    public bool ShowAttribution { get; set; } = true;

    /// <summary>Enable mouse wheel zoom. Default <c>true</c>.</summary>
    public bool MouseWheelZoom { get; set; } = true;

    /// <summary>Show a popup when a marker is clicked. Default <c>true</c>.</summary>
    public bool ShowPopup { get; set; } = true;

    /// <summary>Auto-fit view to all markers on load. Default <c>false</c>.</summary>
    public bool FitToMarkers { get; set; } = false;
}

// ── Sources ───────────────────────────────────────────────────────────────────

/// <summary>CDN source URLs for the Leaflet bundle.</summary>
public sealed class SgLeafletSources
{
    /// <summary>Leaflet JS bundle.</summary>
    public string LeafletScript { get; set; } =
        "https://unpkg.com/leaflet@1.9.4/dist/leaflet.js";

    /// <summary>Leaflet default stylesheet.</summary>
    public string LeafletCss { get; set; } =
        "https://unpkg.com/leaflet@1.9.4/dist/leaflet.css";
}

// ── Marker ────────────────────────────────────────────────────────────────────

/// <summary>A map marker / point feature.</summary>
public class SgLeafletMarker
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Latitude (WGS-84).</summary>
    public double Latitude { get; set; }

    /// <summary>Longitude (WGS-84).</summary>
    public double Longitude { get; set; }

    /// <summary>Tooltip / popup title.</summary>
    public string? Title { get; set; }

    /// <summary>Optional description shown in popup.</summary>
    public string? Description { get; set; }

    /// <summary>Marker colour (CSS colour string). Default accent colour.</summary>
    public string? Color { get; set; }

    /// <summary>Optional icon emoji or single character shown inside the marker.</summary>
    public string? Icon { get; set; }

    /// <summary>Marker size in pixels. Default 32.</summary>
    public int Size { get; set; } = 32;

    /// <summary>Optional JSON data payload.</summary>
    public string? Data { get; set; }
}

// ── Polyline ──────────────────────────────────────────────────────────────────

/// <summary>A polyline (route / path) on the map.</summary>
public class SgLeafletPolyline
{
    public string Id { get; set; } = string.Empty;
    public List<SgLeafletCoord> Coordinates { get; set; } = new();
    public string Color { get; set; } = "#2563eb";
    public double Width { get; set; } = 3;
    public bool Dashed { get; set; } = false;
}

// ── Polygon ───────────────────────────────────────────────────────────────────

/// <summary>A filled polygon on the map.</summary>
public class SgLeafletPolygon
{
    public string Id { get; set; } = string.Empty;
    public List<SgLeafletCoord> Coordinates { get; set; } = new();
    public string FillColor   { get; set; } = "rgba(37,99,235,0.2)";
    public string StrokeColor { get; set; } = "#2563eb";
    public double StrokeWidth { get; set; } = 2;
    public string? Title { get; set; }
}

// ── Coord ─────────────────────────────────────────────────────────────────────

/// <summary>A WGS-84 coordinate pair.</summary>
public class SgLeafletCoord
{
    public double Latitude  { get; set; }
    public double Longitude { get; set; }
}

// ── Circle ────────────────────────────────────────────────────────────────────

/// <summary>A circle on the map.</summary>
public class SgLeafletCircle
{
    public string Id { get; set; } = string.Empty;
    public double Latitude  { get; set; }
    public double Longitude { get; set; }
    /// <summary>Radius in meters.</summary>
    public double Radius { get; set; } = 500;
    public string Color     { get; set; } = "#2563eb";
    public string FillColor { get; set; } = "rgba(37,99,235,0.15)";
    public string? Title { get; set; }
}

// ── Heat point ────────────────────────────────────────────────────────────────

/// <summary>A heat map data point.</summary>
public class SgLeafletHeatPoint
{
    public double Latitude  { get; set; }
    public double Longitude { get; set; }
    /// <summary>Intensity value between 0 and 1.</summary>
    public double Intensity { get; set; } = 1.0;
}

// ── Events ────────────────────────────────────────────────────────────────────

/// <summary>Arguments passed when a marker is clicked.</summary>
public class SgLeafletMarkerClickEventArgs
{
    public string  MarkerId    { get; set; } = string.Empty;
    public string? Title       { get; set; }
    public string? Description { get; set; }
    public double  Latitude    { get; set; }
    public double  Longitude   { get; set; }
    public string? Data        { get; set; }
}

/// <summary>Arguments passed when the map is clicked (not on a marker).</summary>
public class SgLeafletClickEventArgs
{
    public double Latitude  { get; set; }
    public double Longitude { get; set; }
}

/// <summary>Arguments passed when the map view changes.</summary>
public class SgLeafletViewChangedEventArgs
{
    public double CenterLat { get; set; }
    public double CenterLon { get; set; }
    public double Zoom      { get; set; }
}

// ── Route result ──────────────────────────────────────────────────────────────

/// <summary>Single route variant returned by BuildRouteAsync.</summary>
public class SgLeafletRouteVariant
{
    public double  DistanceKm   { get; set; }
    public string? DistanceText { get; set; }
    public double? DurationMin  { get; set; }
    public string? DurationText { get; set; }
    public List<SgLeafletRouteStep> Steps { get; set; } = new();

    public string DistanceFormatted =>
        !string.IsNullOrEmpty(DistanceText) ? DistanceText :
        DistanceKm >= 1 ? $"{DistanceKm:F1} км" : $"{(int)(DistanceKm * 1000)} м";

    public string DurationFormatted =>
        !string.IsNullOrEmpty(DurationText) ? DurationText :
        DurationMin.HasValue ? (DurationMin.Value >= 60
            ? $"{(int)(DurationMin.Value / 60)} ч {(int)(DurationMin.Value % 60)} мин"
            : $"{(int)DurationMin.Value} мин") : "—";
}

/// <summary>Result of BuildRouteAsync.</summary>
public class SgLeafletRouteResult
{
    public bool    Ok            { get; set; }
    public bool    Straight      { get; set; }
    public string? Error         { get; set; }
    public int     SelectedIndex { get; set; }
    public List<SgLeafletRouteVariant> Routes { get; set; } = new();

    public SgLeafletRouteVariant? Best => Routes.Count > 0 ? Routes[SelectedIndex] : null;
    public string DistanceFormatted => Best?.DistanceFormatted ?? "—";
    public string DurationFormatted => Best?.DurationFormatted ?? "—";
    public double? DurationMin      => Best?.DurationMin;
}

public class SgLeafletRouteStep
{
    public string Instruction { get; set; } = string.Empty;
    public string Distance    { get; set; } = string.Empty;
}
