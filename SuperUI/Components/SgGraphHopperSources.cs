namespace SuperUI.Components;

/// <summary>
/// Configuration for the GraphHopper Routing API.
/// </summary>
/// <remarks>
/// Free tier: https://graphhopper.com/dashboard/#/register — 500 req/day.
/// Self-hosted: set <see cref="BaseUrl"/> to your instance, leave <see cref="ApiKey"/> empty.
/// </remarks>
public sealed class SgGraphHopperSources
{
    /// <summary>
    /// GraphHopper API base URL.
    /// Default: GraphHopper public API.
    /// For self-hosted: "http://localhost:8989"
    /// </summary>
    public string BaseUrl { get; set; } = "https://graphhopper.com/api/1";

    /// <summary>
    /// API key for the GraphHopper cloud service.
    /// Not required for self-hosted instances.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>Request timeout. Default 30 s.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
