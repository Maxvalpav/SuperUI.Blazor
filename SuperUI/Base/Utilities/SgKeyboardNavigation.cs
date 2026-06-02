// SuperUI/Base/Utilities/SgKeyboardNavigation.cs
// Helper-структуры для arrow/home/end/enter/escape-обработки в Listbox, Menu, Tabs.
// Реализация следует W3C ARIA APG (https://www.w3.org/WAI/ARIA/apg/).

namespace SuperUI.Base.Utilities;

/// <summary>
/// Keyboard navigation action: Roving Tab Index pattern.
/// </summary>
/// <remarks>
/// <para>Используется компонентами, реализующими ARIA Listbox/Menu/Tabs/Tree.
/// Концентрирует логику "что делать на ArrowUp/Down/Home/End/Enter/Escape" в одном
/// месте — иначе каждый компонент пишет свой switch.</para>
/// </remarks>
public static class SgKeyboardNavigation
{
    /// <summary>
    /// Ориентация списка. Определяет, какая клавиша (ArrowUp/Down или ArrowLeft/Right)
    /// двигает курсор.
    /// </summary>
    public enum Orientation
    {
        /// <summary>Вертикальный список (Listbox, Menu).</summary>
        Vertical,
        /// <summary>Горизонтальный список (Tabs, Toolbar).</summary>
        Horizontal,
        /// <summary>Обе ориентации (Tree, Grid — оба набора работают).</summary>
        Both,
    }

    /// <summary>
    /// Решает, какое действие нужно выполнить по нажатой клавише.
    /// </summary>
    /// <param name="key">Имя клавиши (KeyboardEventArgs.Key).</param>
    /// <param name="orientation">Ориентация списка.</param>
    /// <param name="itemCount">Общее число элементов.</param>
    /// <param name="currentIndex">Текущий индекс (-1 = ничего не выбрано).</param>
    /// <returns>Команда навигации, либо <see cref="NavAction.None"/> если клавиша не обрабатывается.</returns>
    public static NavAction Resolve(string? key, Orientation orientation, int itemCount, int currentIndex)
    {
        if (itemCount <= 0) return NavAction.None;
        key = key ?? "";

        switch (key)
        {
            case "Home":
                return new NavAction(0, NavKind.Move);
            case "End":
                return new NavAction(itemCount - 1, NavKind.Move);
            case "Enter":
            case " ":
                return new NavAction(currentIndex, NavKind.Activate);
            case "Escape":
                return new NavAction(currentIndex, NavKind.Cancel);
            case "ArrowDown":
                if (orientation is Orientation.Vertical or Orientation.Both)
                    return Next(currentIndex, itemCount);
                break;
            case "ArrowUp":
                if (orientation is Orientation.Vertical or Orientation.Both)
                    return Prev(currentIndex, itemCount);
                break;
            case "ArrowRight":
                if (orientation is Orientation.Horizontal or Orientation.Both)
                    return Next(currentIndex, itemCount);
                break;
            case "ArrowLeft":
                if (orientation is Orientation.Horizontal or Orientation.Both)
                    return Prev(currentIndex, itemCount);
                break;
            case "PageDown":
                return new NavAction(Math.Min(itemCount - 1, currentIndex + 10), NavKind.Move);
            case "PageUp":
                return new NavAction(Math.Max(0, currentIndex - 10), NavKind.Move);
        }
        return NavAction.None;
    }

    /// <summary>Clamps <paramref name="index"/> into [0, <paramref name="count"/>-1].</summary>
    public static int Clamp(int index, int count)
    {
        if (count <= 0) return -1;
        if (index < 0) return 0;
        if (index >= count) return count - 1;
        return index;
    }

    private static NavAction Next(int current, int count)
    {
        var next = current < 0 ? 0 : Math.Min(count - 1, current + 1);
        return new NavAction(next, NavKind.Move);
    }

    private static NavAction Prev(int current, int count)
    {
        var prev = current <= 0 ? 0 : current - 1;
        return new NavAction(prev, NavKind.Move);
    }
}

/// <summary>Action requested by <see cref="SgKeyboardNavigation.Resolve"/>.</summary>
/// <param name="Index">Target index. <c>-1</c> if no change.</param>
/// <param name="Kind">What to do.</param>
public readonly record struct NavAction(int Index, NavKind Kind)
{
    /// <summary>No-op (key not handled).</summary>
    public static readonly NavAction None = new(-1, NavKind.None);

    /// <summary>True if the action moves the focus/selection.</summary>
    public bool IsMove => Kind == NavKind.Move;

    /// <summary>True if the action activates the current item.</summary>
    public bool IsActivate => Kind == NavKind.Activate;

    /// <summary>True if the action cancels (e.g. Escape).</summary>
    public bool IsCancel => Kind == NavKind.Cancel;
}

/// <summary>Action kind.</summary>
public enum NavKind
{
    /// <summary>Key not handled.</summary>
    None,
    /// <summary>Move focus/selection to <see cref="NavAction.Index"/>.</summary>
    Move,
    /// <summary>Activate (Enter/Space on the current item).</summary>
    Activate,
    /// <summary>Cancel (Escape).</summary>
    Cancel,
}
