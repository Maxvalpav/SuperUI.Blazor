// SuperUI/Base/Reactive/SgSignalValidator.cs
// НОВЫЙ КЛАСС
// Аналог: Mobx-state-tree types.refinement, Zod (JS)
// Поддержка: .NET 8/9/10

using System.Text.RegularExpressions;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Результат валидации значения сигнала.
/// </summary>
public readonly record struct SignalValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }

    public static SignalValidationResult Valid => new() { IsValid = true };

    public static SignalValidationResult Invalid(string message)
        => new() { IsValid = false, ErrorMessage = message };
}

/// <summary>
/// Сигнал с декларативной валидацией.
/// При попытке Set() невалидного значения — вызывает OnValidationFailed.
///
/// Использование:
/// <code>
/// var age = new SgSignalWithValidation&lt;int&gt;(0, "age")
///     .Required()
///     .Min(0, "Возраст не может быть отрицательным")
///     .Max(150, "Возраст не может быть > 150")
///     .Custom(v =&gt; v % 1 == 0, "Возраст должен быть целым числом");
///
/// age.Set(25);   // OK
/// age.Set(-1);   // ValidationFailed: "Возраст не может быть отрицательным"
/// bool valid = age.IsValid;  // false после -1
/// </code>
/// </summary>
public sealed class SgSignalWithValidation<T> : ISgSignal<T>, IDisposable
{
    private readonly SgSignal<T> _inner;
    private readonly SgSignal<string?> _errorSignal;
    private readonly List<Func<T, SignalValidationResult>> _validators = [];
    private int _disposed;

    /// <summary>Текущее значение сигнала.</summary>
    public T Value => _inner.Value;

    /// <summary>Текущая ошибка валидации (null если валидно).</summary>
    public string? ValidationError => _errorSignal.Value;

    /// <summary>Прошло ли значение валидацию.</summary>
    public bool IsValid => _errorSignal.Value is null;

    /// <summary>Сигнал ошибки (для реактивного использования в UI).</summary>
    public IReadOnlySignal<string?> ErrorSignal => _errorSignal;

    /// <summary>Событие: валидация провалилась.</summary>
    public event Action<T, string>? ValidationFailed;

    /// <summary>Режим: отклонять невалидные значения (true) или принимать с ошибкой (false).</summary>
    public bool RejectInvalid { get; set; } = false;

    public string? DebugName => _inner.DebugName;
    public int SubscriberCount => _inner.SubscriberCount;

    public SgSignalWithValidation(T initialValue, string? debugName = null)
    {
        _inner = new SgSignal<T>(initialValue, debugName);
        _errorSignal = new SgSignal<string?>(null, $"{debugName}-error");
    }

    // ── Fluent валидаторы ────────────────────────────────────────────────────

    /// <summary>Добавить кастомный валидатор.</summary>
    public SgSignalWithValidation<T> Custom(
        Func<T, bool> validator,
        string errorMessage = "Недопустимое значение")
    {
        _validators.Add(v => validator(v)
            ? SignalValidationResult.Valid
            : SignalValidationResult.Invalid(errorMessage));
        return this;
    }

    /// <summary>Добавить кастомный валидатор с детализированным сообщением.</summary>
    public SgSignalWithValidation<T> Custom(Func<T, SignalValidationResult> validator)
    {
        _validators.Add(validator);
        return this;
    }

    // ── ISgSignal ────────────────────────────────────────────────────────────

    public void Set(T newValue)
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        // Прогоняем через все валидаторы
        foreach (var validator in _validators)
        {
            var result = validator(newValue);
            if (!result.IsValid)
            {
                _errorSignal.Set(result.ErrorMessage);
                ValidationFailed?.Invoke(newValue, result.ErrorMessage ?? "Validation failed");

                if (RejectInvalid) return; // Не устанавливаем значение
                break; // Устанавливаем значение, но сохраняем ошибку
            }
        }

        // Если все валидаторы прошли
        if (_validators.All(v => v(newValue).IsValid))
            _errorSignal.Set(null);

        _inner.Set(newValue);
    }

    /// <summary>
    /// Принудительно установить значение без валидации.
    /// Используется для initial state и imports.
    /// </summary>
    public void SetUnchecked(T value) => _inner.Set(value);

    /// <summary>Выполнить валидацию текущего значения вручную.</summary>
    public bool Validate()
    {
        foreach (var validator in _validators)
        {
            var result = validator(_inner.Value);
            if (!result.IsValid)
            {
                _errorSignal.Set(result.ErrorMessage);
                return false;
            }
        }

        _errorSignal.Set(null);
        return true;
    }

    public void Subscribe(ISignalObserver observer) => _inner.Subscribe(observer);

    public void Unsubscribe(ISignalObserver observer) => _inner.Unsubscribe(observer);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _inner.Dispose();
        _errorSignal.Dispose();
    }

    public static implicit operator T(SgSignalWithValidation<T> s) => s.Value;
}

/// <summary>Расширения для удобства валидации числовых типов.</summary>
public static class SgSignalValidationExtensions
{
    public static SgSignalWithValidation<string> Required(
        this SgSignalWithValidation<string> signal,
        string message = "Поле обязательно для заполнения")
        => signal.Custom(v => !string.IsNullOrWhiteSpace(v), message);

    public static SgSignalWithValidation<string> MinLength(
        this SgSignalWithValidation<string> signal,
        int min,
        string? message = null)
        => signal.Custom(v => v?.Length >= min, message ?? $"Минимальная длина: {min}");

    public static SgSignalWithValidation<string> MaxLength(
        this SgSignalWithValidation<string> signal,
        int max,
        string? message = null)
        => signal.Custom(v => v?.Length <= max, message ?? $"Максимальная длина: {max}");

    public static SgSignalWithValidation<string> Pattern(
        this SgSignalWithValidation<string> signal,
        string pattern,
        string? message = null)
        => signal.Custom(
            v => Regex.IsMatch(v ?? "", pattern),
            message ?? $"Не соответствует формату");

    public static SgSignalWithValidation<int> Min(
        this SgSignalWithValidation<int> signal,
        int min,
        string? message = null)
        => signal.Custom(v => v >= min, message ?? $"Минимальное значение: {min}");

    public static SgSignalWithValidation<int> Max(
        this SgSignalWithValidation<int> signal,
        int max,
        string? message = null)
        => signal.Custom(v => v <= max, message ?? $"Максимальное значение: {max}");

    public static SgSignalWithValidation<decimal> Min(
        this SgSignalWithValidation<decimal> signal,
        decimal min,
        string? message = null)
        => signal.Custom(v => v >= min, message ?? $"Минимальное значение: {min}");

    public static SgSignalWithValidation<decimal> Max(
        this SgSignalWithValidation<decimal> signal,
        decimal max,
        string? message = null)
        => signal.Custom(v => v <= max, message ?? $"Максимальное значение: {max}");
}
