// SuperUI/Base/SgRenderBoundary.cs
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace SuperUI.Base;

/// <summary>
/// Граница рендера: предотвращает перерисовку <see cref="ChildContent"/>
/// если указанные <see cref="Dependencies"/> не изменились.
/// </summary>
/// <remarks>
/// Аналог React.memo() / shouldComponentUpdate.
///
/// ⚠️ Dependencies сравниваются через <see cref="object.Equals"/> (shallow).
/// Для mutable объектов передавайте неизменяемые snapshot-значения.
/// </remarks>
/// <example>
/// <code>
/// &lt;SgRenderBoundary Dependencies="@(new object[] { _items.Count, _selectedId })"&gt;
///     &lt;ExpensiveComponent /&gt;
/// &lt;/SgRenderBoundary&gt;
/// </code>
/// </example>
public sealed class SgRenderBoundary : ComponentBase
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Зависимости. Перерисовка происходит только при изменении хотя бы одной.
    /// Сравнение — <see cref="object.Equals"/> (shallow, не deep-clone).
    /// </summary>
    [Parameter] public object?[]? Dependencies { get; set; }

    /// <summary>Принудительно разрешить следующий рендер (игнорирует Dependencies).</summary>
    [Parameter] public bool ForceRender { get; set; }

    private object?[]? _prevDependencies;
    // Отдельный флаг: ShouldRender может вызываться несколько раз до OnAfterRender
    private bool _renderOccurred;

    protected override bool ShouldRender()
    {
        if (ForceRender || _prevDependencies is null)
        {
            _renderOccurred = true;
            return true;
        }

        if (Dependencies is null || Dependencies.Length != _prevDependencies.Length)
        {
            _renderOccurred = true;
            return true;
        }

        for (int i = 0; i < Dependencies.Length; i++)
        {
            if (!Equals(Dependencies[i], _prevDependencies[i]))
            {
                _renderOccurred = true;
                return true;
            }
        }

        _renderOccurred = false;
        return false;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        // Обновляем snapshot только если реально был рендер
        if (_renderOccurred && Dependencies is not null)
            _prevDependencies = (object?[])Dependencies.Clone();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
        => builder.AddContent(0, ChildContent);
}
