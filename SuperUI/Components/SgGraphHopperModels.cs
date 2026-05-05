namespace SuperUI.Components;

// ── Vehicle profiles ──────────────────────────────────────────────────────────

/// <summary>Routing vehicle profile.</summary>
public enum SgGhProfile
{
    Car,
    Bike,
    Foot,
    Hike,
    Mtb,
    Racingbike,
    Scooter,
    Truck,
    SmallTruck,
}

// ── Request ───────────────────────────────────────────────────────────────────

/// <summary>A waypoint (lat/lon) for the routing request.</summary>
public class SgGhWaypoint
{
    public double? Latitude  { get; set; }
    public double? Longitude { get; set; }
    public string? Label     { get; set; }
}

/// <summary>Full routing request.</summary>
public class SgGhRouteRequest
{
    /// <summary>Ordered list of waypoints (min 2).</summary>
    public List<SgGhWaypoint> Waypoints { get; set; } = new();

    /// <summary>Vehicle profile. Default Car.</summary>
    public SgGhProfile Profile { get; set; } = SgGhProfile.Car;

    /// <summary>Request alternative routes (up to 3).</summary>
    public bool Alternatives { get; set; } = false;

    /// <summary>Include turn-by-turn instructions.</summary>
    public bool Instructions { get; set; } = true;

    /// <summary>Locale for instruction text (e.g. "ru", "en").</summary>
    public string Locale { get; set; } = "ru";

    /// <summary>Optimise waypoint order (TSP). Default false.</summary>
    public bool Optimize { get; set; } = false;
}

// ── Response ──────────────────────────────────────────────────────────────────

/// <summary>A single route path returned by GraphHopper.</summary>
public class SgGhRoute
{
    /// <summary>Total distance in metres.</summary>
    public double DistanceMeters { get; set; }

    /// <summary>Total travel time in milliseconds.</summary>
    public long TimeMs { get; set; }

    /// <summary>Decoded polyline coordinates.</summary>
    public List<SgMapCoord> Points { get; set; } = new();

    /// <summary>Turn-by-turn instructions.</summary>
    public List<SgGhInstruction> Instructions { get; set; } = new();

    /// <summary>Bounding box [minLon, minLat, maxLon, maxLat].</summary>
    public double[]? Bbox { get; set; }

    // ── Computed helpers ──────────────────────────────────────────────────────

    /// <summary>Distance formatted as "X.X km" or "X m".</summary>
    public string DistanceFormatted =>
        DistanceMeters >= 1000
            ? $"{DistanceMeters / 1000:F1} км"
            : $"{(int)DistanceMeters} м";

    /// <summary>Duration formatted as "Xч Xмин" or "X мин".</summary>
    public string DurationFormatted
    {
        get
        {
            var ts = TimeSpan.FromMilliseconds(TimeMs);
            return ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours}ч {ts.Minutes}мин"
                : $"{ts.Minutes} мин";
        }
    }
}

/// <summary>A single turn-by-turn instruction.</summary>
public class SgGhInstruction
{
    public string Text        { get; set; } = string.Empty;
    public double Distance    { get; set; }
    public long   Time        { get; set; }
    public int    Sign        { get; set; }
    public int    Interval0   { get; set; }
    public int    Interval1   { get; set; }

    /// <summary>Icon character for the turn sign.</summary>
    public string SignIcon => Sign switch
    {
        -3 => "↰",   // sharp left
        -2 => "←",   // left
        -1 => "↖",   // slight left
         0 => "↑",   // straight
         1 => "↗",   // slight right
         2 => "→",   // right
         3 => "↱",   // sharp right
         4 => "🏁",  // finish
         5 => "⬆",   // via reached
         6 => "↩",   // roundabout
        -7 => "↰",   // keep left
         7 => "↱",   // keep right
        _ =>  "•",
    };

    public string DistanceFormatted =>
        Distance >= 1000 ? $"{Distance / 1000:F1} км" : $"{(int)Distance} м";
}

/// <summary>Result returned from <see cref="SgGraphHopper.RouteAsync"/>.</summary>
public class SgGhResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public List<SgGhRoute> Routes { get; set; } = new();
    public SgGhRoute? Best => Routes.Count > 0 ? Routes[0] : null;
}

// ── Event args ────────────────────────────────────────────────────────────────

/// <summary>Fired when a route is successfully calculated.</summary>
public class SgGhRouteEventArgs
{
    public SgGhResult Result { get; set; } = new();
    public SgGhRouteRequest Request { get; set; } = new();
}
