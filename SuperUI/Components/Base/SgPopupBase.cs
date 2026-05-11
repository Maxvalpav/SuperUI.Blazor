// Файл: Components/Base/SgPopupBase.cs
// Зависимости: SgOverlayBase (уровень 3B)

namespace SuperUI.Components.Base;

/// <summary>
/// УРОВЕНЬ 4: Базовый класс для popup-компонентов (Popover, Dropdown, Tooltip).
/// Добавляет: anchor positioning, trigger management, auto-placement.
/// </summary>
public abstract class SgPopupBase : SgOverlayBase
{
    [Parameter] public SgPlacement Placement { get; set; } = SgPlacement.Bottom;
    [Parameter] public int Offset { get; set; } = 4;
    [Parameter] public bool AutoPlacement { get; set; } = true;
    [Parameter] public PopupTrigger Trigger { get; set; } = PopupTrigger.Click;
    [Parameter] public Microsoft.AspNetCore.Components.ElementReference? AnchorRef { get; set; }

    protected SgPlacement CurrentPlacement { get; private set; }

    protected override void OnComponentInitialized()
    {
        base.OnComponentInitialized();
        CurrentPlacement = Placement;
        HasBackdrop = false; // popup без backdrop по умолчанию
    }

    protected override int GetCloseAnimationDuration() => 150; // popup быстрее закрывается

    protected override string GetComponentPrefix() => "popup";
}

public enum PopupTrigger { Click, Hover, Focus, Manual }

public enum SgPlacement
{
    Top, TopStart, TopEnd,
    Bottom, BottomStart, BottomEnd,
    Left, LeftStart, LeftEnd,
    Right, RightStart, RightEnd
}
