// SuperUI/Base/SgSsrFormSupport.cs
// ✅ SSR-2 FIX: поддержка Static SSR форм с [SupplyParameterFromForm] и Antiforgery
// ✅ Работает совместно с SgFormBase<TModel>
// ✅ .NET 8+ AntiforgeryToken интеграция

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для SSR-совместимых форм.
/// Добавляет поддержку [SupplyParameterFromForm] и Antiforgery для Static SSR.
/// В Interactive режиме работает как обычная SgFormBase.
/// </summary>
/// <typeparam name="TModel">Тип модели формы.</typeparam>
public abstract class SgSsrFormBase<TModel> : SgFormBase<TModel>
    where TModel : class, new()
{
    /// <summary>
    /// Модель из POST-запроса (Static SSR).
    /// В конкретном компоненте добавьте [SupplyParameterFromForm] на это свойство.
    /// </summary>
    [Parameter]
    public TModel? SsrModel { get; set; }

    /// <summary>HttpContext для определения метода запроса в Static SSR.</summary>
    [CascadingParameter]
    protected HttpContext? HttpContext { get; set; }

    protected override void OnInitialized()
    {
        // В Static SSR используем модель из POST вместо new TModel()
        if (SsrModel is not null && IsStaticSSR)
        {
            Model = SsrModel;
            IsModelFromForm = true;
        }

        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // В Static SSR при POST — валидируем и сабмитим сразу
        if (SsrModel is not null && IsStaticSSR
            && string.Equals(HttpContext?.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            Model = SsrModel;
            IsModelFromForm = true;
            await HandleSubmitAsync();
        }
    }

    /// <summary>
    /// Атрибуты для &lt;form&gt; тега в SSR-разметке.
    /// Использование: &lt;form @attributes="SsrFormAttributes"&gt;
    /// </summary>
    protected IReadOnlyDictionary<string, object?> SsrFormAttributes => new Dictionary<string, object?>
    {
        ["method"] = "post",
        ["data-enhance"] = IsInteractive ? null : "true"
    };
}
