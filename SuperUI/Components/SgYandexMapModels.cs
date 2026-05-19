using SuperUI.Enums;

namespace SuperUI.Components;

// ── Map type ──────────────────────────────────────────────────────────────────
// SgYandexMapType — moved to SuperUI.Enums.SgYandexMapType

// ── Options ───────────────────────────────────────────────────────────────────

public class SgYandexMapOptions
{
    public double CenterLat  { get; set; } = 55.751;
    public double CenterLon  { get; set; } = 37.618;
    public int    Zoom       { get; set; } = 10;
    public SgYandexMapType MapType { get; set; } = SgYandexMapType.Map;
    public bool   ShowControls { get; set; } = true;
    public bool   ShowTraffic  { get; set; } = false;
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

public class SgYandexMapMarker
{
    public string  Id          { get; set; } = string.Empty;
    public double  Latitude    { get; set; }
    public double  Longitude   { get; set; }
    public string? Title       { get; set; }
    public string? Description { get; set; }
    public string? Color       { get; set; }
    public string? Icon        { get; set; }
    public string? Data        { get; set; }
}

// ── Events ────────────────────────────────────────────────────────────────────

public class SgYandexMapMarkerClickEventArgs
{
    public string  MarkerId    { get; set; } = string.Empty;
    public string? Title       { get; set; }
    public string? Description { get; set; }
    public double  Latitude    { get; set; }
    public double  Longitude   { get; set; }
    public string? Data        { get; set; }
}
