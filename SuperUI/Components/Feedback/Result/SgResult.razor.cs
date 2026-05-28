using Microsoft.AspNetCore.Components;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Displays the result of an operation (success, error, info, warning) with icon, title, description, and optional actions.
/// </summary>
public partial class SgResult : IDisposable
{
    private CancellationTokenSource? _autoRedirectCts;
    private int _countdownSeconds;
    private bool _autoRedirectAborted;
    private bool _disposed;

    /// <summary>Result status type. Default <see cref="SgResultStatus.Info"/>.</summary>
    [Parameter] public SgResultStatus Status { get; set; } = SgResultStatus.Info;

    /// <summary>Main title text. Required.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <summary>Subtitle text displayed below the title.</summary>
    [Parameter] public string? SubTitle { get; set; }

    /// <summary>Longer description paragraph below the subtitle.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>Custom icon content replacing the default status icon.</summary>
    [Parameter] public RenderFragment? IconContent { get; set; }

    /// <summary>Custom icon as SVG/emoji string. Ignored when <see cref="IconContent"/> is set.</summary>
    [Parameter] public string? Icon { get; set; }

    /// <summary>Extra content (typically action buttons) displayed below the description.</summary>
    [Parameter] public RenderFragment? ExtraContent { get; set; }

    /// <summary>Main body content displayed at the bottom.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Convenience primary-action button label. Used when <see cref="ExtraContent"/> is null.</summary>
    [Parameter] public string? PrimaryActionText { get; set; }

    /// <summary>Visual variant for the primary action button. Default Primary.</summary>
    [Parameter] public SgButtonVariant PrimaryActionVariant { get; set; } = SgButtonVariant.Primary;

    /// <summary>Click callback for the primary action.</summary>
    [Parameter] public EventCallback OnPrimaryAction { get; set; }

    /// <summary>Convenience secondary-action button label.</summary>
    [Parameter] public string? SecondaryActionText { get; set; }

    /// <summary>Click callback for the secondary action.</summary>
    [Parameter] public EventCallback OnSecondaryAction { get; set; }

    /// <summary>Visual variant for the secondary action button. Default Default.</summary>
    [Parameter] public SgButtonVariant SecondaryActionVariant { get; set; } = SgButtonVariant.Default;

    /// <summary>Custom content rendered at the bottom of the result, below actions.</summary>
    [Parameter] public RenderFragment? FooterContent { get; set; }

    /// <summary>Illustration rendered above the icon (e.g. celebration confetti, custom graphic).</summary>
    [Parameter] public RenderFragment? Illustration { get; set; }

    /// <summary>Current step number for multi-step sequences (shown as "Step 2/3").</summary>
    [Parameter] public int? Step { get; set; }

    /// <summary>Total steps when used in a multi-step sequence.</summary>
    [Parameter] public int? TotalSteps { get; set; }

    /// <summary>Text for the auto-redirect abort button. Default "Cancel".</summary>
    [Parameter] public string? AbortRedirectText { get; set; }

    /// <summary>Fired when the user aborts the auto-redirect.</summary>
    [Parameter] public EventCallback OnAbortRedirect { get; set; }

    /// <summary>Semantic heading level for the title (1-6). Default 2 (h2).</summary>
    [Parameter] public int TitleLevel { get; set; } = 2;

    /// <summary>Component size. Default <see cref="SgSize.Md"/>.</summary>
    [Parameter] public SgSize Size { get; set; } = SgSize.Md;

    /// <summary>Reduces padding and icon size.</summary>
    [Parameter] public bool Compact { get; set; }

    /// <summary>Whether to show entrance animations. Default true.</summary>
    [Parameter] public bool Animated { get; set; } = true;

    /// <summary>Whether to show a subtle shadow elevation.</summary>
    [Parameter] public bool Elevated { get; set; }

    /// <summary>Whether to show the left accent border. Default true.</summary>
    [Parameter] public bool ShowBorder { get; set; } = true;

    /// <summary>Whether to stretch to full container width.</summary>
    [Parameter] public bool FullWidth { get; set; }

    /// <summary>Whether to show an intermediate loading state with spinner.</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>Custom text shown in the loading state. Default "Loading...".</summary>
    [Parameter] public string? LoadingText { get; set; }

    /// <summary>Auto-redirect after this many milliseconds (0 = disabled). Shows countdown.</summary>
    [Parameter] public int AutoRedirectMs { get; set; }

    /// <summary>Action invoked when the auto-redirect timer elapses.</summary>
    [Parameter] public EventCallback AutoRedirectAction { get; set; }

    /// <summary>Additional CSS classes.</summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>Inline CSS styles for the root element.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Additional HTML attributes for the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private bool HasShortcutActions =>
        (!string.IsNullOrEmpty(PrimaryActionText) && OnPrimaryAction.HasDelegate) ||
        (!string.IsNullOrEmpty(SecondaryActionText) && OnSecondaryAction.HasDelegate);

    private bool HasCustomIcon => IconContent is not null || !string.IsNullOrWhiteSpace(Icon);

