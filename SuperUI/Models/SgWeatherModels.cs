using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SuperUI.Models;

public class SgWeatherForecast
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("current")]
    public SgWeatherCurrent? Current { get; set; }

    [JsonPropertyName("daily")]
    public SgWeatherDaily? Daily { get; set; }
    
    [JsonPropertyName("hourly")]
    public SgWeatherHourly? Hourly { get; set; }

    // Marine
    [JsonPropertyName("hourly_units")]
    public Dictionary<string, string>? HourlyUnits { get; set; }

    [JsonPropertyName("daily_units")]
    public Dictionary<string, string>? DailyUnits { get; set; }

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("elevation")]
    public double Elevation { get; set; }
}

public class SgWeatherCurrent
{
    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("temperature_2m")]
    public double Temperature { get; set; }

    [JsonPropertyName("relative_humidity_2m")]
    public int Humidity { get; set; }

    [JsonPropertyName("apparent_temperature")]
    public double ApparentTemperature { get; set; }

    [JsonPropertyName("is_day")]
    public int IsDay { get; set; }

    [JsonPropertyName("weather_code")]
    public int WeatherCode { get; set; }

    [JsonPropertyName("wind_speed_10m")]
    public double WindSpeed { get; set; }
}

public class SgWeatherDaily
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = new();

    [JsonPropertyName("weather_code")]
    public List<int> WeatherCode { get; set; } = new();

    [JsonPropertyName("temperature_2m_max")]
    public List<double> TemperatureMax { get; set; } = new();

    [JsonPropertyName("temperature_2m_min")]
    public List<double> TemperatureMin { get; set; } = new();

    [JsonPropertyName("sunrise")]
    public List<string> Sunrise { get; set; } = new();

    [JsonPropertyName("sunset")]
    public List<string> Sunset { get; set; } = new();

    [JsonPropertyName("uv_index_max")]
    public List<double> UvIndexMax { get; set; } = new();
}

public class SgWeatherHourly
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = new();

    [JsonPropertyName("temperature_2m")]
    public List<double>? Temperature { get; set; }

    [JsonPropertyName("weather_code")]
    public List<int>? WeatherCode { get; set; }

    [JsonPropertyName("precipitation_probability")]
    public List<int>? PrecipitationProbability { get; set; }

    // Air Quality
    [JsonPropertyName("pm2_5")]
    public List<double>? Pm25 { get; set; }

    [JsonPropertyName("pm10")]
    public List<double>? Pm10 { get; set; }

    [JsonPropertyName("no2")]
    public List<double>? No2 { get; set; }

    // Marine
    [JsonPropertyName("wave_height")]
    public List<double>? WaveHeight { get; set; }

    [JsonPropertyName("wave_direction")]
    public List<double>? WaveDirection { get; set; }

    [JsonPropertyName("wave_period")]
    public List<double>? WavePeriod { get; set; }
}

public class SgGeocodingResponse
{
    [JsonPropertyName("results")]
    public List<SgGeocodingResult>? Results { get; set; }
}

public class SgGeocodingResult
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("elevation")]
    public double Elevation { get; set; }

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("admin1")]
    public string? Admin1 { get; set; }
}

public class SgElevationResponse
{
    [JsonPropertyName("elevation")]
    public List<double> Elevation { get; set; } = new();
}
