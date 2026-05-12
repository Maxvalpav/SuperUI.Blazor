// SuperUI/Base/State/ParameterState.cs — НОВЫЙ (UX-5)
//
// НОВОЕ:
// ✅ Отслеживание изменений параметров компонента
// ✅ Поддержка кастомных компараторов
// ✅ Callback при изменении параметра
// ✅ Аналог MudBlazor ParameterState<T>
// ✅ Упрощает логику OnParametersSet()

namespace SuperUI.Base.State;

/// <summary>
/// Отслеживает изменения параметра компонента.
/// Позволяет определить, изменился ли параметр между рендерами.
/// </summary>
/// <typeparam name="T">Тип параметра.</typeparam>
/// <remarks>
/// Аналог MudBlazor ParameterState&lt;T&gt;, но с поддержкой кастомных компараторов
/// и callback при изменении.
/// 
/// Использование:
/// <code>
/// private ParameterState&lt;string&gt; _labelState;
/// 
/// [Parameter] public string Label { get; set; }
/// 
/// protected override void OnInitialized()
/// {
///     _labelState = ParameterState&lt;string&gt;.Attach(
///         () => Label,
///         onChange: (prev, next) => Console.WriteLine($"Label changed: {prev} → {next}")
///     );
/// }
/// 
/// protected override void OnParametersSet()
/// {
///     if (_labelState.Sync())
///     {
///         // Label изменился
///         UpdateUI();
///     }
/// }
/// </code>
/// </remarks>
public sealed class ParameterState<T>
{
    private T _value;
    private T _previousValue;
    private bool _hasChanged;
    private readonly IEqualityComparer<T> _comparer;
    private readonly Func<T> _getter;
    private readonly Action<T, T>? _onChange; // (prev, next)

    private ParameterState(
        Func<T> getter,
        IEqualityComparer<T>? comparer = null,
        Action<T, T>? onChange = null)
    {
        _getter = getter ?? throw new ArgumentNullException(nameof(getter));
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _onChange = onChange;
        _value = getter();
        _previousValue = _value;
    }

    // ── Свойства ────────────────────────────────────────────────────────────

    /// <summary>Текущее значение параметра.</summary>
    public T Value => _value;

    /// <summary>Предыдущее значение (до последнего Sync()).</summary>
    public T PreviousValue => _previousValue;

    /// <summary>true — параметр изменился в последнем Sync().</summary>
    public bool HasChanged => _hasChanged;

    // ── Фабрика ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Создать экземпляр с автоматическим getter из компонента.
    /// </summary>
    /// <param name="getter">Функция для получения текущего значения параметра.</param>
    /// <param name="comparer">Кастомный компаратор (по умолчанию EqualityComparer&lt;T&gt;.Default).</param>
    /// <param name="onChange">Callback при изменении: (previousValue, newValue).</param>
    /// <returns>Новый экземпляр ParameterState.</returns>
    public static ParameterState<T> Attach(
        Func<T> getter,
        IEqualityComparer<T>? comparer = null,
        Action<T, T>? onChange = null)
        => new(getter, comparer, onChange);

    // ── Синхронизация ───────────────────────────────────────────────────────

    /// <summary>
    /// Обновить значение из параметра компонента.
    /// Вызывается из OnParametersSet().
    /// </summary>
    /// <returns>true если значение изменилось.</returns>
    public bool Sync()
    {
        var newValue = _getter();
        _hasChanged = !_comparer.Equals(_value, newValue);

        if (_hasChanged)
        {
            _previousValue = _value;
            _value = newValue;
            _onChange?.Invoke(_previousValue, _value);
        }

        return _hasChanged;
    }

    // ── Операторы ───────────────────────────────────────────────────────────

    /// <summary>Неявное приведение к T (возвращает Value).</summary>
    public static implicit operator T(ParameterState<T> state) => state._value;

    /// <summary>Информативное представление состояния.</summary>
    public override string ToString()
        => $"ParameterState<{typeof(T).Name}> {{ Value={_value}, Changed={_hasChanged} }}";
}
