using SuperUI.Enums;

namespace SuperUI.Components;

// ── Map type ──────────────────────────────────────────────────────────────────
// SgGoogleMapType — moved to SuperUI.Enums.SgGoogleMapType

// ── Options ───────────────────────────────────────────────────────────────────

/// <summary>Configuration options for the Google Map component.</summary>
public class SgGoogleMapOptions
{
    /// <summary>Initial center latitude.</summary>
    public double CenterLat  { get; set; } = 55.751;
    /// <summary>Initial center longitude.</summary>
    public double CenterLon  { get; set; } = 37.618;
    /// <summary>Initial zoom level.</summary>
    public int    Zoom       { get; set; } = 10;
    /// <summary>Map type (roadmap, satellite, hybrid, terrain).</summary>
    public SgGoogleMapType MapTypeId { get; set; } = SgGoogleMapType.Roadmap;
    /// <summary>Whether to show zoom and map type controls.</summary>
    public bool   ShowControls   { get; set; } = true;
    /// <summary>Whether to show the Street View control.</summary>
    public bool   ShowStreetView { get; set; } = false;
    /// <summary>Gesture handling mode: "auto" | "cooperative" | "greedy" | "none".</summary>
    public string GestureHandling { get; set; } = "auto";
    /// <summary>Optional JSON styles array (Google Maps styling).</summary>
    public string? Styles { get; set; }
}

// ── Sources ───────────────────────────────────────────────────────────────────

/// <summary>Source configuration for loading Google Maps JavaScript API.</summary>
public class SgGoogleMapSources
{
    /// <summary>Google Maps JavaScript API key. Required for production.</summary>
    public string? ApiKey { get; set; }
}

// ── Marker ────────────────────────────────────────────────────────────────────

/// <summary>Represents a single marker on a Google Map.</summary>
public class SgGoogleMapMarker
{
    /// <summary>Unique identifier for the marker.</summary>
    public string  Id          { get; set; } = string.Empty;
    /// <summary>Marker latitude (WGS-84).</summary>
    public double  Latitude    { get; set; }
    /// <summary>Marker longitude (WGS-84).</summary>
    public double  Longitude   { get; set; }
    /// <summary>Tooltip or popup title.</summary>
    public string? Title       { get; set; }
    /// <summary>Optional description shown in the info window.</summary>
    public string? Description { get; set; }
    /// <summary>Marker colour (CSS colour string).</summary>
    public string? Color       { get; set; }
    /// <summary>Optional custom icon URL.</summary>
    public string? Icon        { get; set; }
    /// <summary>Optional JSON data payload.</summary>
    public string? Data        { get; set; }
}

// ── Events ────────────────────────────────────────────────────────────────────

/// <summary>Event arguments for Google Map marker click events.</summary>
public class SgGoogleMapMarkerClickEventArgs
{
    /// <summary>ID of the clicked marker.</summary>
    public string  MarkerId    { get; set; } = string.Empty;
    /// <summary>Title of the clicked marker.</summary>
    public string? Title       { get; set; }
    /// <summary>Description of the clicked marker.</summary>
    public string? Description { get; set; }
    /// <summary>Latitude of the clicked marker.</summary>
    public double  Latitude    { get; set; }
    /// <summary>Longitude of the clicked marker.</summary>
    public double  Longitude   { get; set; }
    /// <summary>Optional data payload of the clicked marker.</summary>
    public string? Data        { get; set; }
}

// ── Route result ──────────────────────────────────────────────────────────────

/// <summary>Single route variant.</summary>
public class SgMapRouteVariant
{
    public double  DistanceKm   { get; set; }
    public string? DistanceText { get; set; }
    public double? DurationMin  { get; set; }
    public string? DurationText { get; set; }
    public List<SgMapRouteStep> Steps { get; set; } = new();

    public string DistanceFormatted =>
        !string.IsNullOrEmpty(DistanceText) ? DistanceText :
        DistanceKm >= 1 ? $"{DistanceKm:F1} км" : $"{(int)(DistanceKm * 1000)} м";

    public string DurationFormatted =>
        !string.IsNullOrEmpty(DurationText) ? DurationText :
        DurationMin.HasValue ? (DurationMin.Value >= 60
            ? $"{(int)(DurationMin.Value / 60)} ч {(int)(DurationMin.Value % 60)} мин"
            : $"{(int)DurationMin.Value} мин") : "—";
}

/// <summary>Result of BuildRouteAsync — works for both Google and Yandex maps.</summary>
public class SgMapRouteResult
{
    public bool    Ok            { get; set; }
    public bool    Straight      { get; set; }
    public string? Error         { get; set; }
    public int     SelectedIndex { get; set; }
    public List<SgMapRouteVariant> Routes { get; set; } = new();

    public SgMapRouteVariant? Best => Routes.Count > 0 ? Routes[SelectedIndex] : null;

    // Legacy compat
    public double  DistanceKm   => Best?.DistanceKm   ?? 0;
    public string? DistanceText => Best?.DistanceText;
    public double? DurationMin  => Best?.DurationMin;
    public string? DurationText => Best?.DurationText;
    public List<SgMapRouteStep> Steps => Best?.Steps ?? new();
    public string DistanceFormatted => Best?.DistanceFormatted ?? "—";
    public string DurationFormatted => Best?.DurationFormatted ?? "—";
}

/// <summary>An individual turn-by-turn step in a route.</summary>
public class SgMapRouteStep
{
    /// <summary>Text instruction for this step.</summary>
    public string Instruction { get; set; } = string.Empty;
    /// <summary>Distance description for this step.</summary>
    public string Distance    { get; set; } = string.Empty;
    /// <summary>Duration description for this step.</summary>
    public string Duration    { get; set; } = string.Empty;
}
