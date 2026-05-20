using SuperUI.Enums;

namespace SuperUI.Components;

// ── Tile layer type ───────────────────────────────────────────────────────────
// SgMapTileLayer — moved to SuperUI.Enums.SgMapTileLayer

// ── Marker ────────────────────────────────────────────────────────────────────

/// <summary>A map marker / point feature.</summary>
public class SgMapMarker
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Longitude (WGS-84).</summary>
    public double Longitude { get; set; }

    /// <summary>Latitude (WGS-84).</summary>
    public double Latitude { get; set; }

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
public class SgMapPolyline
{
    public string Id { get; set; } = string.Empty;
    public List<SgMapCoord> Coordinates { get; set; } = new();
    public string Color { get; set; } = "#2563eb";
    public double Width { get; set; } = 3;
    public bool Dashed { get; set; } = false;
}

/// <summary>A WGS-84 coordinate pair.</summary>
public class SgMapCoord
{
    public double Longitude { get; set; }
    public double Latitude  { get; set; }
}

// ── Polygon ───────────────────────────────────────────────────────────────────

/// <summary>A filled polygon on the map.</summary>
public class SgMapPolygon
{
    public string Id { get; set; } = string.Empty;
    public List<SgMapCoord> Coordinates { get; set; } = new();
    public string FillColor   { get; set; } = "rgba(37,99,235,0.2)";
    public string StrokeColor { get; set; } = "#2563eb";
    public double StrokeWidth { get; set; } = 2;
    public string? Title { get; set; }
}

// ── Options ───────────────────────────────────────────────────────────────────

/// <summary>Configuration options for <see cref="SgMap"/>.</summary>
public class SgMapOptions
{
    /// <summary>Base tile layer. Default <see cref="SgMapTileLayer.OpenStreetMap"/>.</summary>
    public SgMapTileLayer TileLayer { get; set; } = SgMapTileLayer.OpenStreetMap;

    /// <summary>Custom XYZ tile URL template (used when <see cref="TileLayer"/> is <see cref="SgMapTileLayer.Custom"/>).</summary>
    public string? CustomTileUrl { get; set; }

    /// <summary>Initial centre longitude. Default 37.618 (Moscow).</summary>
    public double CenterLon { get; set; } = 37.618;

    /// <summary>Initial centre latitude. Default 55.751 (Moscow).</summary>
    public double CenterLat { get; set; } = 55.751;

    /// <summary>Initial zoom level (1–20). Default 10.</summary>
    public int Zoom { get; set; } = 10;

    /// <summary>Minimum zoom level. Default 2.</summary>
    public int MinZoom { get; set; } = 2;

    /// <summary>Maximum zoom level. Default 19.</summary>
    public int MaxZoom { get; set; } = 19;

    /// <summary>Show zoom controls. Default <c>true</c>.</summary>
    public bool ShowZoomControl { get; set; } = true;

    /// <summary>Show scale line. Default <c>true</c>.</summary>
    public bool ShowScaleLine { get; set; } = true;

    /// <summary>Show attribution. Default <c>true</c>.</summary>
    public bool ShowAttribution { get; set; } = true;

    /// <summary>Enable mouse wheel zoom. Default <c>true</c>.</summary>
    public bool MouseWheelZoom { get; set; } = true;

    /// <summary>Show a popup when a marker is clicked. Default <c>true</c>.</summary>
    public bool ShowPopup { get; set; } = true;

    /// <summary>Auto-fit view to all markers on load. Default <c>false</c>.</summary>
    public bool FitToMarkers { get; set; } = false;
}

// ── Events ────────────────────────────────────────────────────────────────────

/// <summary>Arguments passed when a marker is clicked.</summary>
public class SgMapMarkerClickEventArgs
{
    public string   MarkerId    { get; set; } = string.Empty;
    public string?  Title       { get; set; }
    public string?  Description { get; set; }
    public double   Longitude   { get; set; }
    public double   Latitude    { get; set; }
    public string?  Data        { get; set; }
}

/// <summary>Arguments passed when the map is clicked (not on a marker).</summary>
public class SgMapClickEventArgs
{
    public double Longitude { get; set; }
    public double Latitude  { get; set; }
}

/// <summary>Arguments passed when the map view changes.</summary>
public class SgMapViewChangedEventArgs
{
    public double CenterLon { get; set; }
    public double CenterLat { get; set; }
    public double Zoom      { get; set; }
}
