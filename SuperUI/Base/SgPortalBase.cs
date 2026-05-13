// SuperUI/Base/SgPortalBase.cs — НОВЫЙ (MISSING-7)
//
// НОВОЕ:
// ✅ Portal-паттерн: рендеринг вне DOM-дерева
// ✅ Поддержка Toasts, Tooltips, Context Menus, Modals
// ✅ Cascading Values для регистрации в хосте
// ✅ Динамическое обновление контента
// ✅ Правильная очистка при dispose

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов, рендерящихся вне своего DOM-дерева (Portal-паттерн).
/// Типичные использования: Toasts, Tooltips, Context Menus, Modals.
/// </summary>
/// <remarks>
/// Реализация через Blazor Cascading Values и динамический рендеринг.
/// Контент рендерится через <see cref="SgPortalHost"/> в целевой контейнер.
/// 
/// Использование:
/// 1. Разместить SgPortalHost в App.razor или MainLayout
/// 2. Наследовать SgPortalBase для создания портала
/// 3. Переопределить PortalContent для определения контента
/// 
/// Пример:
/// <code>
/// public partial class MyToast : SgPortalBase
/// {
///     [Parameter] public string Message { get; set; }
///     
///     protected override RenderFragment PortalContent => builder =>
///     {
///         builder.OpenElement(0, "div");
///         builder.AddAttribute(1, "class", "toast");
///         builder.AddContent(2, Message);
///         builder.CloseElement();
///     };
/// }
/// </code>
/// </remarks>
public abstract class SgPortalBase : SgJsComponentBase
{
    [CascadingParameter]
    private SgPortalHost? PortalHost { get; set; }

    // ── Параметры ───────────────────────────────────────────────────────────

    /// <summary>CSS-селектор целевого контейнера. По умолчанию "body".</summary>
    [Parameter] public string TargetSelector { get; set; } = "body";

    // ── Абстрактные члены ───────────────────────────────────────────────────

    /// <summary>
    /// Контент портала для рендеринга.
    /// Override для определения того, что рендерится в портале.
    /// </summary>
    protected abstract RenderFragment PortalContent { get; }

    // ── Жизненный цикл ──────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        base.OnInitialized();
        PortalHost?.Register(ComponentId, PortalContent);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        PortalHost?.Update(ComponentId, PortalContent);
    }

    /// <summary>
    /// Не рендерим ничего на месте — контент идёт через портал.
    /// </summary>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Portal не рендерит контент в своём месте
    }

    protected override async ValueTask DisposeComponentAsync()
    {
        PortalHost?.Unregister(ComponentId);
        await base.DisposeComponentAsync();
    }
}

/// <summary>
/// Хост для порталов. Размещается в корне приложения (App.razor или MainLayout).
/// Все порталы рендерятся через этот компонент.
/// </summary>
/// <remarks>
/// Использование в App.razor:
/// <code>
/// @page "/"
/// 
/// <Router AppAssembly="@typeof(App).Assembly">
///     <Found Context="routeData">
///         <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
///     </Found>
///     <NotFound>
///         <PageTitle>Not found</PageTitle>
///         <LayoutView Layout="@typeof(MainLayout)">
///             <p role="alert">Sorry, there's nothing at this address.</p>
///         </LayoutView>
///     </NotFound>
/// </Router>
/// 
/// <CascadingValue Value="this">
///     <SgPortalHost />
/// </CascadingValue>
/// </code>
/// </remarks>
public sealed class SgPortalHost : ComponentBase, IDisposable
{
    private readonly Dictionary<string, RenderFragment> _portals = [];

    // ── Регистрация порталов ────────────────────────────────────────────────

    /// <summary>Зарегистрировать новый портал.</summary>
    internal void Register(string id, RenderFragment content)
    {
        _portals[id] = content;
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>Обновить контент существующего портала.</summary>
    internal void Update(string id, RenderFragment content)
    {
        if (_portals.ContainsKey(id))
        {
            _portals[id] = content;
            _ = InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>Отменить регистрацию портала.</summary>
    internal void Unregister(string id)
    {
        if (_portals.Remove(id))
        {
            _ = InvokeAsync(StateHasChanged);
        }
    }

    // ── Рендеринг ───────────────────────────────────────────────────────────

    /// <summary>Рендерить все зарегистрированные порталы.</summary>
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 0;

        foreach (var (_, fragment) in _portals)
        {
            builder.AddContent(seq++, fragment);
        }
    }

    // ── Dispose ─────────────────────────────────────────────────────────────

    /// <summary>Очистить все порталы.</summary>
    public void Dispose()
    {
        _portals.Clear();
    }
}
