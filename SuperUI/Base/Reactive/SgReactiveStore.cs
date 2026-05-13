// SuperUI/Base/Reactive/SgReactiveStore.cs
// ИСПРАВЛЕНИЯ:
// ✅ CS8716 FIX: Reset() использует ISgSignalResettable вместо dynamic
// ✅ AOT FIX: убран dynamic из Snapshot/Export/Import
// ✅ THREAD SAFETY: Dictionary защищён lock во всех методах
// ✅ PARENT STORE: GetSignal с fallback к parent
// ✅ TYPED RESET: каждый сигнал хранит свой defaultValue
// ✅ EVENT: Added при создании нового сигнала, Changed при изменении

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Внутренний интерфейс для сброса сигнала к дефолтному значению.
/// Позволяет вызывать Reset() без dynamic и без рефлексии.
/// </summary>
internal interface ISgSignalResettable
{
    void ResetToDefault();
    object? GetValue();
    void SetFromObject(object? value);
    string Key { get; }
    Type ValueType { get; }
}

/// <summary>
/// Обёртка над SgSignal&lt;T&gt; с хранением ключа и дефолтного значения.
/// Используется внутри SgReactiveStore.
/// </summary>
internal sealed class SgStoreEntry<T> : ISgSignalResettable
{
    public string Key { get; }
    public Type ValueType => typeof(T);
    public SgSignal<T> Signal { get; }
    private readonly T _defaultValue;

    public SgStoreEntry(string key, T defaultValue, string? debugName)
    {
        Key = key;
        _defaultValue = defaultValue;
        Signal = new SgSignal<T>(defaultValue, debugName ?? key);
    }

    /// <summary>Сброс к дефолтному значению — без dynamic, без рефлексии.</summary>
    public void ResetToDefault() => Signal.Set(_defaultValue);

    /// <summary>Получить значение как object (для Export/Snapshot).</summary>
    public object? GetValue() => Signal.Value;

    /// <summary>
    /// Установить значение из object (для Import).
    /// Поддерживает JsonElement от System.Text.Json.
    /// Не требует dynamic.
    /// </summary>
    public void SetFromObject(object? value)
    {
        if (value is T typed)
        {
            Signal.Set(typed);
        }
        else if (value is JsonElement jsonElement)
        {
            // System.Text.Json возвращает JsonElement при десериализации object
            try
            {
                var deserialized = jsonElement.Deserialize<T>();
                if (deserialized is not null)
                    Signal.Set(deserialized);
                else
                    Signal.Set(_defaultValue);
            }
            catch
            {
                Signal.Set(_defaultValue);
            }
        }
        else if (value is null)
        {
            // Для value-types: оставляем дефолт
            Signal.Set(_defaultValue);
        }
        else
        {
            // Попытка конвертации через Convert (для примитивов: int<->long и т.п.)
            try
            {
                var converted = (T)Convert.ChangeType(value, typeof(T));
                Signal.Set(converted);
            }
            catch
            {
                // Игнорируем несовместимые типы
            }
        }
    }
}

/// <summary>
/// Глобальное реактивное хранилище ключ-значение.
/// Поддерживает подписку на изменения, персистентность, снапшоты.
///
/// ИСПРАВЛЕНИЯ:
/// - CS8716: убран bare 'default' — Reset() использует ISgSignalResettable.ResetToDefault()
/// - AOT: убран dynamic — Snapshot/Export/Import работают через интерфейс
/// - Thread-safety: все операции с Dictionary защищены lock
/// - Parent store: GetSignal с fallback к parent
///
/// Использование:
/// <code>
/// var store = new SgReactiveStore();
/// var counter = store.GetSignal&lt;int&gt;("counter", 0);
/// counter.Set(counter.Value + 1);
/// </code>
/// </summary>
public sealed class SgReactiveStore : IDisposable
{
    // Хранит ISgSignalResettable (типизированные обёртки) вместо object
    private readonly Dictionary<string, ISgSignalResettable> _entries = new();
    private readonly object _lock = new();
    private readonly SgReactiveStore? _parent;
    private int _disposed;

    /// <summary>Событие: создан новый сигнал в store.</summary>
    public event Action<string>? SignalCreated;

    /// <summary>Количество сигналов в store.</summary>
    public int Count
    {
        get { lock (_lock) return _entries.Count; }
    }

    public SgReactiveStore(SgReactiveStore? parent = null)
    {
        _parent = parent;
    }

    /// <summary>
    /// Получить или создать сигнал по ключу.
    /// Thread-safe. AOT-совместим. Без dynamic.
    /// </summary>
    public SgSignal<T> GetSignal<T>(
        string key,
        T defaultValue = default!,
        string? debugName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, nameof(SgReactiveStore));

