using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>Represents a versatile button component with support for loading, progress, icons, and danger confirmation.</summary>
public partial class SgButton
{
    private RenderFragment RenderContent => __builder =>
    {
        if (HasProgress)
        {
            if (ProgressType == SgButtonProgressType.Ring)
            {
                var circ = 2 * Math.PI * 14;
                var dashOff = circ * (1 - Progress / 100.0);
                var ringLabel = Loading && Progress < 100 ? $"{Progress}%"
                    : Progress >= 100 ? "✓"
                    : $"{Progress}%";
                __builder.OpenElement(0, "span");
                __builder.AddAttribute(1, "class", "sgc-btn-progress-text");
                __builder.AddAttribute(2, "style", "gap:8px;");
                __builder.OpenElement(3, "svg");
                __builder.AddAttribute(4, "width", "20");
                __builder.AddAttribute(5, "height", "20");
                __builder.AddAttribute(6, "viewBox", "0 0 32 32");
                __builder.AddAttribute(7, "style", "flex-shrink:0;");
                __builder.OpenElement(8, "circle");
                __builder.AddAttribute(9, "cx", "16");
                __builder.AddAttribute(10, "cy", "16");
                __builder.AddAttribute(11, "r", "14");
                __builder.AddAttribute(12, "fill", "none");
                __builder.AddAttribute(13, "stroke", "var(--sg-border, #e5e7eb)");
                __builder.AddAttribute(14, "stroke-width", "3");
                __builder.CloseElement();
                __builder.OpenElement(15, "circle");
                __builder.AddAttribute(16, "cx", "16");
                __builder.AddAttribute(17, "cy", "16");
                __builder.AddAttribute(18, "r", "14");
                __builder.AddAttribute(19, "fill", "none");
                __builder.AddAttribute(20, "stroke", "currentColor");
                __builder.AddAttribute(21, "stroke-width", "3");
                __builder.AddAttribute(22, "stroke-linecap", "round");
                __builder.AddAttribute(23, "stroke-dasharray", $"{circ:F1} {circ:F1}");
                __builder.AddAttribute(24, "style", $"stroke-dashoffset:{dashOff:F1};transform:rotate(-90deg);transform-origin:50% 50%;transition:stroke-dashoffset 0.3s ease;animation:sgc-spin 1s linear infinite;");
                __builder.CloseElement();
                __builder.OpenElement(25, "text");
                __builder.AddAttribute(26, "x", "16");
                __builder.AddAttribute(27, "y", "16");
                __builder.AddAttribute(28, "text-anchor", "middle");
                __builder.AddAttribute(29, "dominant-baseline", "central");
                __builder.AddAttribute(30, "fill", "currentColor");
                __builder.AddAttribute(31, "font-size", "9");
                __builder.AddAttribute(32, "font-weight", "700");
                __builder.AddAttribute(33, "style", "pointer-events:none;");
                __builder.AddContent(34, ringLabel);
                __builder.CloseElement();
                __builder.CloseElement();
                // keep button text visible so size doesn't change
                RenderButtonText(__builder, 35);
                __builder.CloseElement();
            }
            else
            {
                __builder.OpenElement(0, "span");
                __builder.AddAttribute(1, "class", "sgc-btn-progress-text");
                __builder.AddContent(2, RenderProgressContentBuffer);
                __builder.CloseElement();
            }
            return;
        }

        if (ShowSpinnerCenter)
        {
            __builder.OpenElement(0, "span");
            __builder.AddAttribute(1, "class", "sgc-spinner");
            __builder.AddAttribute(2, "style", "width:12px;height:12px;border-width:2px;");
            __builder.CloseElement();
            return;
        }

        if (ShowSpinnerLeft)
        {
            __builder.OpenElement(0, "span");
            __builder.AddAttribute(1, "class", "sgc-spinner");
            __builder.AddAttribute(2, "style", "width:12px;height:12px;border-width:2px;");
            __builder.CloseElement();
        }
        else if (!ShowSpinnerRight && Icon is not null)
        {
            __builder.OpenElement(0, "span");
            __builder.AddAttribute(1, "class", "sgc-btn-icon");
            __builder.AddContent(2, Icon);
            __builder.CloseElement();
        }

        if (_dangerConfirmActive)
        {
            __builder.AddContent(0, ConfirmText);
        }
        else if (_countdownActive)
        {
            __builder.AddContent(0, _countdownValue.ToString());
        }
        else if (ChildContent is not null)
        {
            __builder.AddContent(0, ChildContent);
        }
        else if (!string.IsNullOrEmpty(Text))
        {
            __builder.OpenElement(0, "span");
            __builder.AddContent(1, Text);
            __builder.CloseElement();
        }

        if (ShowSpinnerRight)
        {
            __builder.OpenElement(0, "span");
            __builder.AddAttribute(1, "class", "sgc-spinner");
            __builder.AddAttribute(2, "style", "width:12px;height:12px;border-width:2px;");
            __builder.CloseElement();
        }
        else if (IconRight is not null && !_dangerConfirmActive && !_countdownActive)
        {
            __builder.OpenElement(0, "span");
            __builder.AddAttribute(1, "class", "sgc-btn-icon-right");
            __builder.AddContent(2, IconRight);
            __builder.CloseElement();
        }
    };

