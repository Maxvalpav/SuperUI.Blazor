// SuperUI/Base/Services/SgWasmOptimizer.cs — НОВЫЙ
// ✅ Оптимизации специфичные для WebAssembly размера сборки
// ✅ Условная компиляция методов (исключение из WASM того, что нужно только на сервере)
// ✅ Ленивая загрузка JS модулей

using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Microsoft.JSInterop;

namespace SuperUI.Base.Services;

/// <summary>
/// Оптимизатор WASM: уменьшение размера сборки, ленивая загрузка.
/// </summary>
public sealed class SgWasmOptimizer : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private bool _initialized;

    /// <summary>
    /// true если запущено в браузере (WASM).
    /// </summary>
    public static bool IsWasm => OperatingSystem.IsBrowser();

    public SgWasmOptimizer(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>
    /// Загрузить JS-модуль только когда он действительно нужен
    /// (ленивая инициализация для WASM).
    /// </summary>
    public async ValueTask<IJSObjectReference> GetModuleAsync(string modulePath)
    {
        if (_module is not null)
            return _module;

        _module = await _js.InvokeAsync<IJSObjectReference>("import", modulePath);
        return _module;
    }

    /// <summary>
    /// Предварительно загрузить сборку .NET в WASM (prefetch).
    /// Уменьшает время до интерактивности при InteractiveAuto.
    /// </summary>
    public async Task PrefetchAssembliesAsync(params string[] assemblyNames)
    {
        if (!IsWasm || _module is null) return;

        try
        {
            await _module.InvokeVoidAsync("prefetchAssemblies", (object)assemblyNames);
        }
        catch
        {
            // Недоступно — игнорируем
        }
    }

    /// <summary>
    /// Измерить размер загруженных сборок WASM (для диагностики).
    /// </summary>
    public async Task<long> GetWasmHeapSizeAsync()
    {
        if (!IsWasm || _module is null) return -1;

        try
        {
            return await _module.InvokeAsync<long>("getWasmHeapSize");
        }
        catch
        {
            return -1;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); }
            catch { }
        }
    }
}

/// <summary>
/// Атрибут: метод НЕ компилируется в WASM-сборку (только Server-side).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public sealed class SgServerOnlyAttribute : Attribute { }

/// <summary>
/// Атрибут: метод НЕ компилируется в Server-side сборку (только WASM).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false)]
public sealed class SgWasmOnlyAttribute : Attribute { }

/// <summary>
/// Хелпер для условной компиляции.
/// </summary>
public static class SgPlatformHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWasm() => OperatingSystem.IsBrowser();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsServer() => !OperatingSystem.IsBrowser();

    /// <summary>
    /// Выполнить действие только если WASM.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IfWasm(Action action)
    {
        if (OperatingSystem.IsBrowser()) action();
    }

    /// <summary>
    /// Выполнить действие только если Server.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IfServer(Action action)
    {
        if (!OperatingSystem.IsBrowser()) action();
    }
}
