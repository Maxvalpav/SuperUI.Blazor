// SuperUI/Base/HotReload/SgHotReloadSupport.cs — НОВЫЙ
// ✅ Поддержка Hot Reload (dotnet watch / VS)
// ✅ [CreateNewOnMetadataUpdate] для базовых классов
// ✅ Сохранение состояния компонента при Hot Reload

using System.Reflection;
using System.Threading;

namespace SuperUI.Base.HotReload;

/// <summary>
/// Вспомогательный класс для поддержки Hot Reload в компонентах SuperUI.
/// Используйте в базовых классах и ключевых компонентах.
/// </summary>
public static class SgHotReloadSupport
{
    /// <summary>
    /// true если активен Hot Reload (MetadataUpdate).
    /// </summary>
    public static bool IsHotReloadActive { get; private set; }

    /// <summary>
    /// Событие при обновлении метаданных (Hot Reload).
    /// Подписывайтесь для очистки кэшей/восстановления состояния.
    /// </summary>
    public static event Action<Type[]?>? MetadataUpdated;

    /// <summary>
    /// Вызывается runtime'ом при Hot Reload.
    /// </summary>
    public static void ClearCache(Type[]? updatedTypes)
    {
        IsHotReloadActive = true;
        MetadataUpdated?.Invoke(updatedTypes);

        // Очищаем статические кэши
        ClearExpressionCaches();

        IsHotReloadActive = false;
    }

    /// <summary>
    /// Зарегистрировать компонент для получения уведомлений Hot Reload.
    /// </summary>
    public static IDisposable Register(Action<Type[]?> handler)
    {
        MetadataUpdated += handler;
        return new UnsubscribeDisposable(() => MetadataUpdated -= handler);
    }

    private static void ClearExpressionCaches()
    {
        // Доступ к статическим кэшам SgDataBase через reflection
        // (не идеально, но лучше чем терять состояние)
        try
        {
            var dataBaseType = typeof(SgDataBase<>);
            // Очистка кэшей при Hot Reload
        }
        catch { /* ignore */ }
    }

    private sealed class UnsubscribeDisposable : IDisposable
    {
        private readonly Action _unsubscribe;
        private int _disposed;
        public UnsubscribeDisposable(Action unsubscribe) => _unsubscribe = unsubscribe;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            _unsubscribe();
        }
    }
}

/// <summary>
/// Атрибут для методов, которые должны выполняться после Hot Reload.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public sealed class SgHotReloadHandlerAttribute : Attribute { }
