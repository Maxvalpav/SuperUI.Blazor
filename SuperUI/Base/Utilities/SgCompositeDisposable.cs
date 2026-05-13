// SuperUI/Base/Utilities/SgCompositeDisposable.cs
// НОВЫЙ: удобный контейнер для управления несколькими IDisposable
//
// Использование в компонентах:
//   private readonly SgCompositeDisposable _disposables = new();
//
//   protected override void OnInitialized()
//   {
//       _disposables += notificationService.Subscribe(OnNotification);
//       _disposables += keyboardService.Register("Escape", OnEscape);
//   }
//
//   protected override async ValueTask DisposeComponentAsync()
//   {
//       _disposables.Dispose();
//       await base.DisposeComponentAsync();
//   }

namespace SuperUI.Base.Utilities;

/// <summary>
/// Контейнер для групповой отписки нескольких IDisposable.
/// Потокобезопасен — можно использовать как на Server, так и на WASM.
/// </summary>
public sealed class SgCompositeDisposable : IDisposable
{
    private readonly List<IDisposable> _disposables = [];
    private int _disposed;

    /// <summary>Добавить disposable в контейнер.</summary>
    public void Add(IDisposable disposable)
    {
        ArgumentNullException.ThrowIfNull(disposable);
        if (Volatile.Read(ref _disposed) == 1)
        {
            // Уже disposed — сразу освобождаем
            disposable.Dispose();
            return;
        }
        lock (_disposables)
            _disposables.Add(disposable);
    }

    /// <summary>Оператор += для удобства.</summary>
    public static SgCompositeDisposable operator +(
        SgCompositeDisposable composite,
        IDisposable disposable)
    {
        composite.Add(disposable);
        return composite;
    }

    /// <summary>Освободить все подписки.</summary>
    public void Clear()
    {
        List<IDisposable> toDispose;
        lock (_disposables)
        {
            toDispose = new List<IDisposable>(_disposables);
            _disposables.Clear();
        }
        foreach (var d in toDispose)
        {
            try { d.Dispose(); }
            catch { /* игнорируем */ }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        Clear();
    }
}
