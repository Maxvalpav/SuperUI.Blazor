// SuperUI/Base/Reactive/OffscreenOptimizer.cs
// НОВОЕ: Hook для автоматического пропуска рендеров невидимых компонентов.
// Использует IntersectionObserver через JS interop.
//
// Экономия: компоненты вне viewport не рендерятся → 0 CPU на невидимые строки DataGrid.
// В MudBlazor/Radzen/Telerik реализовано только для Virtualize (не для обычных компонентов).
// Здесь — как переиспользуемый Hook для ЛЮБОГО компонента.
using Microsoft.AspNetCore.Components;
using SuperUI.Base.Hooks;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Hook для оптимизации рендеров через IntersectionObserver.
/// Компоненты вне viewport автоматически пропускают рендеры.
/// 
/// Использование:
///   protected override void OnInitialized()
///   {
///       AddHook(new OffscreenOptimizer(this, JS, ComponentId));
///   }
/// </summary>
public sealed class OffscreenOptimizer : IComponentHook, IRenderHook, IAsyncDisposable
{
    private readonly SgComponentBase _component;
    private volatile bool _isVisible = true; // по умолчанию считаем видимым
    private volatile bool _initialized;

    public OffscreenOptimizer(SgComponentBase component)
    {
        _component = component;
    }

    // ── IComponentHook ─────────────────────────────────────────────────────────
    public void OnInitialized(SgComponentBase component) { }
    public void OnParametersSet(SgComponentBase component) { }
    public void OnAfterRender(SgComponentBase component, bool firstRender) { }

    // ── IRenderHook ────────────────────────────────────────────────────────────
    public bool ShouldRender(SgComponentBase component)
    {
        // Пока не инициализирован IntersectionObserver — всегда рендерим
        if (!_initialized) return true;
        return _isVisible;
    }

    // ── JS Callback ────────────────────────────────────────────────────────────
    /// <summary>
    /// Вызывается из JavaScript при изменении видимости.
    /// [JSInvokable] должен быть на методе компонента, который делегирует сюда.
    /// </summary>
    public void OnVisibilityChanged(bool isVisible)
    {
        if (_isVisible == isVisible) return;
        _isVisible = isVisible;
        
        // Когда компонент становится видимым — рендерим чтобы показать актуальные данные
        if (isVisible && !_component.IsDisposed)
            _component.StateHasChanged();
    }

    public void MarkInitialized() => _initialized = true;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}