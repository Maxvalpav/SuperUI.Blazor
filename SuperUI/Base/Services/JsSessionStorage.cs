// SuperUI/Base/Services/JsSessionStorage.cs
// НОВЫЙ: реализация ISessionStorage через JS interop
// Совместимо с WASM и Server (проверяет prerendering)
using System.Text.Json;
using Microsoft.JSInterop;

namespace SuperUI.Base.Services;

/// <summary>
/// Реализация ISessionStorage через sessionStorage браузера.
/// Безопасна при prerendering — возвращает default если JS недоступен.
/// Работает на Blazor WASM и Blazor Server (интерактивный режим).
/// </summary>
public sealed class JsSessionStorage : ISessionStorage, IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly IPrerendingDetector _prerendingDetector;

    // Кэшируем модуль — ленивая инициализация
    private IJSObjectReference? _module;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private volatile bool _disposed;

    // JSON опции — camelCase, игнорируем null
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public JsSessionStorage(IJSRuntime js, IPrerendingDetector prerendingDetector)
    {
        _js = js;
        _prerendingDetector = prerendingDetector;
    }

    /// <summary>
    /// Получить значение из sessionStorage.
    /// Возвращает default(T) при prerendering, JS ошибках, или отсутствии ключа.
    /// </summary>
    public async Task<T?> GetItemAsync<T>(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (_prerendingDetector.IsPrerendering || _disposed) return default;

        try
        {
            // Используем встроенный JS interop без внешнего модуля
            // sessionStorage.getItem(key) возвращает string | null
            var json = await _js.InvokeAsync<string?>("sessionStorage.getItem", key);
            if (json is null) return default;

            // Десериализуем из JSON
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JSDisconnectedException) { return default; }
        catch (TaskCanceledException) { return default; }
        catch (OperationCanceledException) { return default; }
        catch (JSException) { return default; } // sessionStorage может быть заблокирован (приватный режим)
        catch (JsonException) { return default; } // повреждённые данные
        catch (ObjectDisposedException) { return default; }
    }

    /// <summary>
    /// Сохранить значение в sessionStorage.
    /// Пропускает при prerendering.
    /// </summary>
    public async Task SetItemAsync<T>(string key, T value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (_prerendingDetector.IsPrerendering || _disposed) return;

        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await _js.InvokeVoidAsync("sessionStorage.setItem", key, json);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSException) { }
        catch (ObjectDisposedException) { }
    }

    /// <summary>
    /// Удалить ключ из sessionStorage.
    /// </summary>
    public async Task RemoveItemAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (_prerendingDetector.IsPrerendering || _disposed) return;

        try
        {
            await _js.InvokeVoidAsync("sessionStorage.removeItem", key);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (JSException) { }
        catch (ObjectDisposedException) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_module is not null)
        {
            try { await _module.DisposeAsync(); } catch { }
            _module = null;
        }
        _lock.Dispose();
    }
}