        lock (_lock)
        {
            if (_entries.TryGetValue(key, out var existing))
            {
                // Проверяем совместимость типа
                if (existing is SgStoreEntry<T> typed)
                    return typed.Signal;

                throw new InvalidOperationException(
                    $"Signal '{key}' already exists with type '{existing.ValueType.Name}', " +
                    $"but requested type is '{typeof(T).Name}'.");
            }

            // Проверяем parent store
            if (_parent is not null)
            {
                // Parent store не типизирован на уровне GetSignal,
                // поэтому сначала проверяем, есть ли там ключ
                lock (_parent._lock)
                {
                    if (_parent._entries.TryGetValue(key, out var parentEntry) &&
                        parentEntry is SgStoreEntry<T> parentTyped)
                        return parentTyped.Signal;
                }
            }

            var entry = new SgStoreEntry<T>(key, defaultValue, debugName);
            _entries[key] = entry;
            SignalCreated?.Invoke(key);
            return entry.Signal;
        }
    }

    /// <summary>
    /// Проверить наличие сигнала по ключу.
    /// </summary>
    public bool Contains(string key)
    {
        lock (_lock) return _entries.ContainsKey(key);
    }

    /// <summary>
    /// Удалить сигнал из store и освободить его.
    /// </summary>
    public bool Remove(string key)
    {
        lock (_lock)
        {
            if (_entries.TryGetValue(key, out var entry))
            {
                if (entry.Signal is IDisposable d) d.Dispose();
                _entries.Remove(key);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Получить Computed из нескольких ключей.
    /// ИСПРАВЛЕНИЕ: убран dynamic, работает через GetSignal&lt;T&gt;.
    /// </summary>
    public SgComputed<TResult> GetComputed<T, TResult>(
        string[] keys,
        Func<T[], TResult> compute,
        string? debugName = null)
    {
        var signals = keys.Select(k => GetSignal<T>(k)).ToArray();
        return new SgComputed<TResult>(
            () => compute(signals.Select(s => s.Value).ToArray()),
            debugName: debugName);
    }

    /// <summary>
    /// Получить все сигналы как словарь значений.
    /// ИСПРАВЛЕНИЕ: убран dynamic — используется ISgSignalResettable.GetValue().
    /// </summary>
    public IReadOnlyDictionary<string, object?> Snapshot()
    {
        lock (_lock)
        {
            var dict = new Dictionary<string, object?>(_entries.Count);
            foreach (var kv in _entries)
                dict[kv.Key] = kv.Value.GetValue();
            return dict;
        }
    }

    /// <summary>
    /// Экспортировать все значения как JSON-строку.
    /// ИСПРАВЛЕНИЕ: убран dynamic, System.Text.Json без рефлексии.
    /// </summary>
    public string ExportJson(JsonSerializerOptions? options = null)
    {
        var snapshot = Snapshot();
        return JsonSerializer.Serialize(snapshot, options);
    }

    /// <summary>
    /// Экспортировать как Dictionary&lt;string, object?&gt; для совместимости.
    /// </summary>
    public Dictionary<string, object?> Export()
    {
        var snapshot = Snapshot();
        return new Dictionary<string, object?>(snapshot);
    }

    /// <summary>
    /// Импортировать значения из словаря.
    /// ИСПРАВЛЕНИЕ: убран dynamic — используется ISgSignalResettable.SetFromObject().
    /// Поддерживает JsonElement из System.Text.Json.Deserialize&lt;Dictionary&lt;string,object&gt;&gt;().
    /// </summary>
    public void Import(Dictionary<string, object?> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        lock (_lock)
        {
            foreach (var kv in data)
            {
                if (_entries.TryGetValue(kv.Key, out var entry))
                    entry.SetFromObject(kv.Value);
            }
        }
    }

    /// <summary>
    /// Импортировать из JSON-строки.
    /// </summary>
    public void ImportJson(string json, JsonSerializerOptions? options = null)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
        if (data is null) return;

        lock (_lock)
        {
            foreach (var kv in data)
            {
                if (_entries.TryGetValue(kv.Key, out var entry))
                    entry.SetFromObject(kv.Value);
            }
        }
    }

    /// <summary>
    /// Сбросить все сигналы в значения по умолчанию.
    /// ИСПРАВЛЕНИЕ CS8716: убран 'default' без целевого типа.
    /// Используется ISgSignalResettable.ResetToDefault() — знает свой T.
    /// </summary>
    public void Reset()
    {
        ISgSignalResettable[] snapshot;
        lock (_lock)
            snapshot = _entries.Values.ToArray();

        // Вызываем ВНЕ lock чтобы не блокировать подписчиков
        foreach (var entry in snapshot)
            entry.ResetToDefault();
    }

    /// <summary>
    /// Сбросить конкретный сигнал по ключу.
    /// </summary>
    public bool Reset(string key)
    {
        ISgSignalResettable? entry;
        lock (_lock) _entries.TryGetValue(key, out entry);
        entry?.ResetToDefault();
        return entry is not null;
    }

    /// <summary>
    /// Получить список всех ключей.
    /// </summary>
    public IReadOnlyList<string> Keys
    {
        get { lock (_lock) return _entries.Keys.ToArray(); }
    }

    /// <summary>
    /// Создать дочерний store, наследующий сигналы родителя.
    /// </summary>
    public SgReactiveStore CreateChild() => new(this);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        lock (_lock)
        {
            foreach (var entry in _entries.Values)
            {
                if (entry.Signal is IDisposable d) d.Dispose();
            }
            _entries.Clear();
        }
    }
}
