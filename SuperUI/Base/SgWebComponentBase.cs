// SuperUI/Base/SgWebComponentBase.cs
// ✅ NEW: базовый класс для Blazor Custom Elements (.NET 7+, улучшено в .NET 10)
// ✅ Поддержка shadow DOM атрибутов через параметры
// ✅ NET8/9/10 совместимо

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов, экспортируемых как Custom Elements (Web Components).
/// Используется с RegisterCustomElement в Program.cs (.NET 7+).
/// </summary>
public abstract class SgWebComponentBase : SgJsComponentBase
{
    /// <summary>Имя custom element (kebab-case). Например: "sg-button".</summary>
    protected abstract string ElementName { get; }

    /// <summary>Использовать Shadow DOM для изоляции стилей.</summary>
    protected virtual bool UseShadowDom => false;

    /// <summary>Shadow DOM mode: "open" или "closed".</summary>
    protected virtual string ShadowDomMode => "open";

    protected override async Task OnFirstRenderAsync()
    {
        await base.OnFirstRenderAsync();
        if (UseShadowDom)
            await SafeGlobalInvokeVoidAsync("__sg_attachShadow", ComponentId, ShadowDomMode);
    }
}
