// Файл: Services/ComponentOptionsProvider.cs

namespace SuperUI.Services;

/// <summary>
/// Централизованный провайдер настроек компонентов.
/// Позволяет задать глобальные дефолты для всех компонентов одного типа.
/// 
/// ПРИМЕНЕНИЕ:
/// services.AddSuperUI(o => {
///   o.Components.For&lt;SgButtonOptions&gt;(opts => opts.DefaultVariant = SgVariant.Primary);
///   o.Components.For&lt;SgInputOptions&gt;(opts => opts.DefaultSize = SgSize.Md);
/// });
/// </summary>
public interface IComponentOptionsProvider
{
    TOptions GetOptions<TOptions>() where TOptions : class, new();
}

public sealed class ComponentOptionsProvider : IComponentOptionsProvider
{
    private readonly Dictionary<Type, object> _options = new();

    public void Configure<TOptions>(Action<TOptions> configure) where TOptions : class, new()
    {
        if (!_options.TryGetValue(typeof(TOptions), out var existing))
        {
            existing = new TOptions();
            _options[typeof(TOptions)] = existing;
        }
        configure((TOptions)existing);
    }

    public TOptions GetOptions<TOptions>() where TOptions : class, new()
    {
        if (_options.TryGetValue(typeof(TOptions), out var opts))
            return (TOptions)opts;
        return new TOptions(); // дефолтные настройки
    }
}

/// <summary>Опции для конкретного компонента через каскадный параметр.</summary>
public sealed class SgComponentOptions
{
    public SgSize DefaultSize { get; set; } = SgSize.Md;
    public SgVariant DefaultVariant { get; set; } = SgVariant.Default;
    public bool DisableAnimations { get; set; }
    public bool ReducedMotion { get; set; }
}

public enum SgSize { Xs, Sm, Md, Lg, Xl }
public enum SgVariant { Default, Primary, Secondary, Success, Warning, Error, Info }
