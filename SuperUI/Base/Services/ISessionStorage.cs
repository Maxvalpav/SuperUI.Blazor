// SuperUI/Base/Services/ISessionStorage.cs
namespace SuperUI.Base.Services;

/// <summary>
/// Сервис для работы с session storage в браузере.
/// Используется для хранения snapshots компонентов.
/// Работает в WASM и Server (через JS interop, безопасно при prerendering).
/// </summary>
public interface ISessionStorage
{
    Task<T?> GetItemAsync<T>(string key);
    Task SetItemAsync<T>(string key, T value);
    Task RemoveItemAsync(string key);
}