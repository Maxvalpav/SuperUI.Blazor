using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Base;
using SuperUI.Base.Optimization;
using SuperUI.Base.Services;
using SuperUI.Base.Utilities;

namespace SuperUI.Components;

public partial class SgMaskedInput : SgFormFieldBase<string>
{
    private bool _focused;
    private bool _touched;
    private string? _lastCompleteRaw;
    private readonly SgRenderBudgetGuard _renderGuard = new(maxPerSecond: 60);

    [Parameter] public SgLabelPosition LabelPosition { get; set; } = SgLabelPosition.Top;
    [Parameter] public SgMaskType MaskType { get; set; } = SgMaskType.Custom;
    [Parameter] public string? Mask { get; set; }
    [Parameter] public char MaskPlaceholder { get; set; } = '_';
    [Parameter] public bool ShowMaskAsPlaceholder { get; set; } = true;
    [Parameter] public bool ShowMaskWhileTyping { get; set; }
    [Parameter] public bool Block { get; set; } = true;
    [Parameter] public string? CssClass { get; set; }
    [Parameter] public bool ShowClearButton { get; set; }
    [Parameter] public int DecimalPlaces { get; set; } = 2;
    [Parameter] public string CurrencySymbol { get; set; } = "RUB";
    [Parameter] public bool UseThousandsSeparator { get; set; } = true;
    [Parameter] public bool AllowNegative { get; set; }
    [Parameter] public decimal? MinValue { get; set; }
    [Parameter] public decimal? MaxValue { get; set; }
    [Parameter] public string? DatePattern { get; set; }
    [Parameter] public EventCallback OnEnterPressed { get; set; }
    [Parameter] public EventCallback<string> OnComplete { get; set; }
    [Parameter] public RenderFragment? PrefixContent { get; set; }
    [Parameter] public RenderFragment? SuffixContent { get; set; }

    [Inject] private ISuperUILocalizer Localizer { get; set; } = default!;

    protected override string ComponentPrefix => "msk";

    public bool IsComplete => CheckComplete(Value);

    private bool IsMaskInvalid =>
        (_touched && Required && string.IsNullOrEmpty(Value))
        || (_touched && !string.IsNullOrEmpty(Value) && !IsComplete && !_isNumeric);

    private string? DisplayedError =>
        !string.IsNullOrEmpty(ErrorText) ? ErrorText
        : ValidationMessages.FirstOrDefault()
            ?? (_touched && Required && string.IsNullOrEmpty(Value) ? Localizer["Required"] : null)
            ?? (_touched && !string.IsNullOrEmpty(Value) && !IsComplete && !_isNumeric ? Localizer["Incomplete"] : null);

    private string? DescribedBy => HasError ? $"{EffectiveId}-err" : (!string.IsNullOrEmpty(Hint) ? $"{EffectiveId}-hint" : null);
    private bool _isNumeric => MaskType is SgMaskType.Currency or SgMaskType.Percent;

    protected override bool ShouldRender()
    {
        if (!_renderGuard.TryRender()) return false;
        return base.ShouldRender();
    }

    private string GetFieldClasses() => Css("sgc-field")
        .AddIf("sgc-block", Block)
        .AddEnum(LabelPosition, "sgc-label-")
        .AddIf("sgc-focused", _focused)
        .Add(CssClass)
        .Add(Class)
        .ToString();

