// SuperUI/Base/Reactive/SgBatchEffect.cs
// НОВЫЙ ФАЙЛ: запуск нескольких эффектов в batch (один рендер)

namespace SuperUI.Base.Reactive;

/// <summary>
/// Запускает несколько SgEffect в batch-режиме.
/// Изменения всех зависимых сигналов вызывают только один рендер.
/// </summary>
public sealed class SgBatchEffect : IDisposable
{
    private readonly List<SgEffect> _effects = new();

    public SgBatchEffect Add(Action action, Action<Exception>? onError = null)
    {
        _effects.Add(new SgEffect(action, onError));
        return this;
    }

    public SgBatchEffect Add(Func<Task> action, Action<Exception>? onError = null)
    {
        _effects.Add(new SgEffect(action, onError));
        return this;
    }

    public void Subscribe(SgComponentBase component)
    {
        foreach (var e in _effects) e.Subscribe(component);
    }

    public void PauseAll()
    {
        foreach (var e in _effects) e.Pause();
    }

    public void ResumeAll()
    {
        foreach (var e in _effects) e.Resume();
    }

    public void Dispose()
    {
        foreach (var e in _effects) e.Dispose();
        _effects.Clear();
    }
}
