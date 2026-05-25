using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace SuperUI.Services.Network;

public class SgTracerouteHop
{
    public int HopIndex { get; set; }
    public string Ip { get; set; } = "";
    public string Hostname { get; set; } = "";
    public double LatencyMs { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
    public string Isp { get; set; } = "";
}

public class SgTracerouteService
{
    private readonly HttpClient _httpClient;

    public SgTracerouteService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Выполняет расширенную симуляцию трассировки до целевого хоста.
    /// Включает DNS-разрешение и динамическое построение маршрута.
    /// </summary>
    public async Task<List<SgTracerouteHop>> TraceAsync(string target)
    {
        var hops = new List<SgTracerouteHop>();
        var random = new Random();
        
        // 1. Get source location (user's real IP location)
        var sourceGeo = await GetGeoIpAsync(""); // Empty string gets current user location in FreeIPAPI
        if (string.IsNullOrEmpty(sourceGeo.Ip))
        {
            sourceGeo.Ip = "192.168.1.1";
            sourceGeo.City = "Moscow";
            sourceGeo.Country = "Russia";
            sourceGeo.Latitude = 55.7558;
            sourceGeo.Longitude = 37.6173;
        }

        // 2. Resolve target IP and GeoIP
        string targetIp = await ResolveTargetAsync(target);
        var targetGeo = await GetGeoIpAsync(targetIp);
        
        // 3. Generate "natural" path
        var path = new List<SgTracerouteHop> { 
            new SgTracerouteHop { Ip = sourceGeo.Ip, City = sourceGeo.City, Country = sourceGeo.Country, Latitude = sourceGeo.Latitude, Longitude = sourceGeo.Longitude }
        };
        
        // Gateway (first hop is usually local)
        path.Insert(0, new SgTracerouteHop { Ip = "192.168.1.1", City = "Local Gateway", Country = "Internal", Latitude = sourceGeo.Latitude, Longitude = sourceGeo.Longitude });

        // Intermediate nodes (transit)
        int intermediateCount = random.Next(2, 5);
        for (int i = 0; i < intermediateCount; i++)
        {
            double ratio = (double)(i + 1) / (intermediateCount + 1);
            // Add some "jitter" to the straight line between source and target
            double lat = sourceGeo.Latitude + (targetGeo.Latitude - sourceGeo.Latitude) * ratio + (random.NextDouble() - 0.5) * 10;
            double lon = sourceGeo.Longitude + (targetGeo.Longitude - sourceGeo.Longitude) * ratio + (random.NextDouble() - 0.5) * 10;
            
            // Invent a realistic transit IP
            string transitIp = $"{random.Next(60, 150)}.{random.Next(10, 100)}.{random.Next(1, 254)}.{random.Next(1, 254)}";
            
            path.Add(new SgTracerouteHop 
            { 
                Ip = transitIp, 
                Latitude = lat, 
                Longitude = lon, 
                City = "Backbone Hub", 
                Country = "Internet Transit",
                Isp = "Tier 1 Provider"
            });
        }

        path.Add(targetGeo); // Final hop

        double cumulativeLatency = 0;
        for (int i = 0; i < path.Count; i++)
        {
            var hop = path[i];
            hop.HopIndex = i + 1;
            
            double stepLatency = random.NextDouble() * 10 + 1; 
            cumulativeLatency += stepLatency;
            
            // Random spike on transit
            if (i > 1 && i < path.Count - 1 && random.Next(0, 10) > 8) cumulativeLatency += random.Next(20, 100);
            
            hop.LatencyMs = cumulativeLatency;

            hops.Add(hop);
            
            // Faster, more natural "step" feel
            await Task.Delay(random.Next(30, 80));
        }

        return hops;
    }

    private async Task<string> ResolveTargetAsync(string target)
    {
        if (IPAddress.TryParse(target, out _)) return target;

        try
        {
            // Используем прокси для разрешения имен через GeoIP API
            var targetUrl = $"https://freeipapi.com/api/json/{target}";
            var proxies = new[]
            {
                targetUrl,
                $"https://api.allorigins.win/raw?url={Uri.EscapeDataString(targetUrl)}",
                $"https://corsproxy.io/?{Uri.EscapeDataString(targetUrl)}"
            };

            foreach (var url in proxies)
            {
                try
                {
                    var response = await _httpClient.GetFromJsonAsync<FreeIpApiResponse>(url);
                    if (response != null && !string.IsNullOrEmpty(response.ipAddress))
                    {
                        return response.ipAddress;
                    }
                }
                catch { }
            }
        }
        catch { }

        return "8.8.8.8"; // Fallback
    }

    private async Task<SgTracerouteHop> GetGeoIpAsync(string ip)
    {
        // 1. Пропускаем локальные и частные IP-адреса
        if (IsPrivateIp(ip))
        {
            return new SgTracerouteHop 
            { 
                Ip = ip, 
                City = "Local Network", 
                Country = "Internal",
                Isp = "Private Infrastructure"
            };
        }

        try
        {
            // 2. Пытаемся получить данные через несколько прокси для обхода CORS
            var targetUrl = $"https://freeipapi.com/api/json/{ip}";
            var proxies = new[]
            {
                $"https://corsproxy.io/?{Uri.EscapeDataString(targetUrl)}",
                $"https://api.allorigins.win/raw?url={Uri.EscapeDataString(targetUrl)}",
                $"https://api.codetabs.com/v1/proxy?url={Uri.EscapeDataString(targetUrl)}",
                targetUrl // Прямой запрос только в конце
            };

            foreach (var url in proxies)
            {
                try
                {
                    // Используем GetStringAsync + Deserialize для лучшего контроля ошибок
                    var json = await _httpClient.GetStringAsync(url);
                    var response = JsonSerializer.Deserialize<FreeIpApiResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (response != null && !string.IsNullOrEmpty(response.ipAddress))
                    {
                        return new SgTracerouteHop
                        {
                            Ip = ip,
                            City = response.cityName ?? "Unknown",
                            Country = response.countryName ?? "Unknown",
                            Isp = "Public Network",
                            Latitude = response.latitude,
                            Longitude = response.longitude
                        };
                    }
                }
                catch { }
            }
        }
        catch { }

        return new SgTracerouteHop { Ip = ip };
    }

    private bool IsPrivateIp(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return true;
        if (ip == "127.0.0.1" || ip == "::1" || ip.ToLower() == "localhost") return true;
        
        if (IPAddress.TryParse(ip, out var address))
        {
            byte[] bytes = address.GetAddressBytes();
            if (bytes.Length == 4) // IPv4
            {
                // 10.0.0.0/8
                if (bytes[0] == 10) return true;
                // 172.16.0.0/12
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                // 192.168.0.0/16
                if (bytes[0] == 192 && bytes[1] == 168) return true;
                // 169.254.0.0/16 (APIPA)
                if (bytes[0] == 169 && bytes[1] == 254) return true;
            }
        }
        
        return false;
    }

    private class FreeIpApiResponse
    {
        public string? ipAddress { get; set; }
        public string? countryName { get; set; }
        public string? cityName { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
    }
}
