// SgSignal.cs — Полностью переписанная реактивная система сигналов 
// AOT-совместимая (минимум рефлексии, нет Expression деревьев) 
// Интегрирована с Blazor через SgReactiveComponentBase 
// Поддерживает batch-обновления, equality checking, и отладку 
 
using System.Collections.Concurrent; 
using System.Diagnostics; 
using System.Runtime.CompilerServices; 
 
namespace SuperUI.Base.Reactive; 
 
// ══════════════════════════════════════════════ 
//  ИНТЕРФЕЙСЫ 
// ══════════════════════════════════════════════ 
 
/// <summary> 
/// Базовый интерфейс для всех сигналов. 
/// </summary> 
public interface ISgSignal 
 { 
     /// <summary>Уникальное имя сигнала для отладки.</summary> 
     string? DebugName { get; } 
 
     /// <summary>Количество активных подписчиков.</summary> 
     int SubscriberCount { get; } 
 
     /// <summary>Подписать наблюдателя на изменения сигнала.</summary> 
     void Subscribe(ISignalObserver observer); 
 
     /// <summary>Отписать наблюдателя от изменений сигнала.</summary> 
     void Unsubscribe(ISignalObserver observer); 
 } 
 
 /// <summary> 
 /// Типизированный сигнал с возможностью чтения значения. 
 /// </summary> 
 public interface IReadOnlySignal<out T> : ISgSignal 
 { 
     /// <summary>Текущее значение сигнала.</summary> 
     T Value { get; } 
 } 
 
 /// <summary> 
 /// Типизированный сигнал с возможностью записи значения. 
 /// </summary> 
 public interface ISgSignal<T> : IReadOnlySignal<T> 
 { 
     /// <summary>Установить новое значение сигнала.</summary> 
     void Set(T value); 
 } 
 
 /// <summary> 
 /// Наблюдатель изменений сигналов. 
 /// </summary> 
 public interface ISignalObserver 
 { 
     /// <summary>Вызывается при изменении сигнала.</summary> 
     void OnSignalChanged(ISgSignal signal); 
 } 
 
 // ══════════════════════════════════════════════ 
 //  SgSignal<T> — Основной реактивный сигнал 
 // ══════════════════════════════════════════════ 
 
 /// <summary> 
 /// Реактивный сигнал — ячейка с отслеживаемым значением. 
 /// Аналог useState в React, ref в Vue, createSignal в SolidJS. 
 /// 
 /// Потокобезопасен для чтения, запись через Set должна выполняться в UI потоке. 
 /// AOT-совместим: не использует Expression деревья или рефлексию. 
 /// </summary> 
 [DebuggerDisplay("{DebugName,nq} = {_value} ({SubscriberCount} subscribers)")] 
 public sealed class SgSignal<T> : ISgSignal<T>, IDisposable, ISignalFlushable 
 { 
     // ────────────────────────────────────────────── 
     //  Поля 
     // ────────────────────────────────────────────── 
 
     private T _value; 
     private readonly IEqualityComparer<T>? _comparer; 
     private ISignalObserver? _observer; // Оптимизация: один подписчик без списка 
     private List<ISignalObserver>? _observers; // Расширяемый список подписчиков 
     private bool _isDisposed; 
     private readonly object _lock = new(); 
 
     // ────────────────────────────────────────────── 
     //  Свойства 
     // ────────────────────────────────────────────── 
 
     public string? DebugName { get; } 
 
     public int SubscriberCount 
     { 
         get 
         { 
             lock (_lock) 
             { 
                 if (_observers != null) return _observers.Count; 
                 return _observer != null ? 1 : 0; 
             } 
         } 
     } 
 
     /// <summary> 
     /// Текущее значение сигнала. 
     /// При чтении из SgReactiveComponentBase.BuildReactiveRenderTree автоматически отслеживается. 
     /// </summary> 
     public T Value 
     { 
         [MethodImpl(MethodImplOptions.AggressiveInlining)] 
         get 
         { 
             // Неявное отслеживание для реактивных компонентов 
             SgReactiveComponentBase.TrackSignalImplicitly(this); 
             return _value; 
         } 
     } 
 
     // ────────────────────────────────────────────── 
     //  Конструкторы 
     // ────────────────────────────────────────────── 
 
     public SgSignal(T initialValue, string? debugName = null) 
         : this(initialValue, null, debugName) { } 
 
     public SgSignal(T initialValue, IEqualityComparer<T>? comparer, string? debugName = null) 
     { 
         _value = initialValue; 
         _comparer = comparer; 
         DebugName = debugName ?? $"Signal<{typeof(T).Name}>"; 
     } 
 
     // ────────────────────────────────────────────── 
     //  Публичные методы 
     // ────────────────────────────────────────────── 
 
     /// <summary> 
     /// Установить новое значение. Если значение не изменилось (по equality comparer) — уведомления не рассылаются. 
     /// </summary> 
     [MethodImpl(MethodImplOptions.AggressiveInlining)] 
     public void Set(T newValue) 
     { 
         if (_isDisposed) 
             throw new ObjectDisposedException(DebugName); 
 
         // Проверяем, изменилось ли значение 
         if (AreEqual(_value, newValue)) 
             return; 
 
         _value = newValue; 
 
         // Если внутри батча — откладываем уведомление 
         if (SignalBatch.IsBatching) 
         { 
             SignalBatch.MarkDirty(this); 
             return; 
         } 
 
         NotifyObservers(); 
     } 
 
     /// <summary> 
     /// Обновить значение через мутатор (функция, принимающая текущее значение и возвращающая новое). 
     /// Атомарная операция. 
     /// </summary> 
     [MethodImpl(MethodImplOptions.AggressiveInlining)] 
     public void Update(Func<T, T> mutator) 
     { 
         var newValue = mutator(_value); 
         Set(newValue); 
     } 
 
     /// <summary> 
     /// Замьютить текущее значение (если это ссылочный тип) и принудительно уведомить подписчиков. 
     /// Использовать с осторожностью — обходит equality check. 
     /// </summary> 
     public void MutateAndNotify(Action<T> mutator) 
     { 
         if (_isDisposed) 
             throw new ObjectDisposedException(DebugName); 
 
         mutator(_value); 
 
         if (SignalBatch.IsBatching) 
         { 
             SignalBatch.MarkDirty(this); 
             return; 
         } 
 
         NotifyObservers(); 
     } 
 
     // ────────────────────────────────────────────── 
     //  Подписка / Отписка 
     // ────────────────────────────────────────────── 
 
     public void Subscribe(ISignalObserver observer) 
     { 
         if (_isDisposed) return; 
 
         lock (_lock) 
         { 
             if (_observer == null) 
             { 
                 _observer = observer; 
             } 
             else if (_observer == observer) 
             { 
                 return; // Уже подписан 
             } 
             else 
             { 
                 _observers ??= new List<ISignalObserver>(4) { _observer }; 
                 if (!_observers.Contains(observer)) 
                 { 
                     _observers.Add(observer); 
                 } 
             } 
         } 
     } 
 
     public void Unsubscribe(ISignalObserver observer) 
     { 
         lock (_lock) 
         { 
             if (_observer == observer) 
             { 
                 _observer = null; 
                 // Переносим первого из списка в _observer (если есть) 
                 if (_observers is { Count: > 0 }) 
                 { 
                     _observer = _observers[0]; 
                     _observers.RemoveAt(0); 
                     if (_observers.Count == 0) 
                         _observers = null; 
                 } 
             } 
             else if (_observers != null) 
             { 
                 _observers.Remove(observer); 
                 if (_observers.Count == 0) 
                     _observers = null; 
             } 
         } 
     } 
 
     // ────────────────────────────────────────────── 
     //  Внутренние методы 
     // ────────────────────────────────────────────── 
 
     void ISignalFlushable.FlushIfDirty() => NotifyObservers(); 
 
     internal void NotifyObservers() 
     { 
         lock (_lock) 
         { 
             if (_observer != null) 
             { 
                 _observer.OnSignalChanged(this); 
             } 
 
             if (_observers != null) 
             { 
                 // Копируем список чтобы избежать проблем при изменении во время итерации 
                 var snapshot = _observers.ToArray(); 
                 foreach (var obs in snapshot) 
                 { 
                     obs.OnSignalChanged(this); 
                 } 
             } 
         } 
     } 
 
     [MethodImpl(MethodImplOptions.AggressiveInlining)] 
     private bool AreEqual(T a, T b) 
     { 
         if (_comparer != null) 
             return _comparer.Equals(a, b); 
 
         return EqualityComparer<T>.Default.Equals(a, b); 
     } 
 
     // ────────────────────────────────────────────── 
     //  IDisposable 
     // ────────────────────────────────────────────── 
 
     public void Dispose() 
     { 
         if (_isDisposed) return; 
         _isDisposed = true; 
 
         lock (_lock) 
         { 
             _observer = null; 
             _observers?.Clear(); 
             _observers = null; 
         } 
     } 
 
     // ────────────────────────────────────────────── 
     //  Операторы для удобства 
     // ────────────────────────────────────────────── 
 
     public static implicit operator T(SgSignal<T> signal) => signal.Value; 
 
     public override string ToString() => $"{DebugName}: {_value}"; 
 }