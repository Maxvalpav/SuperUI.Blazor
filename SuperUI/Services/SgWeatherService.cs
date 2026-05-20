using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using SuperUI.Models;

namespace SuperUI.Services;

public class SgWeatherService
{
    private readonly HttpClient _http;
    private const string ForecastUrl = "https://api.open-meteo.com/v1/forecast";
    private const string ArchiveUrl = "https://archive.open-meteo.com/v1/archive";
    private const string AirQualityUrl = "https://air-quality.open-meteo.com/v1/air-quality";
    private const string MarineUrl = "https://marine-api.open-meteo.com/v1/marine";
    private const string GeocodingUrl = "https://geocoding-api.open-meteo.com/v1/search";
    private const string ElevationUrl = "https://api.open-meteo.com/v1/elevation";
    private const string FloodUrl = "https://flood-api.open-meteo.com/v1/flood";

    public SgWeatherService(HttpClient http)
    {
        _http = http;
    }

    public async Task<SgWeatherForecast?> GetForecastAsync(double lat, double lon, int forecastDays = 7)
    {
        var url = $"{ForecastUrl}?latitude={lat.ToString(CultureInfo.InvariantCulture)}&longitude={lon.ToString(CultureInfo.InvariantCulture)}&current=temperature_2m,relative_humidity_2m,apparent_temperature,is_day,weather_code,wind_speed_10m&hourly=temperature_2m,weather_code,precipitation_probability&daily=weather_code,temperature_2m_max,temperature_2m_min,sunrise,sunset,uv_index_max&timezone=auto&forecast_days={forecastDays}";
        return await FetchAsync<SgWeatherForecast>(url);
    }

    public async Task<SgWeatherForecast?> GetHistoricalAsync(double lat, double lon, DateTime start, DateTime end)
    {
        var url = $"{ArchiveUrl}?latitude={lat.ToString(CultureInfo.InvariantCulture)}&longitude={lon.ToString(CultureInfo.InvariantCulture)}&start_date={start:yyyy-MM-dd}&end_date={end:yyyy-MM-dd}&hourly=temperature_2m,precipitation&timezone=auto";
        return await FetchAsync<SgWeatherForecast>(url);
    }

    public async Task<SgWeatherForecast?> GetAirQualityAsync(double lat, double lon)
    {
        var url = $"{AirQualityUrl}?latitude={lat.ToString(CultureInfo.InvariantCulture)}&longitude={lon.ToString(CultureInfo.InvariantCulture)}&hourly=pm10,pm2_5,no2&timezone=auto";
        return await FetchAsync<SgWeatherForecast>(url);
    }

    public async Task<SgWeatherForecast?> GetMarineAsync(double lat, double lon)
    {
        var url = $"{MarineUrl}?latitude={lat.ToString(CultureInfo.InvariantCulture)}&longitude={lon.ToString(CultureInfo.InvariantCulture)}&hourly=wave_height,wave_direction,wave_period&timezone=auto";
        return await FetchAsync<SgWeatherForecast>(url);
    }

    public async Task<SgGeocodingResponse?> SearchCityAsync(string name, int count = 5)
    {
        var url = $"{GeocodingUrl}?name={Uri.EscapeDataString(name)}&count={count}&language=ru&format=json";
        return await FetchAsync<SgGeocodingResponse>(url);
    }

    public async Task<SgWeatherForecast?> GetFloodAsync(double lat, double lon)
    {
        var url = $"{FloodUrl}?latitude={lat}&longitude={lon}&daily=river_discharge&timezone=auto";
        return await FetchAsync<SgWeatherForecast>(url);
    }

    public async Task<SgWeatherForecast?> GetClimateAsync(double lat, double lon, DateTime start, DateTime end)
    {
        var url = $"https://climate-api.open-meteo.com/v1/climate?latitude={lat}&longitude={lon}&start_date={start:yyyy-MM-dd}&end_date={end:yyyy-MM-dd}&models=CMIP6_ensemble&daily=temperature_2m_mean&timezone=auto";
        return await FetchAsync<SgWeatherForecast>(url);
    }

    public async Task<SgWeatherForecast?> GetEnsembleAsync(double lat, double lon)
    {
        var url = $"https://ensemble-api.open-meteo.com/v1/ensemble?latitude={lat}&longitude={lon}&hourly=temperature_2m&models=icon_seamless&timezone=auto";
        return await FetchAsync<SgWeatherForecast>(url);
    }

    private async Task<T?> FetchAsync<T>(string url) where T : class
    {
        try
        {
            return await _http.GetFromJsonAsync<T>(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching data from {url}: {ex.Message}");
            return null;
        }
    }

    public string GetWeatherDescription(int code)
    {
        return code switch
        {
            0 => "Ясно",
            1 or 2 or 3 => "Преимущественно ясно",
            45 or 48 => "Туман",
            51 or 53 or 55 => "Морось",
            61 or 63 or 65 => "Дождь",
            71 or 73 or 75 => "Снег",
            77 => "Снежная крупа",
            80 or 81 or 82 => "Ливень",
            85 or 86 => "Снегопад",
            95 => "Гроза",
            96 or 99 => "Гроза с градом",
            _ => "Неизвестно"
        };
    }
    
    public string GetWeatherIcon(int code, bool isDay)
    {
        return code switch
        {
            0 => isDay ? "☀️" : "🌙",
            1 or 2 or 3 => isDay ? "🌤️" : "☁️",
            45 or 48 => "🌫️",
            51 or 53 or 55 => "🌦️",
            61 or 63 or 65 => "🌧️",
            71 or 73 or 75 => "❄️",
            80 or 81 or 82 => "🌧️",
            95 or 96 or 99 => "⛈️",
            _ => "❓"
        };
    }
}
