// SuperUI/Services/SgStorageService.cs
// Обёртка над localStorage / sessionStorage с типизированным JSON-сериализацией.
// Решает паттерн "каждый сервис пишет свой localStorage.setItem + JsonSerializer".

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace SuperUI.Services;

/// <summary>
/// Тип хранилища браузера.
/// </summary>
public enum SgStorageKind
{
    /// <summary>Сохраняется между сессиями браузера (localStorage).</summary>
    Local,
    /// <summary>Сбрасывается при закрытии вкладки (sessionStorage).</summary>
    Session,
}

/// <summary>
/// Типобезопасная обёртка над <c>localStorage</c> / <c>sessionStorage</c> с JSON-сериализацией.
/// </summary>
/// <remarks>
/// <para>Регистрируется как Scoped: на Blazor Server — один экземпляр на circuit,
/// на WASM — один на сессию.</para>
/// <para>Все методы безопасны для SSR: в режиме prerender они возвращают
/// <c>default</c> / <c>false</c> / ничего не делают.</para>
/// <para>Пример:</para>
/// <code>
/// await Storage.SetAsync("user-pref", new UserPref { Theme = "dark" });
/// var pref = await Storage.GetAsync&lt;UserPref&gt;("user-pref");
/// </code>
/// </remarks>
public sealed class SgStorageService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly JsonSerializerOptions _jsonOptions;
    private int _disposed;

    /// <summary>Создаёт сервис хранилища.</summary>
    public SgStorageService(IJSRuntime js)
    {
        _js = js;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
        };
    }

    // ── Set ───────────────────────────────────────────────────────────────────

    /// <summary>Сохраняет значение <paramref name="value"/> под ключом <paramref name="key"/>.</summary>
    public ValueTask SetAsync<T>(string key, T value, SgStorageKind kind = SgStorageKind.Local)
    {
        if (Volatile.Read(ref _disposed) == 1) return ValueTask.CompletedTask;
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key is empty.", nameof(key));
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        return InvokeSetAsync(key, json, kind);
    }

    /// <summary>Сохраняет строку (без JSON-сериализации).</summary>
    public ValueTask SetStringAsync(string key, string value, SgStorageKind kind = SgStorageKind.Local)
    {
        if (Volatile.Read(ref _disposed) == 1) return ValueTask.CompletedTask;
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Key is empty.", nameof(key));
        return InvokeSetAsync(key, value, kind);
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    /// <summary>Возвращает десериализованное значение или <c>default</c>.</summary>
    public async ValueTask<T?> GetAsync<T>(string key, SgStorageKind kind = SgStorageKind.Local)
    {
        if (Volatile.Read(ref _disposed) == 1) return default;
        if (string.IsNullOrEmpty(key)) return default;
        try
        {
            var json = await InvokeGetAsync(key, kind).ConfigureAwait(false);
            if (string.IsNullOrEmpty(json)) return default;
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (JsonException)  { return default; }
        catch (JSDisconnectedException) { return default; }
        catch (TaskCanceledException)   { return default; }
        catch (ObjectDisposedException) { return default; }
    }

    /// <summary>Возвращает строку (без JSON-десериализации).</summary>
    public ValueTask<string?> GetStringAsync(string key, SgStorageKind kind = SgStorageKind.Local)
    {
        if (Volatile.Read(ref _disposed) == 1) return ValueTask.FromResult<string?>(null);
        if (string.IsNullOrEmpty(key)) return ValueTask.FromResult<string?>(null);
        return InvokeGetAsync(key, kind);
    }

    // ── Remove / Clear ───────────────────────────────────────────────────────

    /// <summary>Удаляет один ключ.</summary>
    public ValueTask RemoveAsync(string key, SgStorageKind kind = SgStorageKind.Local)
    {
        if (Volatile.Read(ref _disposed) == 1 || string.IsNullOrEmpty(key)) return ValueTask.CompletedTask;
        return InvokeRemoveAsync(key, kind);
    }

    /// <summary>Удаляет все ключи с указанным префиксом.</summary>
    public async ValueTask RemoveByPrefixAsync(string prefix, SgStorageKind kind = SgStorageKind.Local)
    {
        if (Volatile.Read(ref _disposed) == 1 || string.IsNullOrEmpty(prefix)) return;
        var keys = await GetKeysAsync(kind).ConfigureAwait(false);
        foreach (var k in keys)
        {
            if (k.StartsWith(prefix, StringComparison.Ordinal))
                await RemoveAsync(k, kind).ConfigureAwait(false);
        }
    }

    /// <summary>Очищает хранилище.</summary>
    public ValueTask ClearAsync(SgStorageKind kind = SgStorageKind.Local)
    {
        if (Volatile.Read(ref _disposed) == 1) return ValueTask.CompletedTask;
        return InvokeClearAsync(kind);
    }

    // ── Has / Keys ───────────────────────────────────────────────────────────

    /// <summary>True, если ключ существует.</summary>
    public async ValueTask<bool> ContainsAsync(string key, SgStorageKind kind = SgStorageKind.Local)
    {
        var v = await GetStringAsync(key, kind).ConfigureAwait(false);
        return v is not null;
    }

    /// <summary>Возвращает все ключи хранилища.</summary>
    public async ValueTask<IReadOnlyList<string>> GetKeysAsync(SgStorageKind kind = SgStorageKind.Local)
    {
        if (Volatile.Read(ref _disposed) == 1) return Array.Empty<string>();
        try
        {
            var method = kind == SgStorageKind.Local ? "localStorageKeys" : "sessionStorageKeys";
            return await _js.InvokeAsync<string[]>(method).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { return Array.Empty<string>(); }
        catch (TaskCanceledException)   { return Array.Empty<string>(); }
        catch (JSException)             { return Array.Empty<string>(); }
        catch (InvalidOperationException) { return Array.Empty<string>(); }
    }

    // ── Private JS invocation ────────────────────────────────────────────────

    private async ValueTask InvokeSetAsync(string key, string value, SgStorageKind kind)
    {
        try
        {
            var method = kind == SgStorageKind.Local ? "localStorage.setItem" : "sessionStorage.setItem";
            await _js.InvokeVoidAsync(method, key, value).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    private async ValueTask<string?> InvokeGetAsync(string key, SgStorageKind kind)
    {
        try
        {
            var method = kind == SgStorageKind.Local ? "localStorage.getItem" : "sessionStorage.getItem";
            return await _js.InvokeAsync<string?>(method, key).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { return null; }
        catch (TaskCanceledException)   { return null; }
        catch (JSException)             { return null; }
        catch (InvalidOperationException) { return null; }
    }

    private async ValueTask InvokeRemoveAsync(string key, SgStorageKind kind)
    {
        try
        {
            var method = kind == SgStorageKind.Local ? "localStorage.removeItem" : "sessionStorage.removeItem";
            await _js.InvokeVoidAsync(method, key).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    private async ValueTask InvokeClearAsync(SgStorageKind kind)
    {
        try
        {
            var method = kind == SgStorageKind.Local ? "localStorage.clear" : "sessionStorage.clear";
            await _js.InvokeVoidAsync(method).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Volatile.Write(ref _disposed, 1);
        return ValueTask.CompletedTask;
    }
}
