// SuperUI/Base/SgRenderBoundary.cs
//
// ДОРАБОТКА: Полная реализация SgRenderBoundary.
// Изолирует перерисовку — дочернее содержимое рендерится только при явном вызове Refresh().
// Полезно для оптимизации крупных деревьев (например, SgDataGrid rows).

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace SuperUI.Base;

/// <summary>
/// Граница рендеринга: дочернее содержимое рендерится только при вызове <see cref="Refresh"/>.
/// Используется для изоляции рендера "горячих" поддеревьев.
/// </summary>
/// <example>
/// <code>
/// &lt;SgRenderBoundary @ref="_boundary"&gt;
///     &lt;ExpensiveComponent Data="@_data" /&gt;
/// &lt;/SgRenderBoundary&gt;
///
/// // Обновить только при реальных изменениях:
/// _boundary.Refresh();
/// </code>
/// </example>
public sealed class SgRenderBoundary : ComponentBase
{
    private bool _shouldRender = true;

    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Разрешить один рендер дочернего содержимого.
    /// После рендера автоматически блокирует следующие рендеры.
    /// </summary>
    public void Refresh()
    {
        _shouldRender = true;
        StateHasChanged();
    }

    protected override bool ShouldRender()
    {
        if (!_shouldRender) return false;
        _shouldRender = false; // Сброс: следующий рендер будет заблокирован
        return true;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, ChildContent);
    }
}
