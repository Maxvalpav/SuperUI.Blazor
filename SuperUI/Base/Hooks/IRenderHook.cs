// SuperUI/Base/Hooks/IRenderHook.cs
namespace SuperUI.Base.Hooks;

/// <summary>
/// Хук для контроля рендера компонента.
/// Возвращает <see langword="false"/> → рендер пропускается полностью.
/// </summary>
/// <remarks>
/// Расширяет <see cref="IComponentHook"/> — реализуйте также
/// <see cref="IComponentHook.OnInitialized"/>, <see cref="IComponentHook.OnParametersSet"/>
/// и <see cref="IComponentHook.OnAfterRender"/> при необходимости.
/// </remarks>
public interface IRenderHook : IComponentHook
{
    /// <summary>
    /// Определяет, нужно ли перерисовывать компонент.
    /// </summary>
    /// <returns><see langword="true"/> — рендер разрешён; <see langword="false"/> — пропустить.</returns>
    bool ShouldRender(SgComponentBase component);
}