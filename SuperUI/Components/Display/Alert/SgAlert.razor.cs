using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Enums;
using SuperUI.Localization;

namespace SuperUI.Components;

/// <summary>
/// A contextual notification banner with 4 variants, icons, actions, dismiss, auto-timeout, and elevation.
/// </summary>
public partial class SgAlert : IDisposable
{
    private bool _visible = true;
    private bool _dismissing;
    private bool _collapsed;
    private CancellationTokenSource? _timeoutCts;
    private bool _disposed;

    /// <summary>Visual variant: Info (default), Success, Warn, Danger.</summary>
    [Parameter] public SgAlertVariant Variant { get; set; } = SgAlertVariant.Info;

    /// <summary>Bold title text displayed above the message.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Message text. Ignored if <see cref="ChildContent"/> is provided.</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Custom icon (text, emoji, or SVG markup). When null, uses default variant icon.</summary>
    [Parameter] public string? Icon { get; set; }

    /// <summary>Custom icon render fragment. Takes precedence over <see cref="Icon"/> when set.</summary>
    [Parameter] public RenderFragment? IconTemplate { get; set; }

    /// <summary>Whether to show the icon area. Default true.</summary>
    [Parameter] public bool ShowIcon { get; set; } = true;

    /// <summary>Whether the alert can be dismissed with a close button.</summary>
    [Parameter] public bool Dismissible { get; set; }

    /// <summary>Compact padding mode.</summary>
    [Parameter] public bool Dense { get; set; }

    /// <summary>Size preset. Default Md.</summary>
    [Parameter] public SgSize Size { get; set; } = SgSize.Md;

    /// <summary>Whether to stretch to full container width.</summary>
    [Parameter] public bool FullWidth { get; set; }

    /// <summary>Whether to show a loading/skeleton state.</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>Alias for <see cref="Dismissible"/> — for backward compatibility.</summary>
    [Parameter]
    [Obsolete("Use Dismissible instead")]
    public bool Closable
    {
        get => Dismissible;
        set => Dismissible = value;
    }

    /// <summary>Whether to show the left accent border. Default true.</summary>
    [Parameter] public bool ShowBorder { get; set; } = true;

    /// <summary>Whether to show a subtle shadow elevation.</summary>
    [Parameter] public bool Elevated { get; set; }

    /// <summary>Auto-dismiss after this many milliseconds. Default 0 = no auto-dismiss.</summary>
    [Parameter] public int Timeout { get; set; }

    /// <summary>Whether to show a progress bar when <see cref="Timeout"/> is active.</summary>
    [Parameter] public bool ShowProgress { get; set; }

    /// <summary>Fired when the timeout elapses, before the alert closes.</summary>
    [Parameter] public EventCallback OnTimeout { get; set; }

    /// <summary>Whether the body content is collapsible via a toggle button.</summary>
    [Parameter] public bool Collapsible { get; set; }

    /// <summary>Whether the collapsible body is collapsed. Two-way bindable.</summary>
    [Parameter] public bool Collapsed { get; set; }

    /// <summary>Fires when Collapsed changes (two-way binding).</summary>
    [Parameter] public EventCallback<bool> CollapsedChanged { get; set; }

    /// <summary>ARIA role override. Default derived from variant: "alert" for Danger/Warn, "status" otherwise.</summary>
    [Parameter] public string? Role { get; set; }

    /// <summary>Whether the alert is visible. Two-way bindable.</summary>
    [Parameter] public bool? Visible { get; set; }

