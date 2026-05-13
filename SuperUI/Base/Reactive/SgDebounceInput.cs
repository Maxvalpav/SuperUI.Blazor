// SuperUI/Base/Reactive/SgDebounceInput.cs
// УНИКАЛЬНЫЙ КЛАСС — связывает HTML input с SgSignal с debounce.

namespace SuperUI.Base.Reactive;

/// <summary>
/// Связывает HTML input с сигналом с настраиваемым debounce.
/// Поддерживает: text, number, search.
/// 
/// Использование:
/// <code>
/// var searchSignal = new SgSignal&lt;string&gt;("");
/// var debounce = new SgDebounceInput&lt;string&gt;(searchSignal, 300);
/// 
/// // В HTML:
/// &lt;input @bind-value="@debounce.InputValue"
///        @bind-value:event="oninput"
///        @oninput="@(e => debounce.OnInput(e))" /&gt;
/// </code>
/// </summary>
public sealed class SgDebounceInput<T> : IDisposable
{
    private readonly SgSignal<T> _target;
    private readonly int _debounceMs;
    private readonly Func<string, T> _parser;
    private readonly Func<T, string> _formatter;
    private CancellationTokenSource? _cts;

    public string InputValue { get; private set; } = "";

    public SgDebounceInput(
        SgSignal<T> target,
        int debounceMs = 300,
        Func<string, T>? parser = null,
        Func<T, string>? formatter = null)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _debounceMs = debounceMs;
        _parser = parser ?? DefaultParser;
        _formatter = formatter ?? (v => v?.ToString() ?? "");

        // Инициализация из сигнала
        InputValue = _formatter(_target.Value);
    }

    /// <summary>Обработчик oninput.</summary>
    public async void OnInput(ChangeEventArgs e)
    {
        var value = e.Value?.ToString() ?? "";
        InputValue = value;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        try
        {
            await Task.Delay(_debounceMs, _cts.Token);
            var parsed = _parser(value);
            _target.Set(parsed);
        }
        catch (OperationCanceledException) { }
        catch (Exception) { /* parse error — не обновляем сигнал */ }
    }

    /// <summary>Принудительная синхронизация без debounce.</summary>
    public void Flush()
    {
        _cts?.Cancel();
        var parsed = _parser(InputValue);
        _target.Set(parsed);
    }

    /// <summary>Сброс значения.</summary>
    public void Reset(T value)
    {
        InputValue = _formatter(value);
        _target.Set(value);
    }

    private static T DefaultParser(string s) => (T)Convert.ChangeType(s, typeof(T));

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
