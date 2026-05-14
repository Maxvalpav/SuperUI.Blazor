using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Base.Reactive;
using SuperUI.Base.Utilities;

namespace SuperUI.Components;

public partial class SgButton : SgReactiveComponentBase
{
    private bool _isDebouncing;
    private CancellationTokenSource? _debounceCts;

    [Parameter] public string? Text { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? Icon { get; set; }
    [Parameter] public SgButtonType Type { get; set; } = SgButtonType.Button;
    [Parameter] public SgButtonVariant Variant { get; set; } = SgButtonVariant.Default;
    [Parameter] public SgSize Size { get; set; } = SgSize.Md;
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Loading { get; set; }
    [Parameter] public bool Block { get; set; }
    [Parameter] public string? Title { get; set; }
    [Parameter] public bool IsToggle { get; set; }
    [Parameter] public bool Pressed { get; set; }
    [Parameter] public EventCallback<bool> PressedChanged { get; set; }
    [Parameter] public int DebounceInterval { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    protected override string ComponentPrefix => "btn";

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    protected override void BuildReactiveRenderTree(RenderTreeBuilder builder)
    {
        // This method is called by SgReactiveComponentBase.BuildRenderTree.
        // For Razor components, we use the EnterScope pattern in the .razor file,
        // so we don't need to do anything here.
    }

    private string GetButtonClasses() => Css("sgc-btn")
        .AddEnum(Variant, "sgc-btn-")
        .AddEnum(Size, "sgc-")
        .AddIf("sgc-block", Block)
        .AddIf("sgc-debouncing", _isDebouncing)
        .AddIf("sgc-pressed", IsToggle && Pressed)
        .Add(Class)
        .ToString();

    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var builder = new AriaBuilder();
        builder.Label(Title ?? Text)
               .Disabled(Disabled)
               .Busy(Loading);

        if (IsToggle)
            builder.Pressed(Pressed);

        return builder.Build();
    }

    private async Task OnClickAsync(MouseEventArgs e)
    {
        if (Disabled || Loading || _isDebouncing) return;

        if (DebounceInterval > 0)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = CancellationTokenSource.CreateLinkedTokenSource(ComponentToken);
            _isDebouncing = true;
            await RefreshAsync();
            _ = ResetDebounceAsync(DebounceInterval, _debounceCts.Token);
        }

        if (IsToggle)
        {
            Pressed = !Pressed;
            await PressedChanged.InvokeAsync(Pressed);
        }

        await OnClick.InvokeAsync(e);
    }

    private async Task ResetDebounceAsync(int interval, CancellationToken token)
    {
        try
        {
            await Task.Delay(interval, token);
            if (IsDisposed) return;
            _isDebouncing = false;
            await RefreshAsync();
        }
        catch (OperationCanceledException) { }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
        }
        base.Dispose(disposing);
    }
}
