// SuperUI/Base/SgRenderBoundary.cs
// НОВЫЙ: декларативный контроль рендера (аналог React.memo)
// Предотвращает лишние рендеры дочерних компонентов

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace SuperUI.Base;

/// <summary>
/// Граница рендера: предотвращает перерисовку ChildContent
/// если указанные параметры не изменились.
/// Аналог React.memo() / shouldComponentUpdate().
/// </summary>
/// <example>
/// <SgRenderBoundary Dependencies="@(new object[] { count, filter })">
///     <HeavyComponent />
/// </SgRenderBoundary>
/// </example>
public sealed class SgRenderBoundary : ComponentBase
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Зависимости: массив объектов. Перерисовка только если хотя бы одна изменилась.
    /// Аналог массива зависимостей useEffect в React.
    /// </summary>
    [Parameter] public object?[]? Dependencies { get; set; }

    /// <summary>Принудительно разрешить следующий рендер.</summary>
    [Parameter] public bool ForceRender { get; set; }

    private object?[]? _prevDependencies;
    private bool _shouldRender = true;

    protected override bool ShouldRender()
    {
        if (ForceRender || _prevDependencies is null)
        {
            _shouldRender = true;
            return true;
        }

        if (Dependencies is null || Dependencies.Length != _prevDependencies.Length)
        {
            _shouldRender = true;
            return true;
        }

        for (int i = 0; i < Dependencies.Length; i++)
        {
            if (!Equals(Dependencies[i], _prevDependencies[i]))
            {
                _shouldRender = true;
                return true;
            }
        }

        _shouldRender = false;
        return false;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (_shouldRender && Dependencies is not null)
            _prevDependencies = (object?[])Dependencies.Clone();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, ChildContent);
    }
}
