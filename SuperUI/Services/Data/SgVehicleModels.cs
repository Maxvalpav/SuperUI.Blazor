namespace SuperUI.Services.Data;

public class SgVehicle
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int UpdateIntervalMs { get; set; } = 5000;
}

public class SgGpsPoint
{
    public double Lat { get; set; }
    public double Lon { get; set; }
    public DateTime Timestamp { get; set; }
}
