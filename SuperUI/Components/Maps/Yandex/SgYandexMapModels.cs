using SuperUI.Enums;

namespace SuperUI.Components;

// ── Map type ──────────────────────────────────────────────────────────────────
// SgYandexMapType — moved to SuperUI.Enums.SgYandexMapType

// ── Options ───────────────────────────────────────────────────────────────────

/// <summary>Configuration options for a Yandex Map component.</summary>
public class SgYandexMapOptions
{
    /// <summary>Initial center latitude.</summary>
    public double CenterLat  { get; set; } = 55.751;
    /// <summary>Initial center longitude.</summary>
    public double CenterLon  { get; set; } = 37.618;
    /// <summary>Initial zoom level.</summary>
    public int    Zoom       { get; set; } = 10;
    /// <summary>Map type (map, satellite, hybrid).</summary>
    public SgYandexMapType MapType { get; set; } = SgYandexMapType.Map;
    /// <summary>Whether to show zoom and map type controls.</summary>
    public bool   ShowControls { get; set; } = true;
    /// <summary>Whether to show traffic layer.</summary>
    public bool   ShowTraffic  { get; set; } = false;
    /// <summary>Whether to show the ruler tool.</summary>
    public bool   ShowRuler    { get; set; } = false;
}

// ── Sources ───────────────────────────────────────────────────────────────────

public class SgYandexMapSources
{
    /// <summary>Yandex Maps API key. Get at developer.tech.yandex.ru</summary>
    public string? ApiKey { get; set; }
    /// <summary>Locale, e.g. "ru_RU", "en_US".</summary>
    public string Lang { get; set; } = "ru_RU";
}

// ── Marker ────────────────────────────────────────────────────────────────────

/// <summary>Represents a single marker on a Yandex Map.</summary>
public class SgYandexMapMarker
{
    /// <summary>Unique identifier for the marker.</summary>
    public string  Id          { get; set; } = string.Empty;
    /// <summary>Marker latitude (WGS-84).</summary>
    public double  Latitude    { get; set; }
    /// <summary>Marker longitude (WGS-84).</summary>
    public double  Longitude   { get; set; }
    /// <summary>Tooltip or popup title.</summary>
    public string? Title       { get; set; }
    /// <summary>Optional description shown in the balloon.</summary>
    public string? Description { get; set; }
    /// <summary>Marker colour (CSS colour string).</summary>
    public string? Color       { get; set; }
    /// <summary>Optional custom icon URL.</summary>
    public string? Icon        { get; set; }
    /// <summary>Optional JSON data payload.</summary>
    public string? Data        { get; set; }
}

// ── Events ────────────────────────────────────────────────────────────────────

/// <summary>Event arguments for Yandex Map marker click events.</summary>
public class SgYandexMapMarkerClickEventArgs
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
