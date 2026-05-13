// SuperUI/Base/SgSupplyFromQueryBase.cs
// ✅ SSR-1 NEW: базовый класс с удобным доступом к query-параметрам в SSR
// ✅ Поддерживает Static SSR, Interactive Server и WebAssembly

using Microsoft.AspNetCore.Components;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для страниц/компонентов, читающих параметры из URL query string.
///
/// Пример:
/// <code>
/// public class MyPage : SgQueryParamBase
/// {
///     [SupplyParameterFromQuery(Name = "page")]
///     private int Page { get; set; } = 1;
///
///     [SupplyParameterFromQuery(Name = "search")]
///     private string? Search { get; set; }
/// }
/// </code>
/// </summary>
public abstract class SgQueryParamBase : SgComponentBase
{
    [Inject] protected NavigationManager Navigation { get; set; } = null!;

    /// <summary>
    /// Построить URL с обновлёнными query-параметрами.
    /// null-значение удаляет параметр из строки запроса.
    /// </summary>
    protected string BuildUrl(params (string Key, object? Value)[] parameters)
    {
        var uri = new Uri(Navigation.Uri);
        var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);

        foreach (var (key, value) in parameters)
        {
            if (value is null)
                queryParams.Remove(key);
            else
                queryParams[key] = value.ToString();
        }

        var query = queryParams.Count > 0 ? "?" + queryParams : string.Empty;
        return uri.GetLeftPart(UriPartial.Path) + query;
    }

    /// <summary>
    /// Навигировать с обновлёнными query-параметрами (soft navigation, без перезагрузки).
    /// </summary>
    protected void NavigateWithParams(params (string Key, object? Value)[] parameters)
        => Navigation.NavigateTo(BuildUrl(parameters));

    /// <summary>
    /// Навигировать с обновлёнными query-параметрами через enhanced navigation (.NET 8+).
    /// </summary>
    protected void NavigateWithParamsEnhanced(params (string Key, object? Value)[] parameters)
        => Navigation.NavigateTo(BuildUrl(parameters), forceLoad: false);
}
