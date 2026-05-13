// SuperUI/Base/SgStreamingComponentBase.cs 
// Новый класс для поддержки [StreamRendering] (.NET 8+) 
// Улучшения: 
// - Правильный паттерн для streaming rendering 
// - Skeleton/placeholder пока данные загружаются 
// - Skeleton автоматически скрывается после загрузки 
// - Поддержка ошибок при загрузке 
 
using System; 
using System.Threading; 
using System.Threading.Tasks; 
using Microsoft.AspNetCore.Components; 
using Microsoft.Extensions.Logging;
 
namespace SuperUI.Base; 
 
/// <summary> 
/// Базовый класс для компонентов с потоковым рендерингом. 
/// Автоматически показывает skeleton пока данные загружаются. 
/// 
/// Использование: 
/// <code> 
/// @attribute [StreamRendering] 
/// @inherits SgStreamingComponentBase&lt;MyData&gt; 
/// 
/// @if (IsLoading) { &lt;Skeleton /&gt; } 
/// else if (HasError) { &lt;Error Message="@ErrorMessage" /&gt; } 
/// else { &lt;MyContent Data="@Data" /&gt; } 
/// </code> 
/// </summary> 
public abstract class SgStreamingComponentBase<TData> : SgComponentBase 
{ 
    private CancellationTokenSource? _loadCts; 
 
    /// <summary>Данные загружаются.</summary> 
    protected bool IsLoading { get; private set; } = true; 
 
    /// <summary>Произошла ошибка загрузки.</summary> 
    protected bool HasError { get; private set; } 
 
    /// <summary>Сообщение об ошибке.</summary> 
    protected string? ErrorMessage { get; private set; } 
 
    /// <summary>Загруженные данные.</summary> 
    protected TData? Data { get; private set; } 
 
    /// <summary>Прогресс загрузки (0-100), если поддерживается.</summary> 
    protected int LoadProgress { get; private set; } 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Параметры 
    // ────────────────────────────────────────────────────────────────────── 
 
    /// <summary>Таймаут загрузки данных.</summary> 
    [Parameter] public TimeSpan LoadTimeout { get; set; } = TimeSpan.FromSeconds(30); 
 
    /// <summary>Количество попыток при ошибке.</summary> 
    [Parameter] public int RetryCount { get; set; } = 0; 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Жизненный цикл 
    // ────────────────────────────────────────────────────────────────────── 
 
    protected override async Task OnInitializedAsync() 
    { 
        await base.OnInitializedAsync(); 
        await LoadDataWithRetryAsync(); 
    } 
 
    private async Task LoadDataWithRetryAsync() 
    { 
        IsLoading = true; 
        HasError = false; 
        ErrorMessage = null; 
 
        _loadCts?.Dispose(); 
        _loadCts = new CancellationTokenSource(LoadTimeout); 
 
        int attempts = 0; 
        while (true) 
        { 
            try 
            { 
                Data = await LoadAsync(_loadCts.Token); 
                IsLoading = false; 
                return; 
            } 
            catch (OperationCanceledException) when (_loadCts.Token.IsCancellationRequested) 
            { 
                HasError = true; 
                ErrorMessage = "Превышено время ожидания загрузки данных."; 
                IsLoading = false; 
                return; 
            } 
            catch (Exception ex) when (attempts < RetryCount) 
            { 
                attempts++; 
                Logger.LogWarning(ex, "Load attempt {Attempt}/{Max} failed, retrying...", 
                    attempts, RetryCount); 
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempts)), _loadCts.Token); 
            } 
            catch (Exception ex) 
            { 
                HasError = true; 
                ErrorMessage = GetUserFriendlyError(ex); 
                IsLoading = false; 
                Logger.LogError(ex, "SgStreamingComponentBase failed to load data"); 
                return; 
            } 
        } 
    } 
 
    /// <summary>Повторяет загрузку данных.</summary> 
    public async Task ReloadAsync() 
    { 
        await InvokeAsync(async () => 
        { 
            await LoadDataWithRetryAsync(); 
            StateHasChanged(); 
        }); 
    } 
 
    /// <summary>Обновляет прогресс загрузки.</summary> 
    protected void ReportProgress(int percent) 
    { 
        LoadProgress = Math.Clamp(percent, 0, 100); 
        // При StreamRendering StateHasChanged отправляет patch клиенту 
        StateHasChanged(); 
    } 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Абстрактные методы 
    // ────────────────────────────────────────────────────────────────────── 
 
    /// <summary> 
    /// Загружает данные. Переопределите. 
    /// При StreamRendering промежуточные вызовы StateHasChanged 
    /// отправляют обновления клиенту в реальном времени. 
    /// </summary> 
    protected abstract Task<TData> LoadAsync(CancellationToken ct); 
 
    /// <summary>Преобразует исключение в пользовательское сообщение.</summary> 
    protected virtual string GetUserFriendlyError(Exception ex) 
        => "Произошла ошибка при загрузке данных. Попробуйте ещё раз."; 
 
    protected override async ValueTask DisposeAsyncCore() 
    { 
        _loadCts?.Cancel(); 
        _loadCts?.Dispose(); 
        await base.DisposeAsyncCore(); 
    } 
} 
