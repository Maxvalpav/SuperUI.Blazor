using Microsoft.AspNetCore.Components;
using SuperUI.Base.Reactive;

namespace SuperUI.Base;

/// <summary> 
/// Методы расширения для SgStore. 
/// </summary> 
public static class SgStoreExtensions 
{ 
    /// <summary> 
    /// Привязать SgSignal к ключу хранилища. 
    /// Изменения сигнала автоматически синхронизируются с хранилищем и наоборот. 
    /// </summary> 
    public static SgSignal<T> BindSignal<T>(this SgStore store, string key, T initialValue, string? debugName = null) 
    { 
        // Восстанавливаем из хранилища если есть 
        var stored = store.Get<T>(key); 
        var signal = new SgSignal<T>(stored ?? initialValue, debugName ?? $"Store:{key}"); 
 
        // Синхронизация Signal → Store 
        var observer = new StoreSignalSyncObserver(() => store.Set(key, signal.Value)); 
        signal.Subscribe(observer); 
 
        // Синхронизация Store → Signal 
        store.Subscribe(key, () => 
        { 
            var storeValue = store.Get<T>(key); 
            signal.Set(storeValue!); 
        }); 
 
        return signal; 
    } 

    private class StoreSignalSyncObserver : ISignalObserver
    {
        private readonly Action _action;
        public StoreSignalSyncObserver(Action action) => _action = action;
        public void OnSignalChanged(ISgSignal signal) => _action();
        public void OnSignalRead(ISgSignal signal) { }
    }
} 
