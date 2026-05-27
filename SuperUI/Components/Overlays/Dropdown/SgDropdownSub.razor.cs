namespace SuperUI.Components;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Enums;

public partial class SgDropdownSub : IDisposable
{
    private bool _open;
    private ElementReference _triggerRef;

    [CascadingParameter] private SgDropdown? ParentDropdown { get; set; }

    /// <summary>Submenu trigger label.</summary>
    [Parameter] public string Text { get; set; } = "";

    /// <summary>Optional leading icon (raw SVG markup).</summary>
    [Parameter] public string? Icon { get; set; }

    /// <summary>Optional icon color.</summary>
    [Parameter] public string? IconColor { get; set; }

    /// <summary>Whether the submenu is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Submenu items.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Additional CSS class on the submenu item.</summary>
    [Parameter] public string? CssClass { get; set; }

    public bool IsOpen => _open;

    internal void Open()
    {
        if (Disabled) return;
        ParentDropdown?.CloseAllSubs();
        _open = true;
        StateHasChanged();
    }

    internal void Close()
    {
        if (!_open) return;
        _open = false;
        StateHasChanged();
    }

    internal void Toggle()
    {
        if (_open) Close();
        else Open();
    }

    private void HandleTriggerClick()
    {
        Toggle();
    }

    private void HandleTriggerMouseEnter()
    {
        if (ParentDropdown?.Trigger == SgDropdownTrigger.Hover)
            Open();
    }

    public void Dispose()
    {
        ParentDropdown?.UnregisterSub(this);
    }

    protected override void OnInitialized()
    {
        ParentDropdown?.RegisterSub(this);
    }
}
