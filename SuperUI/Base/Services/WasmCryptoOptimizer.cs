// SuperUI/Base/Services/WasmCryptoOptimizer.cs
//
// Оптимизатор криптографических операций для WASM.
// Делегирует вычисления в JS (через Web Crypto API) вместо медленного managed-кода.
// На WASM SHA256 через managed может быть в 5-10x медленнее Web Crypto API.

using System.Runtime.CompilerServices;
using Microsoft.JSInterop;

namespace SuperUI.Base.Services;

/// <summary>
/// Оптимизатор криптографических операций для WASM.
/// Делегирует вычисления в JS (через Web Crypto API) вместо медленного managed-кода.
/// </summary>
public interface ICryptoOptimizer
{
    /// <summary>Вычислить SHA-256 через Web Crypto API (на порядок быстрее на WASM).</summary>
    ValueTask<string> ComputeSha256Async(string input, CancellationToken ct = default);

    /// <summary>Сгенерировать UUID v4 через crypto.randomUUID().</summary>
    ValueTask<string> GenerateUuidAsync(CancellationToken ct = default);
}

/// <summary>
/// Реализация крипто-оптимизатора.
/// На WASM делегирует в Web Crypto API через JSInterop.
/// На Server использует managed .NET криптографию.
/// </summary>
public sealed class WasmCryptoOptimizer : ICryptoOptimizer
{
    private readonly IJSRuntime _js;

    public WasmCryptoOptimizer(IJSRuntime js)
    {
        _js = js ?? throw new ArgumentNullException(nameof(js));
    }

    public async ValueTask<string> ComputeSha256Async(string input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (!OperatingSystem.IsBrowser())
        {
            // Server-side — используем managed SHA256
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hash = System.Security.Cryptography.SHA256.HashData(bytes);
            return Convert.ToHexStringLower(hash);
        }

        return await _js.InvokeAsync<string>(
            "superui.crypto.sha256", ct, input);
    }

    public async ValueTask<string> GenerateUuidAsync(CancellationToken ct = default)
    {
        if (!OperatingSystem.IsBrowser())
            return Guid.NewGuid().ToString();

        return await _js.InvokeAsync<string>(
            "superui.crypto.randomUUID", ct);
    }
}