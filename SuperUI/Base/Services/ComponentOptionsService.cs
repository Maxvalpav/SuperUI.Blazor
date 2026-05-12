// SuperUI/Base/Services/ComponentOptionsService.cs

using Microsoft.Extensions.Options;
using SuperUI.Components;

namespace SuperUI.Base.Services;

/// <summary>
/// Реализация IComponentOptionsService.
/// Предоставляет доступ к глобальным опциям библиотеки.
/// </summary>
public sealed class ComponentOptionsService : IComponentOptionsService
{
    private readonly SgLibraryOptions _options;

    public ComponentOptionsService(IOptions<SgLibraryOptions> options)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public SgSize DefaultSize => _options.DefaultSize;
    public bool EnableAnimations => _options.AnimationsEnabled;
    public bool EnableAria => _options.EnableAria;
    public string Locale => _options.Locale;
    public int BaseZIndex => _options.BaseZIndex;
    public int ZIndexStep => _options.ZIndexStep;
    public string CssPrefix => _options.CssPrefix;

    public SgLibraryOptions LibraryOptions => _options;

    public TOptions GetOptions<TComponent, TOptions>()
        where TOptions : class, new()
    {
        // В базовой реализации возвращаем дефолтные опции.
        // В расширенной — можно читать из конфигурации per-component.
        return new TOptions();
    }
}