    private string ComputedClass
    {
        get
        {
            var cls = "sgc-result sgc-result-" + StatusClass;

            if (Size != SgSize.Md) cls += Size switch
            {
                SgSize.Sm => " sgc-result-sm",
                SgSize.Lg => " sgc-result-lg",
                SgSize.Xl => " sgc-result-xl",
                _ => ""
            };
            if (Compact) cls += " sgc-result-compact";
            if (Elevated) cls += " sgc-result-elevated";
            if (FullWidth) cls += " sgc-result-fullwidth";
            if (!ShowBorder) cls += " sgc-result-noborder";
            if (!Animated) cls += " sgc-result-noanim";
            if (!string.IsNullOrEmpty(CssClass)) cls += " " + CssClass;

            return cls;
        }
    }

    private string StatusClass => Status switch
    {
        SgResultStatus.Success => "success",
        SgResultStatus.Error => "error",
        SgResultStatus.Info => "info",
        SgResultStatus.Warning => "warning",
        SgResultStatus.Status403 => "403",
        SgResultStatus.Status404 => "404",
        SgResultStatus.Status500 => "500",
        _ => "info"
    };

    private string ResolvedIcon => Icon ?? (Status switch
    {
        SgResultStatus.Success => DefaultIcons.Success,
        SgResultStatus.Error => DefaultIcons.Error,
        SgResultStatus.Warning => DefaultIcons.Warning,
        SgResultStatus.Status403 => "403",
        SgResultStatus.Status404 => "404",
        SgResultStatus.Status500 => "500",
        _ => DefaultIcons.Info
    });

    /// <summary>True when the icon is an SVG string (starts with &lt;svg).</summary>
    private bool IsSvg => ResolvedIcon.StartsWith("<svg", StringComparison.OrdinalIgnoreCase);

    /// <summary>True for HTTP status codes (403/404/500) shown as text.</summary>
    private bool IsStatusCode => ResolvedIcon is "403" or "404" or "500";

    private bool ShowLoading => Loading;

    protected override void OnInitialized()
    {
        StartAutoRedirect();
    }

    protected override void OnParametersSet()
    {
        if (AutoRedirectMs > 0 && _countdownSeconds == 0)
        {
            StartAutoRedirect();
        }
    }

    private void CancelAutoRedirect()
    {
        _autoRedirectAborted = true;
        _autoRedirectCts?.Cancel();
        _autoRedirectCts?.Dispose();
        _autoRedirectCts = null;
        StateHasChanged();
    }

    private async Task HandleAbortRedirect()
    {
        CancelAutoRedirect();
        if (OnAbortRedirect.HasDelegate)
            await OnAbortRedirect.InvokeAsync();
    }

    private void StartAutoRedirect()
    {
        if (AutoRedirectMs <= 0) return;
        _autoRedirectAborted = false;
        _autoRedirectCts?.Cancel();
        _autoRedirectCts = new CancellationTokenSource();
        _countdownSeconds = AutoRedirectMs / 1000;
        var token = _autoRedirectCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                for (var i = _countdownSeconds; i > 0; i--)
                {
                    await Task.Delay(1000, token);
                    if (token.IsCancellationRequested || _autoRedirectAborted) return;
                    _countdownSeconds = i - 1;
                    await InvokeAsync(StateHasChanged);
                }
                if (!token.IsCancellationRequested && !_autoRedirectAborted && AutoRedirectAction.HasDelegate)
                {
                    await InvokeAsync(AutoRedirectAction.InvokeAsync);
                }
            }
            catch (TaskCanceledException) { }
        }, token);
    }

    private async Task HandlePrimaryAsync()
    {
        if (OnPrimaryAction.HasDelegate) await OnPrimaryAction.InvokeAsync();
    }

    private async Task HandleSecondaryAsync()
    {
        if (OnSecondaryAction.HasDelegate) await OnSecondaryAction.InvokeAsync();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _autoRedirectCts?.Cancel();
        _autoRedirectCts?.Dispose();
    }

    /// <summary>Default SVG icon strings for each variant.</summary>
    private static class DefaultIcons
    {
        public const string Success =
            "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">" +
            "<circle cx=\"12\" cy=\"12\" r=\"10\"/><polyline points=\"9 12 11 14 15 10\"/></svg>";

        public const string Error =
            "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">" +
            "<circle cx=\"12\" cy=\"12\" r=\"10\"/><line x1=\"15\" y1=\"9\" x2=\"9\" y2=\"15\"/><line x1=\"9\" y1=\"9\" x2=\"15\" y2=\"15\"/></svg>";

        public const string Warning =
            "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">" +
            "<path d=\"M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z\"/>" +
            "<line x1=\"12\" y1=\"9\" x2=\"12\" y2=\"13\"/><line x1=\"12\" y1=\"17\" x2=\"12.01\" y2=\"17\"/></svg>";

        public const string Info =
            "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">" +
            "<circle cx=\"12\" cy=\"12\" r=\"10\"/><line x1=\"12\" y1=\"8\" x2=\"12\" y2=\"12\"/><line x1=\"12\" y1=\"16\" x2=\"12.01\" y2=\"16\"/></svg>";
    }
}
