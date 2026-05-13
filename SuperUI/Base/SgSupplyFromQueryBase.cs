// SgSupplyFromQueryBase.cs — Поддержка SupplyParameterFromQuery (.NET 8+) 
// Автоматически привязывает параметры компонента к query string 
 
using System.Reflection; 
using Microsoft.AspNetCore.Components; 
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Logging;
 
namespace SuperUI.Base; 
 
/// <summary> 
/// Базовый класс для компонентов, параметры которых автоматически привязываются к query string. 
/// Использует [SupplyParameterFromQuery] (.NET 8+). 
/// 
/// Пример: 
/// <code> 
/// @page "/search" 
/// @inherits SgSupplyFromQueryBase 
/// 
/// @code { 
///     [SupplyParameterFromQuery] 
///     public string? Query { get; set; } 
///     
///     [SupplyParameterFromQuery(Name = "page")] 
///     public int PageNumber { get; set; } = 1; 
///     
///     [SupplyParameterFromQuery(Name = "size")] 
///     public int PageSize { get; set; } = 20; 
/// } 
/// </code> 
/// </summary> 
public abstract class SgSupplyFromQueryBase : SgComponentBase 
{ 
    // ────────────────────────────────────────────── 
    //  Поля 
    // ────────────────────────────────────────────── 
 
    private Dictionary<string, PropertyInfo>? _queryProperties; 
    private NavigationManager? _navigationManager; 
 
    // ────────────────────────────────────────────── 
    //  Конструктор / Инжекция 
    // ────────────────────────────────────────────── 
 
    [Inject] 
    private NavigationManager NavigationManager 
    { 
        get => _navigationManager!;
        set => _navigationManager = value; 
    } 
 
    // ────────────────────────────────────────────── 
    //  Жизненный цикл 
    // ────────────────────────────────────────────── 
 
    protected override Task OnInitializeAsync() 
    { 
        // Сканируем свойства с атрибутом [SupplyParameterFromQuery] 
        _queryProperties = GetType() 
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic) 
            .Where(p => p.GetCustomAttribute<SupplyParameterFromQueryAttribute>() != null) 
            .ToDictionary( 
                p => p.GetCustomAttribute<SupplyParameterFromQueryAttribute>()!.Name ?? p.Name, 
                p => p, 
                StringComparer.OrdinalIgnoreCase); 
 
        // Применяем текущие query параметры 
        ApplyQueryParameters(); 
 
        // Подписываемся на изменения URL 
        if (_navigationManager != null) 
        { 
            _navigationManager.LocationChanged += OnLocationChanged; 
        } 
 
        return Task.CompletedTask; 
    } 
 
    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) 
    { 
        ApplyQueryParameters(); 
        _ = RefreshAsync(); 
    } 
 
    private void ApplyQueryParameters() 
    { 
        if (_queryProperties == null || _navigationManager == null) return; 
 
        var uri = new Uri(_navigationManager.Uri); 
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query); 
 
        foreach (var (key, property) in _queryProperties) 
        { 
            var stringValue = query[key]; 
            if (stringValue == null) continue; 
 
            try 
            { 
                var converted = Convert.ChangeType(stringValue, property.PropertyType); 
                property.SetValue(this, converted); 
            } 
            catch (Exception ex) 
            { 
                Logger.LogWarning(ex, "[{ComponentId}] Failed to parse query parameter {Key}={Value} as {Type}", 
                    ComponentId, key, stringValue, property.PropertyType.Name); 
            } 
        } 
    } 
 
    /// <summary> 
    /// Обновляет query string с текущими значениями параметров. 
    /// </summary> 
    protected void UpdateQueryString() 
    { 
        if (_queryProperties == null || _navigationManager == null) return; 
 
        var uri = new Uri(_navigationManager.Uri); 
        var query = System.Web.HttpUtility.ParseQueryString(uri.Query); 
 
        foreach (var (key, property) in _queryProperties) 
        { 
            var value = property.GetValue(this); 
            if (value != null) 
            { 
                query[key] = value.ToString(); 
            } 
            else 
            { 
                query.Remove(key); 
            } 
        } 
 
        var newUri = uri.GetLeftPart(UriPartial.Path); 
        var queryString = query.ToString(); 
        if (!string.IsNullOrEmpty(queryString)) 
            newUri += "?" + queryString; 
 
        _navigationManager.NavigateTo(newUri, forceLoad: false, replace: true); 
    } 
 
    protected override void Dispose(bool disposing) 
    { 
        if (disposing && _navigationManager != null) 
        { 
            _navigationManager.LocationChanged -= OnLocationChanged; 
        } 
 
        base.Dispose(disposing); 
    } 
} 
