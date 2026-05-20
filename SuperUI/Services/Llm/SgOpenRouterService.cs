using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SuperUI.Services.Llm;

public interface IOpenRouterService
{
    Task<List<SgOpenRouterModel>> GetModelsAsync();
    Task<SgOpenRouterKeyData?> GetKeyInfoAsync(string apiKey);
}

public class SgOpenRouterService : IOpenRouterService
{
    private readonly HttpClient _http;
    private readonly ILogger<SgOpenRouterService>? _logger;

    public SgOpenRouterService(HttpClient http, ILogger<SgOpenRouterService>? logger = null)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<SgOpenRouterModel>> GetModelsAsync()
    {
        try
        {
            var response = await _http.GetFromJsonAsync<SgOpenRouterModelsResponse>("https://openrouter.ai/api/v1/models");
            return response?.Data ?? new();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to fetch OpenRouter models");
            return new();
        }
    }

    public async Task<SgOpenRouterKeyData?> GetKeyInfoAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) return null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://openrouter.ai/api/v1/auth/key");
            request.Headers.Add("Authorization", $"Bearer {apiKey}");
            
            var response = await _http.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SgOpenRouterKeyResponse>();
                return result?.Data;
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to fetch OpenRouter key info");
            return null;
        }
    }
}
