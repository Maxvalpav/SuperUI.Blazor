// SgStore.cs — Улучшенное хранилище состояния с поддержкой .NET 8+ 
// Интеграция с PersistentComponentState, Undo/Redo, Time-travel debugging 
 
using System.Collections.Concurrent; 
using System.Text.Json; 
using Microsoft.AspNetCore.Components; 
using Microsoft.Extensions.Logging; 
using SuperUI.Base.Reactive;
using SuperUI.Base.Services;

namespace SuperUI.Base; 
 
/// <summary> 
/// Централизованное хранилище состояния приложения. 
/// 
/// Улучшения: 
/// - Интеграция с PersistentComponentState (.NET 8+) 
/// - Поддержка снапшотов для time-travel debugging 
/// - Автоматическая синхронизация между вкладками (через SgBroadcastService) 
/// - Оптимистичные обновления 
/// - Селекторы для эффективной подписки на части состояния 
/// </summary> 
public class SgStore : IDisposable, IAsyncDisposable 
 { 
     // ────────────────────────────────────────────── 
     //  Поля 
     // ────────────────────────────────────────────── 
 
     private readonly ILogger<SgStore> _logger; 
     private readonly ISgBroadcastService? _broadcastService; 
     private readonly PersistentComponentState? _persistentState; 
     private readonly ConcurrentDictionary<string, object> _state = new(); 
     private readonly ConcurrentDictionary<string, HashSet<Action>> _subscribers = new(); 
     private readonly ConcurrentStack<Dictionary<string, object>> _undoStack = new(); 
     private readonly ConcurrentStack<Dictionary<string, object>> _redoStack = new(); 
     private readonly List<StoreSnapshot> _snapshots = new(); 
     private readonly object _lock = new(); 
     private bool _isDisposed; 
     private int _maxUndoSteps = 50; 
     private int _maxSnapshots = 100; 
 
     // ────────────────────────────────────────────── 
     //  Конструктор 
     // ────────────────────────────────────────────── 
 
     public SgStore( 
         ILogger<SgStore> logger, 
         ISgBroadcastService? broadcastService = null, 
         PersistentComponentState? persistentState = null) 
     { 
         _logger = logger; 
         _broadcastService = broadcastService; 
         _persistentState = persistentState; 

         // Подписка на внешние обновления через Broadcast
         _broadcastService?.Subscribe<SgStoreChangedMessage>(msg => 
         {
             if (_state.TryGetValue(msg.Key, out var current))
             {
                 try 
                 {
                     var newValue = JsonSerializer.Deserialize(msg.SerializedValue, current.GetType());
                     if (newValue != null)
                     {
                         _state[msg.Key] = newValue;
                         NotifySubscribers(msg.Key);
                     }
                 }
                 catch (Exception ex)
                 {
                     _logger.LogError(ex, "Error deserializing broadcasted store update for key {Key}", msg.Key);
                 }
             }
         });
     } 
 
     // ────────────────────────────────────────────── 
     //  Получение / Установка состояния 
     // ────────────────────────────────────────────── 
 
     /// <summary> 
     /// Получить значение по ключу. 
     /// </summary> 
     public T? Get<T>(string key) 
     { 
         if (_state.TryGetValue(key, out var value) && value is T typed) 
             return typed; 
         return default; 
     } 
 
     /// <summary> 
     /// Установить значение и уведомить подписчиков. 
     /// </summary> 
     public void Set<T>(string key, T value) 
     { 
         var oldValue = Get<T>(key); 
         _state[key] = value!; 
 
         // Push в undo стек 
         PushUndo(key, oldValue, value); 
 
         // Уведомляем локальных подписчиков 
         NotifySubscribers(key); 
 
         // Броадкастим изменение другим вкладкам 
         if (_broadcastService != null && value != null) 
         { 
             _ = _broadcastService.PublishAsync(new SgStoreChangedMessage(key, JsonSerializer.Serialize(value))); 
         } 
 
         // Сохраняем в PersistentState 
         _persistentState?.RegisterOnPersisting(() => 
         { 
             _persistentState.PersistAsJson($"store:{key}", value); 
             return Task.CompletedTask; 
         }); 
 
         _logger.LogDebug("Store[{Key}] = {Value}", key, value); 
     } 
 
     /// <summary> 
     /// Обновить значение атомарно (concurrent-safe). 
     /// </summary> 
     public void Update<T>(string key, Func<T?, T> updater) where T : class 
     { 
         lock (_lock) 
         { 
             var oldValue = Get<T>(key); 
             var newValue = updater(oldValue); 
             Set(key, newValue); 
         } 
     } 
 
     // ────────────────────────────────────────────── 
     //  Подписки 
     // ────────────────────────────────────────────── 
 
     /// <summary> 
     /// Подписаться на изменения определённого ключа. 
     /// Возвращает IDisposable для отписки. 
     /// </summary> 
     public IDisposable Subscribe(string key, Action callback) 
     { 
         var subscribers = _subscribers.GetOrAdd(key, _ => new HashSet<Action>()); 
         lock (subscribers)
         {
             subscribers.Add(callback); 
         }
 
         _logger.LogDebug("Subscribed to Store[{Key}], total: {Count}", key, subscribers.Count); 
 
         return new StoreSubscription(() => 
         { 
             if (_subscribers.TryGetValue(key, out var set)) 
             { 
                 lock (set)
                 {
                    set.Remove(callback); 
                    if (set.Count == 0) 
                        _subscribers.TryRemove(key, out _); 
                 }
             } 
         }); 
     } 
 
     /// <summary> 
     /// Подписаться на несколько ключей сразу. 
     /// </summary> 
     public IDisposable Subscribe(string[] keys, Action callback) 
     { 
         var disposables = keys.Select(k => Subscribe(k, callback)).ToArray(); 
         return new CompositeDisposable(disposables); 
     } 
 
     private void NotifySubscribers(string key) 
     { 
         if (_subscribers.TryGetValue(key, out var subscribers)) 
         { 
             Action[] snapshot;
             lock (subscribers)
             {
                 snapshot = subscribers.ToArray();
             }

             foreach (var callback in snapshot) 
             { 
                 try 
                 { 
                     callback(); 
                 } 
                 catch (Exception ex) 
                 { 
                     _logger.LogError(ex, "Store subscriber error for key {Key}", key); 
                 } 
             } 
         } 
     } 
 
     // ────────────────────────────────────────────── 
     //  Undo / Redo 
     // ────────────────────────────────────────────── 
 
     public int MaxUndoSteps 
     { 
         get => _maxUndoSteps; 
         set => _maxUndoSteps = Math.Max(1, value); 
     } 
 
     private void PushUndo<T>(string key, T? oldValue, T newValue) 
     { 
         var snapshot = new Dictionary<string, object>(_state); 
         _undoStack.Push(snapshot); 
 
         // Обрезаем стек 
         while (_undoStack.Count > _maxUndoSteps) 
             _undoStack.TryPop(out _); 
 
         // Очищаем redo стек при новом действии 
         _redoStack.Clear(); 
     } 
 
     public bool Undo() 
     { 
         if (!_undoStack.TryPop(out var snapshot)) return false; 
 
         // Сохраняем текущее состояние для redo 
         var currentSnapshot = new Dictionary<string, object>(_state); 
         _redoStack.Push(currentSnapshot); 
 
         // Восстанавливаем состояние 
         RestoreSnapshotInternal(snapshot); 
 
         _logger.LogDebug("Store: Undo applied. UndoStack={Undo}, RedoStack={Redo}", 
             _undoStack.Count, _redoStack.Count); 
         return true; 
     } 
 
     public bool Redo() 
     { 
         if (!_redoStack.TryPop(out var snapshot)) return false; 
 
         var currentSnapshot = new Dictionary<string, object>(_state); 
         _undoStack.Push(currentSnapshot); 
 
         RestoreSnapshotInternal(snapshot); 
 
         _logger.LogDebug("Store: Redo applied. UndoStack={Undo}, RedoStack={Redo}", 
             _undoStack.Count, _redoStack.Count); 
         return true; 
     } 
 
     private void RestoreSnapshotInternal(Dictionary<string, object> snapshot) 
     { 
         _state.Clear(); 
         var changedKeys = new HashSet<string>(); 
 
         foreach (var (key, value) in snapshot) 
         { 
             _state[key] = value; 
             changedKeys.Add(key); 
         } 
 
         // Уведомляем об изменении всех ключей 
         foreach (var key in changedKeys) 
             NotifySubscribers(key); 
     } 
 
     public bool CanUndo => !_undoStack.IsEmpty; 
     public bool CanRedo => !_redoStack.IsEmpty; 
 
     // ────────────────────────────────────────────── 
     //  Снапшоты (Time-Travel Debugging) 
     // ────────────────────────────────────────────── 
 
     public void TakeSnapshot(string label = "") 
     { 
         var snapshot = new StoreSnapshot( 
             DateTimeOffset.UtcNow, 
             label, 
             new Dictionary<string, object>(_state)); 
 
         _snapshots.Add(snapshot); 
         while (_snapshots.Count > _maxSnapshots) 
             _snapshots.RemoveAt(0); 
 
         _logger.LogDebug("Store snapshot taken: {Label} (#{Index})", label, _snapshots.Count); 
     } 
 
     public IReadOnlyList<StoreSnapshot> GetSnapshots() => _snapshots.AsReadOnly(); 
 
     public void RestoreSnapshot(int index) 
     { 
         if (index < 0 || index >= _snapshots.Count) 
             throw new ArgumentOutOfRangeException(nameof(index)); 
 
         RestoreSnapshotInternal(_snapshots[index].State); 
     } 
 
     // ────────────────────────────────────────────── 
     //  Селекторы (derived state) 
     // ────────────────────────────────────────────── 
 
     /// <summary> 
     /// Создать селектор — производное значение из нескольких ключей. 
     /// </summary> 
     public IReadOnlySignal<T> Select<T>(string[] keys, Func<IReadOnlyDictionary<string, object?>, T> selector) 
     { 
         var signal = new SgSignal<T>(selector(GetSnapshotInternal())); 
         Subscribe(keys, () => signal.Set(selector(GetSnapshotInternal()))); 
         return signal; 
     } 
 
     private Dictionary<string, object?> GetSnapshotInternal() 
     { 
         var dict = new Dictionary<string, object?>(); 
         foreach (var (key, value) in _state) 
             dict[key] = value; 
         return dict; 
     } 
 
     // ────────────────────────────────────────────── 
     //  Очистка 
     // ────────────────────────────────────────────── 
 
     public void Clear() 
     { 
         _state.Clear(); 
         _undoStack.Clear(); 
         _redoStack.Clear(); 
         _snapshots.Clear(); 
 
         foreach (var key in _subscribers.Keys.ToArray()) 
             NotifySubscribers(key); 
     } 
 
     public void Dispose() 
     { 
         if (_isDisposed) return; 
         _isDisposed = true; 
         _subscribers.Clear(); 
         _state.Clear(); 
         _undoStack.Clear(); 
         _redoStack.Clear(); 
         _snapshots.Clear(); 
     } 
 
     public ValueTask DisposeAsync() 
     { 
         Dispose(); 
         return ValueTask.CompletedTask; 
     } 
 
     // ────────────────────────────────────────────── 
     //  Вложенные типы 
     // ────────────────────────────────────────────── 
 
     public record StoreSnapshot(DateTimeOffset Timestamp, string Label, Dictionary<string, object> State); 
 
     private sealed class StoreSubscription : IDisposable 
     { 
         private readonly Action _onDispose; 
         public StoreSubscription(Action onDispose) => _onDispose = onDispose; 
         public void Dispose() => _onDispose(); 
     } 
 
     private sealed class CompositeDisposable : IDisposable 
     { 
         private readonly IDisposable[] _disposables; 
         public CompositeDisposable(IDisposable[] disposables) => _disposables = disposables; 
         public void Dispose() 
         { 
             foreach (var d in _disposables) d.Dispose(); 
         } 
     } 
 }

/// <summary>
/// Сообщение об изменении состояния хранилища.
/// </summary>
public sealed record SgStoreChangedMessage(string Key, string SerializedValue);
