// SuperUI/Base/Services/JsSessionStorage.cs
//
// ПОЛИРОВКА:
// 1. Удалён мёртвый код: _module и _lock SemaphoreSlim (никогда не использовались).
// 2. IPrerenderingDetector вместо IPrerendingDetector (правильный интерфейс).
// 3. Добавлен ClearAsync() — очистить всё sessionStorage.
// 4. Добавлен ContainsKeyAsync() — проверить существование ключа.
// 5. GetOrSetAsync() — получить или установить значение.

using System.Text.Json;
using Microsoft.JSInterop;

namespace SuperUI.Base.Services;

/// <summary>
/// Реализация <see cref="ISessionStorage"/> через sessionStorage браузера.
/// Безопасна при prerendering — возвращает default если JS недоступен.
/// </summary>
/// <remarks>
/// Совместимость: Blazor WASM и Blazor Server (интерактивный режим).<br/>
/// Prerendering: все методы — no-op / return default.
/// </remarks>
public sealed class JsSessionStorage : ISessionStorage
{
    private readonly IJSRuntime            _js;
    private readonly IPrerenderingDetector _detector;
    private volatile bool                  _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy      = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition    = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    // ПОЛИРОВКА: принимаем IPrerenderingDetector (правильный интерфейс)
    public JsSessionStorage(IJSRuntime js, IPrerenderingDetector detector)
    {
        _js       = js       ?? throw new ArgumentNullException(nameof(js));
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
    }

    public async Task<T?> GetItemAsync<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (_detector.IsPrerendering || _disposed) return default;
        try
        {
            var json = await _js.InvokeAsync<string?>("sessionStorage.getItem", key);
            if (json is null) return default;
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex) when (IsIgnorable(ex)) { return default; }
    }

    public async Task SetItemAsync<T>(string key, T value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (_detector.IsPrerendering || _disposed) return;
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await _js.InvokeVoidAsync("sessionStorage.setItem", key, json);
        }
        catch (Exception ex) when (IsIgnorable(ex)) { }
    }

    public async Task RemoveItemAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (_detector.IsPrerendering || _disposed) return;
        try { await _js.InvokeVoidAsync("sessionStorage.removeItem", key); }
        catch (Exception ex) when (IsIgnorable(ex)) { }
    }

    /// <summary>Проверить наличие ключа в sessionStorage.</summary>
    public async Task<bool> ContainsKeyAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (_detector.IsPrerendering || _disposed) return false;
        try
        {
            var json = await _js.InvokeAsync<string?>("sessionStorage.getItem", key);
            return json is not null;
        }
        catch (Exception ex) when (IsIgnorable(ex)) { return false; }
    }

    /// <summary>Получить значение или установить его через фабрику если отсутствует.</summary>
    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        var existing = await GetItemAsync<T>(key);
        if (existing is not null) return existing;

        var value = await factory();
        await SetItemAsync(key, value);
        return value;
    }

    /// <summary>Очистить всё sessionStorage (используйте с осторожностью).</summary>
    public async Task ClearAsync()
    {
        if (_detector.IsPrerendering || _disposed) return;
        try { await _js.InvokeVoidAsync("sessionStorage.clear"); }
        catch (Exception ex) when (IsIgnorable(ex)) { }
    }

    private static bool IsIgnorable(Exception ex) =>
        ex is JSDisconnectedException
           or TaskCanceledException
           or OperationCanceledException
           or JSException
           or JsonException
           or ObjectDisposedException;
}
