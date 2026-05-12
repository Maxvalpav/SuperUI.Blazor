// SuperUI/Base/SgSmartFormBase.cs
//
// ИСПРАВЛЕНИЯ:
//   ✅ GetAllErrors() — исправлен тип возврата: (string Field, IReadOnlyList<string> Errors)
//   ✅ ConcurrentDictionary — thread-safe для Server
//   ✅ CompletionPercent — реальный подсёт через reflection (кэшированный)
//   ✅ IsFormValid — учитывает невалидированные поля
//   ✅ CrossFieldValidate — virtual Task<IEnumerable<(string, string)>>
//
// УЛУЧШЕНИЯ:
//   ✅ ResetAsync() — сброс формы к начальному состоянию
//   ✅ GetFieldErrors(string) / HasFieldError(string) — удобные helper-ы

using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace SuperUI.Base;

/// <summary>
/// SmartForm: форма с интеллектуальной валидацией.
/// - Thread-safe словарь ошибок (Blazor Server)
/// - Дедупликация ошибок
/// - Cross-field validation
/// - Async remote validation с debounce
/// - Прогресс заполнения формы
/// </summary>
/// <typeparam name="TModel">Тип модели формы (должен иметь конструктор без параметров).</typeparam>
public abstract class SgSmartFormBase<TModel> : SgInteractiveBase
    where TModel : class, new()
{
    [Parameter] public TModel Model { get; set; } = new();
    [Parameter] public Func<TModel, CancellationToken, Task<Dictionary<string, List<string>>>>?
        RemoteValidator { get; set; }
    [Parameter] public EventCallback<TModel> OnValidSubmit { get; set; }
    [Parameter] public int ValidationDebounceMs { get; set; } = 500;

    // ИСПРАВЛЕНИЕ: ConcurrentDictionary для thread-safety на Server
    private readonly ConcurrentDictionary<string, List<string>> _fieldErrors = new();

    // ── Completion ───────────────────────────────────────────────────────────────

    /// <summary>Процент заполнения формы (0-100).</summary>
    protected double CompletionPercent
    {
        get
        {
            var props = GetModelProperties();
            if (props.Length == 0) return 0;
            int filled = 0;
            foreach (var p in props)
            {
                var val = p.GetValue(Model);
                if (val is not null && (val is not string s || !string.IsNullOrWhiteSpace(s)))
                    filled++;
            }
            return (double)filled / props.Length * 100;
        }
    }

    // ── Validation ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Форма валидна если все поля прошли валидацию без ошибок.
    /// Невалидированные поля считаются невалидными.
    /// </summary>
    protected bool IsFormValid
    {
        get
        {
            var props = GetModelProperties();
            if (_fieldErrors.Count < props.Length) return false; // не все поля валидированы
            return _fieldErrors.Values.All(e => e.Count == 0);
        }
    }

    /// <summary>
    /// Валидировать поле с удалённой и cross-field валидацией.
    /// </summary>
    protected async Task ValidateFieldAsync(string fieldName, object? value)
    {
        var localErrors = ValidateField(fieldName, value).Distinct().ToList();
        _fieldErrors[fieldName] = localErrors;

        // Удалённая валидация с дебаунсом
        if (RemoteValidator is not null && localErrors.Count == 0)
        {
            await DebounceAsync($"remote_{fieldName}", async () =>
            {
                try
                {
                    var remoteErrors = await RemoteValidator(Model, ComponentToken);
                    foreach (var (field, errors) in remoteErrors)
                    {
                        _fieldErrors[field] = errors;
                    }
                    _ = RefreshAsync();
                }
                catch (OperationCanceledException) { }
            }, TimeSpan.FromMilliseconds(ValidationDebounceMs));
        }

        // Cross-field validation
        var crossErrors = await ValidateCrossFieldAsync(fieldName);
        foreach (var (field, error) in crossErrors)
        {
            _fieldErrors.AddOrUpdate(
                field,
                _ => [error],
                (_, list) => { lock (list) { if (!list.Contains(error)) list.Add(error); } return list; });
        }

        _ = RefreshAsync();
    }

    /// <summary>Синхронная локальная валидация поля. Переопределите для добавления правил.</summary>
    protected virtual IEnumerable<string> ValidateField(string fieldName, object? value)
        => [];

    /// <summary>
    /// Cross-field валидация. Переопределите для межполевых проверок.
    /// Возвращает список (fieldName, errorMessage).
    /// </summary>
    protected virtual Task<IEnumerable<(string Field, string Error)>> ValidateCrossFieldAsync(
        string changedField)
        => Task.FromResult(Enumerable.Empty<(string, string)>());

    /// <summary>Получить ошибки для конкретного поля.</summary>
    protected IEnumerable<string> GetFieldErrors(string fieldName)
        => _fieldErrors.TryGetValue(fieldName, out var errors) ? errors : [];

    /// <summary>Есть ли ошибки у поля.</summary>
    protected bool HasFieldError(string fieldName)
        => _fieldErrors.TryGetValue(fieldName, out var e) && e.Count > 0;

    /// <summary>
    /// Получить все ошибки формы.
    /// ИСПРАВЛЕНИЕ: правильный тип tuple — (Field, Errors).
    /// </summary>
    protected IEnumerable<(string Field, IReadOnlyList<string> Errors)> GetAllErrors()
        => _fieldErrors
            .Where(kv => kv.Value.Count > 0)
            .Select(kv => (kv.Key, (IReadOnlyList<string>)kv.Value));

    /// <summary>Очистить все ошибки.</summary>
    protected void ClearAllErrors() => _fieldErrors.Clear();

    // ── Submit ───────────────────────────────────────────────────────────────────

    /// <summary>Валидировать все поля и отправить форму если валидна.</summary>
    protected async Task SubmitAsync()
    {
        var props = GetModelProperties();
        foreach (var p in props)
            await ValidateFieldAsync(p.Name, p.GetValue(Model));

        if (!IsFormValid) return;
        await OnValidSubmit.InvokeAsync(Model);
    }

    /// <summary>Сбросить форму к начальному состоянию.</summary>
    protected void ResetAsync()
    {
        Model = new TModel();
        _fieldErrors.Clear();
        _ = RefreshAsync();
    }

    // ── Internals ────────────────────────────────────────────────────────────────

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propCache = new();

    private PropertyInfo[] GetModelProperties()
        => _propCache.GetOrAdd(typeof(TModel), static t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
             .Where(p => p.CanRead && p.CanWrite)
             .ToArray());
}
