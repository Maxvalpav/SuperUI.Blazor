// Файл: Utilities/SmartShouldRender.cs
// ИННОВАЦИЯ: Автоматическое определение нужности рендера через снапшоты параметров

namespace SuperUI.Utilities;

/// <summary>
/// Умный ShouldRender: сравнивает снапшоты параметров.
/// Работает с value types и IEquatable через generic.
/// 
/// ПРОБЛЕМА: Blazor всегда вызывает рендер при любых параметрах родителя.
/// ParameterState решает это для отдельных параметров.
/// SmartShouldRender — для компонентов без ParameterState.
/// 
/// ИСПОЛЬЗОВАНИЕ в SgComponentBase.OnShouldRender():
/// return _smartRender.HasChanged(this);
/// </summary>
public sealed class SmartShouldRender<TSnapshot> where TSnapshot : IEquatable<TSnapshot>
{
    private TSnapshot? _lastSnapshot;
    private bool _firstCall = true;

    public bool HasChanged(TSnapshot currentSnapshot)
    {
        if (_firstCall)
        {
            _firstCall = false;
            _lastSnapshot = currentSnapshot;
            return true; // первый рендер всегда нужен
        }

        if (_lastSnapshot is not null && _lastSnapshot.Equals(currentSnapshot))
            return false; // нет изменений — пропускаем рендер

        _lastSnapshot = currentSnapshot;
        return true;
    }

    public void ForceNext() => _firstCall = true;
}
