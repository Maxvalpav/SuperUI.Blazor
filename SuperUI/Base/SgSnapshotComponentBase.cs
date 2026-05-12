// SuperUI/Base/SgSnapshotComponentBase.cs
//
// Компонент с оптимизацией рендера через снапшоты параметров.
// Перерисовывается ТОЛЬКО при реальном изменении параметров (структурное сравнение).
//
// Аналог React.PureComponent / React.memo.
//
// НОВОЕ:
// 1. ShouldRender — сравнивает параметры через снапшот.
// 2. CaptureSnapshot() — вызывается автоматически после OnParametersSet.
// 3. ParametersChanged() — виртуальный: переопределите для custom-сравнения.

namespace SuperUI.Base;

/// <summary>
/// Базовый класс с оптимизацией рендера через снапшоты параметров.
/// </summary>
/// <remarks>
/// Перерисовывается только при реальном изменении параметров.
/// Аналог <c>React.PureComponent</c> / <c>shouldComponentUpdate</c>.
/// </remarks>
public abstract class SgSnapshotComponentBase : SgComponentBase
{
    private IReadOnlyDictionary<string, object>? _snapshot;
    private bool _firstRender = true;

    protected override bool ShouldRender()
    {
        if (!base.ShouldRender()) return false;
        if (_firstRender) return true;
        return ParametersChanged();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        CaptureSnapshot();
        _firstRender = false;
    }

    /// <summary>
    /// Сравнить текущие параметры со снапшотом.
    /// Переопределите для custom-сравнения.
    /// </summary>
    protected virtual bool ParametersChanged()
    {
        if (_snapshot is null) return true;
        if (AdditionalAttributes is null && _snapshot.Count == 0) return false;
        if (AdditionalAttributes?.Count != _snapshot.Count) return true;

        if (AdditionalAttributes is not null)
            foreach (var kvp in AdditionalAttributes)
                if (!_snapshot.TryGetValue(kvp.Key, out var old) || !Equals(old, kvp.Value))
                    return true;

        return false;
    }

    /// <summary>Зафиксировать текущие параметры как снапшот.</summary>
    protected void CaptureSnapshot()
    {
        _snapshot = AdditionalAttributes is null
            ? new Dictionary<string, object>()
            : new Dictionary<string, object>(AdditionalAttributes);
    }
}
