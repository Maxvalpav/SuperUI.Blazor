using System.Text;
using System.Xml.Serialization;

namespace SuperUI.Services.Data;

/// <summary>
/// Service for interacting with Central Bank of Russia (CBR) Data Web Services.
/// </summary>
public class SgCbrService
{
    private readonly HttpClient _http;

    public SgCbrService(HttpClient http)
    {
        _http = http;
        // CBR uses windows-1251 encoding for XML
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Gets daily exchange rates.
    /// </summary>
    /// <param name="date">Optional date. If null, current rates are returned.</param>
    public async Task<SgCbrDailyRates?> GetDailyRatesAsync(DateTime? date = null)
    {
        var url = "https://www.cbr.ru/scripts/XML_daily.asp";
        if (date.HasValue)
        {
            url += $"?date_req={date.Value:dd/MM/yyyy}";
        }

        return await FetchAndDeserializeAsync<SgCbrDailyRates>(url);
    }

    /// <summary>
    /// Gets historical exchange rates for a specific currency.
    /// </summary>
    /// <param name="fromDate">Start date.</param>
    /// <param name="toDate">End date.</param>
    /// <param name="valuteId">CBR internal currency ID (e.g. R01235 for USD).</param>
    public async Task<SgCbrDynamicRates?> GetDynamicRatesAsync(DateTime fromDate, DateTime toDate, string valuteId)
    {
        var url = $"https://www.cbr.ru/scripts/XML_dynamic.asp?date_req1={fromDate:dd/MM/yyyy}&date_req2={toDate:dd/MM/yyyy}&VAL_NM_RQ={valuteId}";
        return await FetchAndDeserializeAsync<SgCbrDynamicRates>(url);
    }

    /// <summary>
    /// Gets precious metals prices.
    /// </summary>
    public async Task<SgCbrMetals?> GetMetalsAsync(DateTime fromDate, DateTime toDate)
    {
        var url = $"https://www.cbr.ru/scripts/xml_metall.asp?date_req1={fromDate:dd/MM/yyyy}&date_req2={toDate:dd/MM/yyyy}";
        return await FetchAndDeserializeAsync<SgCbrMetals>(url);
    }

    private async Task<T?> FetchAndDeserializeAsync<T>(string url) where T : class
    {
        try
        {
            // Try different proxies and formats
            var result = await TryFetchWithProxies(url);
            if (result != null)
            {
                return DeserializeXml<T>(result);
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SgCbrService] Fatal error fetching {url}: {ex.Message}");
            return null;
        }
    }

    private async Task<string?> TryFetchWithProxies(string targetUrl)
    {
        // 1. Сначала пробуем прямой запрос (быстро, без задержки)
        try
        {
            var result = await FetchStringAsync(targetUrl);
            if (IsValidXml(result)) return result;
        }
        catch { }

        // 2. Все прокси запускаем параллельно — берём первый успешный
        var proxies = new[]
        {
            $"https://api.allorigins.win/raw?url={Uri.EscapeDataString(targetUrl)}",
            $"https://corsproxy.io/?{Uri.EscapeDataString(targetUrl)}",
            $"https://api.codetabs.com/v1/proxy?url={Uri.EscapeDataString(targetUrl)}",
            $"https://shcors.vercel.app/api?url={Uri.EscapeDataString(targetUrl)}",
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var tasks = proxies.Select(async proxy =>
        {
            try
            {
                var r = await FetchStringAsync(proxy);
                return IsValidXml(r) ? r : null;
            }
            catch { return null; }
        }).ToList();

        // Ждём первый успешный результат
        while (tasks.Count > 0)
        {
            var completed = await Task.WhenAny(tasks);
            tasks.Remove(completed);
            var result = await completed;
            if (result != null) return result;
        }

        // 3. Fallback: AllOrigins JSON wrapper
        try
        {
            var json = await _http.GetStringAsync(
                $"https://api.allorigins.win/get?url={Uri.EscapeDataString(targetUrl)}");
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var content = doc.RootElement.GetProperty("contents").GetString();
            if (IsValidXml(content)) return content;
        }
        catch { }

        return null;
    }

    private bool IsValidXml(string? xml) => !string.IsNullOrWhiteSpace(xml) && xml.Trim().StartsWith("<");

    private async Task<string?> FetchStringAsync(string url)
    {
        var bytes = await _http.GetByteArrayAsync(url);
        return Encoding.GetEncoding("windows-1251").GetString(bytes);
    }

    private T? DeserializeXml<T>(string xml) where T : class
    {
        if (string.IsNullOrWhiteSpace(xml) || !xml.Trim().StartsWith("<")) return null;
        
        var serializer = new XmlSerializer(typeof(T));
        using var reader = new StringReader(xml);
        return (T?)serializer.Deserialize(reader);
    }
}