    private string GetInputClasses() => Css("sgc-input")
        .AddIf("sgc-invalid", HasError)
        .AddIf("sgc-text-right", _isNumeric)
        .AddIf("sgc-mask-complete", IsComplete)
        .ToString();

    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        return new AriaBuilder()
            .Label(Label)
            .Required(Required)
            .Invalid(HasError)
            .Disabled(Disabled)
            .ReadOnly(ReadOnly)
            .Build();
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!HasError && IsMaskInvalid)
            AddValidationErrorIfNeeded();
    }

    private void AddValidationErrorIfNeeded()
    {
        ClearValidationErrors();
        if (_touched && Required && string.IsNullOrEmpty(Value))
            AddValidationError(Localizer["Required"]);
        else if (_touched && !string.IsNullOrEmpty(Value) && !IsComplete && !_isNumeric)
            AddValidationError(Localizer["Incomplete"]);
    }

    private string InputMode => MaskType switch
    {
        SgMaskType.Phone or SgMaskType.Inn or SgMaskType.Kpp or SgMaskType.Account or SgMaskType.Bic
            or SgMaskType.Snils or SgMaskType.Ogrn or SgMaskType.CreditCard or SgMaskType.CardExpiry
            or SgMaskType.Cvv or SgMaskType.Postal => "numeric",
        SgMaskType.Currency or SgMaskType.Percent => "decimal",
        SgMaskType.Iban => "text",
        _ => "text"
    };

    private string? EffectivePlaceholder
    {
        get
        {
            if (!string.IsNullOrEmpty(Placeholder)) return Placeholder;
            if (!ShowMaskAsPlaceholder) return null;
            var mask = GetMaskPattern();
            if (string.IsNullOrEmpty(mask)) return null;
            return MaskTemplate(mask, MaskPlaceholder);
        }
    }

    private string DisplayValue => GetDisplayValue();

    private string GetDisplayValue()
    {
        if (string.IsNullOrEmpty(Value))
            return ShowMaskWhileTyping && !_isNumeric ? MaskTemplate(GetMaskPattern(), MaskPlaceholder) : string.Empty;

        return MaskType switch
        {
            SgMaskType.Currency => _focused ? FormatNumericEditing(Value) : FormatCurrency(Value),
            SgMaskType.Percent  => _focused ? FormatNumericEditing(Value) : FormatPercent(Value),
            _                   => ShowMaskWhileTyping
                                    ? ApplyMaskWithPlaceholder(Value, GetMaskPattern(), MaskPlaceholder)
                                    : ApplyMask(Value, GetMaskPattern())
        };
    }

    private string GetMaskPattern() => MaskType switch
    {
        SgMaskType.Phone      => "+7 (999) 999-99-99",
        SgMaskType.Inn        => CountDigits(Value) > 10 ? "999999999999" : "9999999999",
        SgMaskType.Kpp        => "9999nn999",
        SgMaskType.Account    => "9999 9999 9999 9999 9999",
        SgMaskType.Bic        => "999999999",
        SgMaskType.Snils      => "999-999-999 99",
        SgMaskType.Ogrn       => CountDigits(Value) > 13 ? "999999999999999" : "9999999999999",
        SgMaskType.Iban       => "AAnn nnnn nnnn nnnn nnnn nnnn nnnn nn",
        SgMaskType.CreditCard => "9999 9999 9999 9999",
        SgMaskType.CardExpiry => "99/99",
        SgMaskType.Cvv        => "999",
        SgMaskType.Postal     => "999999",
        SgMaskType.Date       => DateMask(),
        SgMaskType.Time       => "99:99",
        _                     => Mask ?? ""
    };

    private string DateMask()
    {
        if (!string.IsNullOrEmpty(DatePattern)) return DigitMaskFromDatePattern(DatePattern);
        var c = Culture ?? CultureInfo.CurrentCulture;
        return DigitMaskFromDatePattern(c.DateTimeFormat.ShortDatePattern);
    }

    private static string DigitMaskFromDatePattern(string p)
    {
        var sb = new StringBuilder(p.Length);
        foreach (var c in p)
            sb.Append(char.IsLetter(c) ? '9' : c);
        return sb.ToString();
    }

    private int? MaxDisplayLength => MaskType switch
    {
        SgMaskType.Currency or SgMaskType.Percent => null,
        _ => GetMaskPattern().Length > 0 ? GetMaskPattern().Length : null
    };

    private async Task OnInputAsync(ChangeEventArgs e)
    {
        await ProcessInputAsync(e.Value?.ToString() ?? "");
    }

    private Task OnChangeAsync(ChangeEventArgs e) => Task.CompletedTask;

    private async Task HandleMaskedFocusAsync(FocusEventArgs e)
    {
        _focused = true;
        await HandleFocusAsync(e);
    }

    private async Task HandleMaskedBlurAsync(FocusEventArgs e)
    {
        _focused = false;
        _touched = true;
        AddValidationErrorIfNeeded();
        if (_isNumeric && !string.IsNullOrEmpty(Value))
            await ApplyNumericConstraintsAsync();
        await HandleBlurAsync(e);
    }

    private async Task ProcessInputAsync(string rawInput)
    {
        try
        {
            string nextRaw = MaskType switch
            {
                SgMaskType.Currency => ParseNumericInput(rawInput, true),
                SgMaskType.Percent  => ParseNumericInput(rawInput, false),
                _                   => StripToSignificantChars(rawInput, GetMaskPattern())
            };

            if (!_isNumeric)
            {
                int maxLen = CountSignificantChars(GetMaskPattern());
                if (maxLen > 0 && nextRaw.Length > maxLen)
                    nextRaw = nextRaw[..maxLen];
            }

            await SetValueAsync(string.IsNullOrEmpty(nextRaw) ? null : nextRaw);
            
            if (CheckComplete(Value) && _lastCompleteRaw != Value)
            {
                _lastCompleteRaw = Value;
                if (OnComplete.HasDelegate)
                    await OnComplete.InvokeAsync(Value!);
            }
            else if (!CheckComplete(Value))
            {
                _lastCompleteRaw = null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing masked input");
        }
    }

    private async Task ApplyNumericConstraintsAsync()
    {
        var culture = Culture ?? CultureInfo.CurrentCulture;
        if (!decimal.TryParse(Value, NumberStyles.Any, culture, out var d)) return;
        var clamped = d;
        if (MinValue.HasValue && clamped < MinValue.Value) clamped = MinValue.Value;
        if (MaxValue.HasValue && clamped > MaxValue.Value) clamped = MaxValue.Value;
        if (!AllowNegative && clamped < 0) clamped = 0;
        if (clamped != d)
            await SetValueAsync(clamped.ToString(culture));
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && OnEnterPressed.HasDelegate)
            await OnEnterPressed.InvokeAsync();
    }

    private async Task ClearAsync()
    {
        await SetValueAsync(null);
        if (_touched)
            AddValidationErrorIfNeeded();
    }

    public string? GetFormattedValue() => string.IsNullOrEmpty(Value) ? null : GetDisplayValue();

    // ---------- Mask engine ----------

    private static bool TokenMatches(char token, char c) => token switch
    {
        '9' => char.IsDigit(c),
        'a' => char.IsLetter(c),
        'A' => char.IsLetter(c),
        'n' => char.IsLetterOrDigit(c),
        '*' => true,
        _   => false
    };

    private static bool IsToken(char c) => c is '9' or 'a' or 'A' or 'n' or '*';

    private static char TransformChar(char token, char c) => token switch
    {
        'A' => char.ToUpperInvariant(c),
        _   => c
    };

    private static string ApplyMask(string raw, string mask)
    {
        if (string.IsNullOrEmpty(mask)) return raw;
        var sb = new StringBuilder();
        int ri = 0;
        foreach (char m in mask)
        {
            if (ri >= raw.Length) break;
            if (IsToken(m))
            {
                if (TokenMatches(m, raw[ri])) { sb.Append(TransformChar(m, raw[ri])); ri++; }
                else { ri++; } 
            }
            else
            {
                sb.Append(m);
            }
        }
        return sb.ToString();
    }

    private static string ApplyMaskWithPlaceholder(string raw, string mask, char ph)
    {
        if (string.IsNullOrEmpty(mask)) return raw;
        var sb = new StringBuilder();
        int ri = 0;
        foreach (char m in mask)
        {
            if (IsToken(m))
            {
                if (ri < raw.Length) { sb.Append(TransformChar(m, raw[ri])); ri++; }
                else sb.Append(ph);
            }
            else
            {
                sb.Append(m);
            }
        }
        return sb.ToString();
    }

    private static string MaskTemplate(string mask, char ph)
    {
        if (string.IsNullOrEmpty(mask)) return string.Empty;
        var sb = new StringBuilder(mask.Length);
        foreach (var m in mask)
            sb.Append(IsToken(m) ? ph : m);
        return sb.ToString();
    }

    private string StripToSignificantChars(string input, string mask)
    {
        if (string.IsNullOrEmpty(mask))
            return new string(input.Where(char.IsLetterOrDigit).ToArray());

        var sb = new StringBuilder();
        int mi = 0;
        foreach (char c in input)
        {
            if (c == MaskPlaceholder) continue;
            if (mi >= mask.Length) break;

            char m = mask[mi];
            if (IsToken(m))
            {
                if (TokenMatches(m, c))
                {
                    sb.Append(TransformChar(m, c));
                    mi++;
                }
            }
            else if (c == m)
            {
                mi++;
            }
            else if (char.IsLetterOrDigit(c))
            {
                while (mi < mask.Length && !IsToken(mask[mi])) mi++;
                if (mi < mask.Length && TokenMatches(mask[mi], c))
                {
                    sb.Append(TransformChar(mask[mi], c));
                    mi++;
                }
            }
        }
        return sb.ToString();
    }

    private static int CountSignificantChars(string mask)
    {
        int n = 0;
        foreach (char c in mask)
            if (IsToken(c)) n++;
        return n;
    }

    private static int CountDigits(string? s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        int n = 0;
        foreach (var c in s) if (char.IsDigit(c)) n++;
        return n;
    }

    private bool CheckComplete(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return false;
        if (_isNumeric)
        {
            var culture = Culture ?? CultureInfo.CurrentCulture;
            if (!decimal.TryParse(raw, NumberStyles.Any, culture, out var d)) return false;
            if (MinValue.HasValue && d < MinValue.Value) return false;
            if (MaxValue.HasValue && d > MaxValue.Value) return false;
            return true;
        }
        var mask = GetMaskPattern();
        if (string.IsNullOrEmpty(mask)) return raw.Length > 0;
        return raw.Length >= CountSignificantChars(mask);
    }

    // ---------- Numeric formatting ----------

    private string ParseNumericInput(string input, bool isCurrency)
    {
        var culture = Culture ?? CultureInfo.CurrentCulture;
        char decSep = culture.NumberFormat.NumberDecimalSeparator[0];
        var sb = new StringBuilder();
        bool hasDecimal = false;
        foreach (char c in input)
        {
            if (char.IsDigit(c))
            {
                sb.Append(c);
            }
            else if ((c == '.' || c == ',') && !hasDecimal)
            {
                sb.Append(decSep);
                hasDecimal = true;
            }
            else if (c == '-' && sb.Length == 0 && AllowNegative)
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private string FormatNumericEditing(string raw)
    {
        return raw;
    }

    private string FormatCurrency(string raw)
    {
        if (!decimal.TryParse(raw, out var d)) return raw;
        var culture = (CultureInfo)EffectiveCulture.Clone();
        culture.NumberFormat.CurrencySymbol = CurrencySymbol;
        return d.ToString("C", culture);
    }

    private string FormatPercent(string raw)
    {
        if (!decimal.TryParse(raw, out var d)) return raw;
        return (d / 100).ToString("P", EffectiveCulture);
    }
}
