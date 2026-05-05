namespace SuperUI.Components;

// ── Map type ──────────────────────────────────────────────────────────────────

public enum SgGoogleMapType { Roadmap, Satellite, Hybrid, Terrain }

// ── Options ───────────────────────────────────────────────────────────────────

public class SgGoogleMapOptions
{
    public double CenterLat  { get; set; } = 55.751;
    public double CenterLon  { get; set; } = 37.618;
    public int    Zoom       { get; set; } = 10;
    public SgGoogleMapType MapTypeId { get; set; } = SgGoogleMapType.Roadmap;
    public bool   ShowControls   { get; set; } = true;
    public bool   ShowStreetView { get; set; } = false;
    /// <summary>"auto" | "cooperative" | "greedy" | "none"</summary>
    public string GestureHandling { get; set; } = "auto";
    /// <summary>Optional JSON styles array (Google Maps styling).</summary>
    public string? Styles { get; set; }
}

// ── Sources ───────────────────────────────────────────────────────────────────

public class SgGoogleMapSources
{
    /// <summary>Google Maps JavaScript API key. Required for production.</summary>
    public string? ApiKey { get; set; }
}

// ── Marker ────────────────────────────────────────────────────────────────────

public class SgGoogleMapMarker
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

public class SgGoogleMapMarkerClickEventArgs
{
    public string  MarkerId    { get; set; } = string.Empty;
    public string? Title       { get; set; }
    public string? Description { get; set; }
    public double  Latitude    { get; set; }
    public double  Longitude   { get; set; }
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

public class SgMapRouteStep
{
    public string Instruction { get; set; } = string.Empty;
    public string Distance    { get; set; } = string.Empty;
    public string Duration    { get; set; } = string.Empty;
}