    /// <summary>Fires when Visible changes (two-way binding).</summary>
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }

    /// <summary>Additional CSS classes.</summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>Inline styles.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Custom body content. Takes precedence over <see cref="Text"/>.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Custom action buttons/content rendered after the body.</summary>
    [Parameter] public RenderFragment? ActionsContent { get; set; }

    /// <summary>Convenience primary-action button label. Renders an inline button below the body.</summary>
    [Parameter] public string? PrimaryActionText { get; set; }

    /// <summary>Click callback for the primary action button.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnPrimaryAction { get; set; }

    /// <summary>Custom content rendered below the body, separated by a divider.</summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>Whether to render child content as a bulleted list (&lt;ul&gt;).</summary>
    [Parameter] public bool ListMode { get; set; }

    /// <summary>Full-width banner mode with no border-radius.</summary>
    [Parameter] public bool Banner { get; set; }

    /// <summary>Place the icon above the title (centered) instead of on the left.</summary>
    [Parameter] public bool IconTop { get; set; }

    /// <summary>Fires when the alert is dismissed (via close button or timeout).</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>Click event on the alert root.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Keyboard event on the alert root (Escape to dismiss, Enter for primary action).</summary>
    [Parameter] public EventCallback<KeyboardEventArgs> OnKeyDown { get; set; }

    /// <summary>Captures unmatched HTML attributes.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private ISuperUILocalizer Localizer { get; set; } = default!;

    private Action? _localeChangedHandler;

    private string VariantClass => Variant switch
    {
        SgAlertVariant.Success => "sgc-success",
        SgAlertVariant.Warn => "sgc-warn",
        SgAlertVariant.Danger => "sgc-danger",
        _ => "sgc-info"
    };

    private string ResolvedRole => Role ?? (Variant is SgAlertVariant.Danger or SgAlertVariant.Warn ? "alert" : "status");
    private string ResolvedAriaLive => Variant is SgAlertVariant.Danger or SgAlertVariant.Warn ? "assertive" : "polite";

    private bool IsVisible => Visible ?? _visible;

    private string ComputedIcon => !string.IsNullOrWhiteSpace(Icon)
        ? Icon!
        : Variant switch
        {
            SgAlertVariant.Success => DefaultIcons.Success,
            SgAlertVariant.Warn => DefaultIcons.Warn,
            SgAlertVariant.Danger => DefaultIcons.Danger,
            _ => DefaultIcons.Info
        };

    private bool IsSvgIcon => ComputedIcon.StartsWith("<svg", StringComparison.OrdinalIgnoreCase);

    protected override void OnInitialized()
    {
        _localeChangedHandler = () => { try { InvokeAsync(StateHasChanged); } catch (ObjectDisposedException) { } catch (InvalidOperationException) { } catch (TaskCanceledException) { } };
        Localizer.OnLocaleChanged += _localeChangedHandler;
        _collapsed = Collapsed;
        StartTimeout();
    }

    protected override void OnParametersSet()
    {
        if (Visible is true)
        {
            _visible = true;
        }
        _collapsed = Collapsed;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && Timeout > 0)
        {
            StartTimeout();
        }
    }

    private void StartTimeout()
    {
        if (Timeout <= 0) return;

        _timeoutCts?.Cancel();
        _timeoutCts = new CancellationTokenSource();
        var token = _timeoutCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(Timeout, token);
                if (!token.IsCancellationRequested)
                {
                    await InvokeAsync(async () =>
                    {
                        if (OnTimeout.HasDelegate)
                            await OnTimeout.InvokeAsync();
                        await CloseAsync();
                    });
                }
            }
            catch (TaskCanceledException)
            {
                // Expected on manual dismiss or re-render
            }
        }, token);
    }

    private async Task CloseAsync()
    {
        if (_dismissing) return;
        _dismissing = true;
        StateHasChanged();

        await Task.Delay(250);

        _visible = false;
        _dismissing = false;

        if (VisibleChanged.HasDelegate)
            await VisibleChanged.InvokeAsync(false);

        if (OnClose.HasDelegate)
            await OnClose.InvokeAsync();
    }

    private void ToggleCollapse()
    {
        _collapsed = !_collapsed;
        Collapsed = _collapsed;
        if (CollapsedChanged.HasDelegate)
            CollapsedChanged.InvokeAsync(_collapsed);
    }

    private async Task HandlePrimaryClick(MouseEventArgs args)
    {
        if (OnPrimaryAction.HasDelegate)
            await OnPrimaryAction.InvokeAsync(args);
    }

    private async Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (OnKeyDown.HasDelegate)
            await OnKeyDown.InvokeAsync(args);

        if (args.Key is "Escape" && Dismissible)
        {
            await CloseAsync();
        }
        else if (args.Key is "Enter" && PrimaryActionText is not null && OnPrimaryAction.HasDelegate)
        {
            await OnPrimaryAction.InvokeAsync(new MouseEventArgs());
        }
    }

    private async Task HandleClick(MouseEventArgs args)
    {
        if (OnClick.HasDelegate)
            await OnClick.InvokeAsync(args);
    }

    /// <summary>Programmatically show the alert.</summary>
    public void Show()
    {
        _visible = true;
        _dismissing = false;
        StartTimeout();
        StateHasChanged();
    }

    /// <summary>Programmatically hide the alert (without animation).</summary>
    public void Hide()
    {
        _timeoutCts?.Cancel();
        _visible = false;
        _dismissing = false;
        StateHasChanged();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_localeChangedHandler is not null)
            Localizer.OnLocaleChanged -= _localeChangedHandler;
        _timeoutCts?.Cancel();
        _timeoutCts?.Dispose();
    }

    private string ComputedClass
    {
        get
        {
            var cls = "sgc-alert " + VariantClass;

            if (Dense) cls += " sgc-alert-dense";
            if (Elevated) cls += " sgc-alert-elevated";
            if (_dismissing) cls += " sgc-alert-dismissing";
            if (!ShowBorder) cls += " sgc-alert-noborder";
            if (FullWidth) cls += " sgc-alert-fullwidth";
            if (Loading) cls += " sgc-alert-loading";
            if (Collapsible) cls += " sgc-alert-collapsible";
            if (_collapsed) cls += " sgc-alert-collapsed";
            if (ShowProgress && Timeout > 0) cls += " sgc-alert-has-progress";
            if (Banner) cls += " sgc-alert-banner";
            if (IconTop) cls += " sgc-alert-icon-top";
            if (ListMode) cls += " sgc-alert-list-mode";

            if (Size != SgSize.Md) cls += Size switch
            {
                SgSize.Sm => " sgc-alert-sm",
                SgSize.Lg => " sgc-alert-lg",
                SgSize.Xl => " sgc-alert-xl",
                _ => ""
            };

            if (!string.IsNullOrEmpty(CssClass)) cls += " " + CssClass;

            return cls;
        }
    }

    /// <summary>Default SVG icon strings for each variant.</summary>
    private static class DefaultIcons
    {
        public const string Info = "<svg viewBox=\"0 0 20 20\" fill=\"currentColor\" width=\"16\" height=\"16\"><path fill-rule=\"evenodd\" d=\"M18 10a8 8 0 1 1-16 0 8 8 0 0 1 16 0Zm-7-4a1 1 0 1 1-2 0 1 1 0 0 1 2 0ZM9 9a.75.75 0 0 0 0 1.5h.253a.25.25 0 0 1 .244.304l-.459 2.066A1.75 1.75 0 0 0 10.747 15H11a.75.75 0 0 0 0-1.5h-.253a.25.25 0 0 1-.244-.304l.459-2.066A1.75 1.75 0 0 0 9.253 9H9Z\" clip-rule=\"evenodd\" /></svg>";
        public const string Success = "<svg viewBox=\"0 0 20 20\" fill=\"currentColor\" width=\"16\" height=\"16\"><path fill-rule=\"evenodd\" d=\"M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16Zm3.857-9.809a.75.75 0 0 0-1.214-.882l-3.483 4.79-1.88-1.88a.75.75 0 1 0-1.06 1.061l2.5 2.5a.75.75 0 0 0 1.137-.089l4-5.5Z\" clip-rule=\"evenodd\" /></svg>";
        public const string Warn = "<svg viewBox=\"0 0 20 20\" fill=\"currentColor\" width=\"16\" height=\"16\"><path fill-rule=\"evenodd\" d=\"M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495ZM10 5a.75.75 0 0 1 .75.75v3.5a.75.75 0 0 1-1.5 0v-3.5A.75.75 0 0 1 10 5Zm0 9a1 1 0 1 0 0-2 1 1 0 0 0 0 2Z\" clip-rule=\"evenodd\" /></svg>";
        public const string Danger = "<svg viewBox=\"0 0 20 20\" fill=\"currentColor\" width=\"16\" height=\"16\"><path fill-rule=\"evenodd\" d=\"M10 18a8 8 0 1 0 0-16 8 8 0 0 0 0 16ZM8.28 7.22a.75.75 0 0 0-1.06 1.06L8.94 10l-1.72 1.72a.75.75 0 1 0 1.06 1.06L10 11.06l1.72 1.72a.75.75 0 1 0 1.06-1.06L11.06 10l1.72-1.72a.75.75 0 0 0-1.06-1.06L10 8.94 8.28 7.22Z\" clip-rule=\"evenodd\" /></svg>";
    }
}