    private RenderFragment RenderProgressContentBuffer => __builder =>
    {
        if (Loading && Progress < 100)
        {
            RenderProgressSpinnerInner(__builder);
            __builder.AddContent(0, $"{Progress}%");
        }
        else if (Progress >= 100)
        {
            __builder.AddContent(0, string.IsNullOrEmpty(Text) ? "Complete" : Text);
        }
        else
        {
            __builder.AddContent(0, $"{Progress}%");
        }
    };

    private void RenderButtonText(RenderTreeBuilder __builder, int seq)
    {
        if (_dangerConfirmActive)
        {
            __builder.AddContent(seq, ConfirmText);
        }
        else if (_countdownActive)
        {
            __builder.AddContent(seq, _countdownValue.ToString());
        }
        else if (ChildContent is not null)
        {
            __builder.AddContent(seq, ChildContent);
        }
        else if (!string.IsNullOrEmpty(Text))
        {
            __builder.OpenElement(seq, "span");
            __builder.AddContent(seq + 1, Text);
            __builder.CloseElement();
        }
    }

    private void RenderProgressSpinnerInner(RenderTreeBuilder __builder)
    {
        var type = ProgressSpinnerType ?? SgSpinnerType.Border;
        switch (type)
        {
            case SgSpinnerType.Pulse:
                __builder.OpenElement(0, "div");
                __builder.AddAttribute(1, "class", "sgc-spinner-pulse-circle");
                __builder.AddAttribute(2, "style", "width:10px;height:10px;flex-shrink:0;");
                __builder.CloseElement();
                break;
            case SgSpinnerType.Dots:
                __builder.OpenElement(0, "span");
                __builder.AddAttribute(1, "style", "display:inline-flex;gap:3px;align-items:center;flex-shrink:0;");
                for (int i = 0; i < 3; i++)
                {
                    __builder.OpenElement(2 + i * 2, "span");
                    __builder.AddAttribute(3 + i * 2, "class", "sgc-spinner-dot");
                    __builder.AddAttribute(4 + i * 2, "style", "width:5px;height:5px;");
                    __builder.CloseElement();
                }
                __builder.CloseElement();
                break;
            case SgSpinnerType.Bars:
                __builder.OpenElement(0, "span");
                __builder.AddAttribute(1, "style", "display:inline-flex;gap:2px;align-items:center;flex-shrink:0;height:14px;");
                var barHeights = new[] { "40%", "70%", "100%", "70%", "40%" };
                for (int i = 0; i < 5; i++)
                {
                    __builder.OpenElement(2 + i * 2, "span");
                    __builder.AddAttribute(3 + i * 2, "class", "sgc-spinner-bar");
                    __builder.AddAttribute(4 + i * 2, "style", $"width:2px;height:{barHeights[i]};");
                    __builder.CloseElement();
                }
                __builder.CloseElement();
                break;
            default:
                __builder.OpenElement(0, "span");
                __builder.AddAttribute(1, "class", "sgc-spinner");
                __builder.AddAttribute(2, "style", "width:12px;height:12px;border-width:2px;flex-shrink:0;");
                __builder.CloseElement();
                break;
        }
    }
}
