namespace SuperUI.Components;

public sealed class SgTraceHop
{
    public int HopNumber { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public double PingMs { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Isp { get; set; } = string.Empty;
    public bool IsTimeout { get; set; }
}

public sealed class SgTraceResult
{
    public string Target { get; set; } = string.Empty;
    public List<SgTraceHop> Hops { get; set; } = new();
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public TimeSpan Duration { get; set; }
}
