// SuperUI/Base/Services/ComponentOptionsService.cs

using Microsoft.Extensions.Options;

namespace SuperUI.Base.Services;

/// <summary>
/// Реализация IComponentOptionsService.
/// </summary>
public sealed class ComponentOptionsService : IComponentOptionsService
{
    private readonly SgLibraryOptions _options;

    public ComponentOptionsService(IOptions<SgLibraryOptions> options)
    {
        _options = options.Value;
    }

    public SgLibraryOptions LibraryOptions => _options;

    public TOptions GetOptions<TComponent, TOptions>()
        where TOptions : class, new()
    {
        // В базовой реализации возвращаем дефолтные опции.
        // В расширенной — можно читать из конфигурации per-component.
        return new TOptions();
    }
}
