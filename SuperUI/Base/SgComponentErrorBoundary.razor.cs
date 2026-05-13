// SgComponentErrorBoundary.razor.cs — Улучшенный компонент обработки ошибок 
// Интеграция с .NET встроенным ErrorBoundary + кастомная логика 
 
using Microsoft.AspNetCore.Components; 
using Microsoft.AspNetCore.Components.Rendering; 
using Microsoft.AspNetCore.Components.Web; 
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging; 
using SuperUI.Base.Diagnostics;
 
namespace SuperUI.Base; 
 
/// <summary> 
/// Расширенный Error Boundary компонент. 
/// 
/// Улучшения относительно стандартного ErrorBoundary: 
/// - Кастомный UI ошибки 
/// - Кнопка Retry 
/// - Логирование ошибок 
/// - Поддержка разных стратегий восстановления 
/// - Accessibility (роль alert) 
/// </summary> 
public partial class SgComponentErrorBoundary : ErrorBoundary 
{ 
    // ────────────────────────────────────────────── 
    //  Параметры 
    // ────────────────────────────────────────────── 
 
    /// <summary>Заголовок ошибки.</summary> 
    [Parameter] 
    public string ErrorTitle { get; set; } = "Something went wrong"; 
 
    /// <summary>Описание ошибки.</summary> 
    [Parameter] 
    public string ErrorDescription { get; set; } = "An unexpected error occurred. Please try again."; 
 
    /// <summary>Показывать ли технические детали ошибки.</summary> 
    [Parameter] 
    public bool ShowTechnicalDetails { get; set; } 
 
    /// <summary>Максимальное количество попыток восстановления.</summary> 
    [Parameter] 
    public int MaxRetryAttempts { get; set; } = 3; 
 
    /// <summary>Текст кнопки Retry.</summary> 
    [Parameter] 
    public string RetryText { get; set; } = "Try Again"; 
 
    /// <summary>CSS класс контейнера ошибки.</summary> 
    [Parameter] 
    public string? ErrorClass { get; set; } 
 
    // ────────────────────────────────────────────── 
    //  Состояние 
    // ────────────────────────────────────────────── 
 
    private int _retryCount; 
    private bool _isRecovering; 
 
    // ────────────────────────────────────────────── 
    //  Инжекция 
    // ────────────────────────────────────────────── 
 
    [Inject] private ILogger<SgComponentErrorBoundary> Logger { get; set; } = default!; 
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;
 
    // ────────────────────────────────────────────── 
    //  Переопределение методов 
    // ────────────────────────────────────────────── 
 
    protected override Task OnErrorAsync(Exception exception) 
    { 
        _retryCount++; 
 
        Logger.LogError(exception, 
            "[SgErrorBoundary] Error caught (attempt {Attempt}/{MaxAttempts}): {Message}", 
            _retryCount, MaxRetryAttempts, exception.Message); 
 
        // Отправляем в диагностику если доступна 
        var diagnostics = ServiceProvider.GetService<ComponentDiagnostics>(); 
        diagnostics?.RecordError(GetType().Name, exception); 
 
        return Task.CompletedTask; 
    } 
 
    /// <summary> 
    /// Пытается восстановиться после ошибки. 
    /// </summary> 
    public new async Task RecoverAsync() 
    { 
        if (_retryCount >= MaxRetryAttempts) 
        { 
            Logger.LogWarning("[SgErrorBoundary] Max retry attempts ({Max}) reached", MaxRetryAttempts); 
            return; 
        } 
 
        _isRecovering = true; 
 
        try 
        { 
            base.Recover(); // ErrorBoundary.Recover() is synchronous in some versions, but standard is Recover()
            await Task.Delay(100); // Give it a moment to clear
            _isRecovering = false;
            StateHasChanged();
            Logger.LogInformation("[SgErrorBoundary] Recovery succeeded on attempt {Attempt}", _retryCount); 
        } 
        catch (Exception ex) 
        { 
            Logger.LogError(ex, "[SgErrorBoundary] Recovery failed on attempt {Attempt}", _retryCount); 
            _isRecovering = false; 
            throw; 
        } 
    } 
 
    /// <summary> 
    /// Сбрасывает счётчик ошибок. 
    /// </summary> 
    public void ResetCounters() 
    { 
        _retryCount = 0; 
        _isRecovering = false; 
    } 
} 
