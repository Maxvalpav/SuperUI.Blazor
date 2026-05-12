// SuperUI/Base/SgSmartFormBase.cs
// ИСПРАВЛЕНО:
// 1. _fieldErrors — ConcurrentDictionary (thread-safe для Server)
// 2. CompletionPercent — реальный подсчёт через reflection
// 3. IsFormValid — учитывает незаполненные поля
// 4. CrossFieldValidate — добавлен виртуальный метод
// 5. GetAllErrors() — добавлен
// 6. Нет интеграции с EditContext — наследует SgInteractiveBase (ок, SmartForm не EditContext-зависим)
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
public abstract class SgSmartFormBase<TModel> : SgInteractiveBase
    where TModel : class, new()
{
    [Parameter] public TModel Model { get; set; } = new();
    [Parameter] public Func<TModel, CancellationToken, Task<IEnumerable<(string Field, string Error)>>>? RemoteValidator { get; set; }
    [Parameter] public EventCallback<TModel> OnValidSubmit { get; set; }
    [Parameter] public int ValidationDebounceMs { get; set; } = 500;

    // ИСПРАВЛЕНО: ConcurrentDictionary для thread-safety на Server
    private readonly ConcurrentDictionary<string, List<string>> _fieldErrors = new();

    // ── Completion ─────────────────────────────────────────────────────────────

    protected double CompletionPercent
    {
        get
        {
            // ИСПРАВЛЕНО: реальный подсчёт
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

    // ── Validation ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Форма валидна только если все поля валидировались И не имеют ошибок.
    /// </summary>
    protected bool IsFormValid
    {
        get
        {
            var props = GetModelProperties();
            // Если поле не валидировалось — считаем невалидным (строго)
            if (_fieldErrors.Count < props.Length) return false;
            return _fieldErrors.Values.All(e => e.Count == 0);
        }
    }

    protected async Task ValidateFieldAsync(string fieldName, object? value)
    {
        var localErrors = ValidateField(fieldName, value).Distinct().ToList();
        _fieldErrors[fieldName] = localErrors;

        // Удалённая валидация с дебаунсом
        if (RemoteValidator is not null && localErrors.Count == 0)
        {
            await DebounceAsync($"remote_{fieldName}", async () =>
            {
                var remoteErrors = await RemoteValidator(Model, ComponentToken);
                foreach (var (field, error) in remoteErrors)
                    _fieldErrors.AddOrUpdate(
                        field,
                        _ => [error],
                        (_, list) => { lock (list) { if (!list.Contains(error)) list.Add(error); } return list; });
                _ = RefreshAsync();
            }, TimeSpan.FromMilliseconds(ValidationDebounceMs));
        }

        // Cross-field validation
        var crossErrors = await ValidateCrossFieldAsync(fieldName);
        foreach (var (field, error) in crossErrors)
            _fieldErrors.AddOrUpdate(
                field,
                _ => [error],
                (_, list) => { lock (list) { if (!list.Contains(error)) list.Add(error); } return list; });

        _ = RefreshAsync();
    }

    /// <summary>Синхронная локальная валидация поля.</summary>
    protected virtual IEnumerable<string> ValidateField(string fieldName, object? value) => [];

    /// <summary>
    /// Cross-field валидация. Переопределите для межполевых проверок.
    /// Возвращает список (fieldName, errorMessage).
    /// </summary>
    protected virtual Task<IEnumerable<(string Field, string Error)>> ValidateCrossFieldAsync(string changedField)
        => Task.FromResult(Enumerable.Empty<(string, string)>());

    /// <summary>Получить ошибки для конкретного поля.</summary>
    protected IEnumerable<string> GetErrors(string fieldName)
        => _fieldErrors.TryGetValue(fieldName, out var errors) ? errors : [];

    /// <summary>Получить все ошибки формы.</summary>
    protected IEnumerable<(string Field, IReadOnlyList<string> Errors)> GetAllErrors()
        => _fieldErrors
            .Where(kv => kv.Value.Count > 0)
            .Select(kv => (kv.Key, (IReadOnlyList<string>)kv.Value));

    /// <summary>Очистить все ошибки.</summary>
    protected void ClearAllErrors() => _fieldErrors.Clear();

    // ── Submit ─────────────────────────────────────────────────────────────────

    protected async Task SubmitAsync()
    {
        // Валидируем все поля перед отправкой
        var props = GetModelProperties();
        foreach (var p in props)
            await ValidateFieldAsync(p.Name, p.GetValue(Model));

        if (!IsFormValid) return;
        await OnValidSubmit.InvokeAsync(Model);
    }

    // ── Internals ──────────────────────────────────────────────────────────────

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propCache = new();

    private PropertyInfo[] GetModelProperties()
        => _propCache.GetOrAdd(typeof(TModel), static t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
             .Where(p => p.CanRead && p.CanWrite)
             .ToArray());
}