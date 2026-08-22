using System.Collections.Concurrent;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SuperUI.Services.Data;

/// <summary>
/// Service for interacting with Central Bank of Russia (CBR) Data Web Services.
/// </summary>
public class SgCbrService
{
    private static int _encodingProviderRegistered;
    private static readonly ConcurrentDictionary<Type, XmlSerializer> _serializerCache = new();
    private readonly HttpClient _http;
    private readonly ILogger<SgCbrService> _logger;

    public SgCbrService(HttpClient http) : this(http, null) { }

    public SgCbrService(HttpClient http, ILogger<SgCbrService>? logger)
    {
        _http = http;
        _logger = logger ?? NullLogger<SgCbrService>.Instance;
        if (Interlocked.Exchange(ref _encodingProviderRegistered, 1) == 0)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<SgCbrDailyRates?> GetDailyRatesAsync(DateTime? date = null, CancellationToken ct = default)
    {
        var url = "https://www.cbr.ru/scripts/XML_daily.asp";
        if (date.HasValue) url += $"?date_req={date.Value:dd/MM/yyyy}";
        return await FetchAndDeserializeAsync<SgCbrDailyRates>(url, ct);
    }

    public async Task<SgCbrDynamicRates?> GetDynamicRatesAsync(DateTime fromDate, DateTime toDate, string valuteId, CancellationToken ct = default)
    {
        var url = $"https://www.cbr.ru/scripts/XML_dynamic.asp?date_req1={fromDate:dd/MM/yyyy}&date_req2={toDate:dd/MM/yyyy}&VAL_NM_RQ={valuteId}";
        return await FetchAndDeserializeAsync<SgCbrDynamicRates>(url, ct);
    }

    public async Task<SgCbrMetals?> GetMetalsAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var url = $"https://www.cbr.ru/scripts/xml_metall.asp?date_req1={fromDate:dd/MM/yyyy}&date_req2={toDate:dd/MM/yyyy}";
        return await FetchAndDeserializeAsync<SgCbrMetals>(url, ct);
    }

    private async Task<T?> FetchAndDeserializeAsync<T>(string url, CancellationToken ct) where T : class
    {
        try
        {
            var result = await TryFetchWithProxies(url, ct);
            if (result != null) return DeserializeXml<T>(result);
            return null;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (!IsCritical(ex))
        {
            _logger.LogError(ex, "Fatal error fetching {Url}", url);
            return null;
        }
    }

    private async Task<string?> TryFetchWithProxies(string targetUrl, CancellationToken outerCt)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(outerCt);
        linked.CancelAfter(TimeSpan.FromSeconds(30));
        var ct = linked.Token;

        try
        {
            var result = await FetchStringAsync(targetUrl, ct);
            if (IsValidXml(result)) return result;
        }
        catch (OperationCanceledException) when (outerCt.IsCancellationRequested) { throw; }
        catch (Exception ex) when (!IsCritical(ex)) { _logger.LogDebug(ex, "Direct fetch failed for {Url}", targetUrl); }

        var proxies = new[]
        {
            $"https://api.allorigins.win/raw?url={Uri.EscapeDataString(targetUrl)}",
            $"https://corsproxy.io/?{Uri.EscapeDataString(targetUrl)}",
            $"https://api.codetabs.com/v1/proxy?url={Uri.EscapeDataString(targetUrl)}",
            $"https://shcors.vercel.app/api?url={Uri.EscapeDataString(targetUrl)}",
        };

        using var proxyCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        proxyCts.CancelAfter(TimeSpan.FromSeconds(10));
        var pct = proxyCts.Token;

        var tasks = proxies.Select(async proxy =>
        {
            try
            {
                var r = await FetchStringAsync(proxy, pct);
                return IsValidXml(r) ? r : null;
            }
            catch (OperationCanceledException) { return null; }
            catch (Exception ex) when (!IsCritical(ex))
            {
                _logger.LogDebug(ex, "Proxy fetch failed: {Proxy}", proxy);
                return null;
            }
        }).ToList();

        try
        {
            while (tasks.Count > 0)
            {
                var completed = await Task.WhenAny(tasks);
                tasks.Remove(completed);
                var result = await completed;
                if (result != null)
                {
                    proxyCts.Cancel();
                    return result;
                }
            }
        }
        catch (OperationCanceledException) { }

        try
        {
            var json = await _http.GetStringAsync($"https://api.allorigins.win/get?url={Uri.EscapeDataString(targetUrl)}", ct);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var content = doc.RootElement.GetProperty("contents").GetString();
            if (IsValidXml(content)) return content;
        }
        catch (OperationCanceledException) when (outerCt.IsCancellationRequested) { throw; }
        catch (Exception ex) when (!IsCritical(ex)) { _logger.LogDebug(ex, "AllOrigins JSON fallback failed"); }

        return null;
    }

    private bool IsValidXml(string? xml) => !string.IsNullOrWhiteSpace(xml) && xml.Trim().StartsWith("<");

    private async Task<string?> FetchStringAsync(string url, CancellationToken ct)
    {
        var bytes = await _http.GetByteArrayAsync(url, ct);
        return Encoding.GetEncoding("windows-1251").GetString(bytes);
    }

    private T? DeserializeXml<T>(string xml) where T : class
    {
        if (string.IsNullOrWhiteSpace(xml) || !xml.Trim().StartsWith("<")) return null;
        var serializer = _serializerCache.GetOrAdd(typeof(T), static t => new XmlSerializer(t));
        using var reader = new StringReader(xml);
        return (T?)serializer.Deserialize(reader);
    }

    private static bool IsCritical(Exception ex) => ex is OutOfMemoryException or StackOverflowException or ThreadAbortException;
}
