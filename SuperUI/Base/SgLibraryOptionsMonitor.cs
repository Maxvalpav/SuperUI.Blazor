// SuperUI/Base/SgLibraryOptionsMonitor.cs
// NEW: Поддержка IOptionsMonitor<T> для горячей перезагрузки конфигурации
// Позволяет изменять настройки библиотеки без перезапуска приложения

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace SuperUI.Base;

/// <summary>
/// Сервис для доступа к <see cref="SgLibraryOptions"/> с hot-config поддержкой.
/// Использует <see cref="IOptionsMonitor{TOptions}"/> — конфигурация обновляется без перезапуска.
/// </summary>
public interface ISgLibraryOptionsService
{
    /// <summary>Текущие настройки.</summary>
    SgLibraryOptions Current { get; }

    /// <summary>Подписаться на изменения конфигурации.</summary>
    IDisposable OnChange(Action<SgLibraryOptions> listener);
}

/// <summary>
/// Реализация через IOptionsMonitor — поддерживает hot-reload из appsettings.json.
/// </summary>
public sealed class SgLibraryOptionsService : ISgLibraryOptionsService, IDisposable
{
    private readonly IOptionsMonitor<SgLibraryOptions> _monitor;
    private readonly List<IDisposable> _changeRegistrations = [];

    public SgLibraryOptionsService(IOptionsMonitor<SgLibraryOptions> monitor)
        => _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));

    public SgLibraryOptions Current => _monitor.CurrentValue;

    public IDisposable OnChange(Action<SgLibraryOptions> listener)
    {
        var reg = _monitor.OnChange(listener);
        _changeRegistrations.Add(reg);
        return reg;
    }

    public void Dispose()
    {
        foreach (var reg in _changeRegistrations)
            try { reg.Dispose(); } catch { }
        _changeRegistrations.Clear();
    }
}

// ── DI Extension ─────────────────────────────────────────────────────────────

public static class SgLibraryOptionsExtensions
{
    /// <summary>
    /// Зарегистрировать SgLibraryOptions с поддержкой IOptionsMonitor (hot-config).
    /// </summary>
    /// <example>
    /// builder.Services.AddSuperUIOptions(builder.Configuration);
    /// </example>
    public static IServiceCollection AddSuperUIOptions(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.Configure<SgLibraryOptions>(
            configuration.GetSection("SuperUI"));

        services.AddSingleton<ISgLibraryOptionsService, SgLibraryOptionsService>();
        return services;
    }

    /// <summary>Настроить опции напрямую (без файла конфигурации).</summary>
    public static IServiceCollection AddSuperUIOptions(
        this IServiceCollection services,
        Action<SgLibraryOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<ISgLibraryOptionsService, SgLibraryOptionsService>();
        return services;
    }
}
