using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Forms;
using SuperUI.Base;

namespace SuperUI.Components;

public partial class SgMultiSelect<TItem, TKey> : SgInteractiveBase, IDisposable
{
    private bool _open;
    private string _search = string.Empty;
    private CancellationTokenSource? _blurCts;
    private List<SgEnumItem>? _enumItems;
    private Type? _lastEnumType;
    private EditContext? _editContext;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasInitializedParameters;

    [Inject] protected ISuperUILocalizer Localizer { get; set; } = default!;
    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    [Parameter] public IEnumerable<TKey>? Values { get; set; }
    [Parameter] public EventCallback<IEnumerable<TKey>> ValuesChanged { get; set; }
    [Parameter] public Expression<Func<IEnumerable<TKey>?>>? ValueExpression { get; set; }
    
    [Parameter] public IEnumerable<TItem> Items { get; set; } = Array.Empty<TItem>();
    [Parameter] public Type? EnumType { get; set; }
    [Parameter] public SgEnumKeyMode EnumKeyMode { get; set; } = SgEnumKeyMode.Name;
    [Parameter] public Func<TItem, TKey> KeySelector { get; set; } = default!;
    [Parameter] public Func<TItem, string>? LabelSelector { get; set; }
    
    [Parameter] public string? Label { get; set; }
    [Parameter] public SgLabelPosition LabelPosition { get; set; } = SgLabelPosition.Top;
    [Parameter] public string? Hint { get; set; }
    [Parameter] public string? ErrorText { get; set; }
    [Parameter] public string? Placeholder { get; set; }
    [Parameter] public string? EmptyText { get; set; }
    [Parameter] public bool Required { get; set; }
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Block { get; set; } = true;

    protected override string ComponentPrefix => "multi";

    private bool IsEnumMode => _enumItems is not null;
    private string EffectivePlaceholder => Placeholder ?? Localizer["Select"];
    private string EffectiveEmptyText => string.IsNullOrEmpty(EmptyText) ? Localizer["NoMatches"] : EmptyText;

    private bool HasError => !string.IsNullOrEmpty(ErrorText) || (_editContext?.GetValidationMessages(_fieldIdentifier).Any() ?? false);
    private string? DisplayedError => !string.IsNullOrEmpty(ErrorText) 
        ? ErrorText 
        : _editContext?.GetValidationMessages(_fieldIdentifier).FirstOrDefault();

    private string GetFieldClasses() => Css("sgc-field")
        .AddIf("sgc-block", Block)
        .AddEnum(LabelPosition, "sgc-label-")
        .AddIf("sgc-invalid", HasError)
        .Add(Class)
        .ToString();

    private string GetControlClasses() => Css("sgc-combo")
        .AddIf("sgc-block", Block)
        .AddIf("sgc-open", _open)
        .AddIf("sgc-disabled", Disabled)
        .AddIf("sgc-invalid", HasError)
        .ToString();

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        if (!_hasInitializedParameters)
        {
            if (ValueExpression is not null) _fieldIdentifier = FieldIdentifier.Create(ValueExpression);
            _editContext = CascadedEditContext;
            _hasInitializedParameters = true;
        }
        else if (CascadedEditContext != _editContext)
        {
            _editContext = CascadedEditContext;
        }

        if (EnumType is not null && EnumType != _lastEnumType)
        {
            _lastEnumType = EnumType;
            _enumItems = SgEnumHelper.GetItems(EnumType);
        }
        else if (EnumType is null)
        {
            _enumItems = null;
            _lastEnumType = null;
        }
    }

    private string LabelFrom(TItem v) => LabelSelector is not null ? LabelSelector(v) : v?.ToString() ?? "";

    private TKey EnumItemToKey(SgEnumItem ei)
    {
        var targetType = typeof(TKey);
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        try
        {
            if (underlying == typeof(string))
                return (TKey)(object)(EnumKeyMode == SgEnumKeyMode.IntValue ? ei.IntValue.ToString() : ei.Name);
            if (underlying == typeof(int))
                return (TKey)(object)ei.IntValue;
            if (underlying.IsEnum)
                return (TKey)Enum.Parse(underlying, ei.Name);
        }
        catch { }
        return default!;
    }

    private bool IsEnumItemSelected(SgEnumItem ei)
    {
        if (Values is null) return false;
        var eiKey = EnumKeyMode == SgEnumKeyMode.IntValue ? ei.IntValue.ToString() : ei.Name;
        foreach (var k in Values)
        {
            var kStr = k?.ToString() ?? "";
            if (kStr == eiKey) return true;
            if (EnumKeyMode == SgEnumKeyMode.Name && int.TryParse(kStr, out var kInt) && kInt == ei.IntValue) return true;
        }
        return false;
    }

    private IEnumerable<TItem> GetSelectedItems()
    {
        if (Values is null) yield break;
        var keys = Values.ToHashSet();
        foreach (var it in Items)
            if (keys.Contains(KeySelector(it))) yield return it;
    }

    private IEnumerable<SgEnumItem> GetSelectedEnumItems()
    {
        if (_enumItems is null || Values is null) yield break;
        foreach (var ei in _enumItems)
            if (IsEnumItemSelected(ei)) yield return ei;
    }

    private bool IsSelected(TKey key)
    {
        if (Values is null) return false;
        foreach (var k in Values)
            if (EqualityComparer<TKey>.Default.Equals(k, key)) return true;
        return false;
    }

    private IEnumerable<TItem> GetFilteredItems()
    {
        if (string.IsNullOrEmpty(_search)) return Items;
        return Items.Where(i => LabelFrom(i).Contains(_search, StringComparison.CurrentCultureIgnoreCase));
    }

    private IEnumerable<SgEnumItem> GetFilteredEnumItems()
    {
        if (_enumItems is null) return Array.Empty<SgEnumItem>();
        if (string.IsNullOrEmpty(_search)) return _enumItems;
        return _enumItems.Where(ei =>
            ei.Label.Contains(_search, StringComparison.CurrentCultureIgnoreCase) ||
            ei.Name.Contains(_search, StringComparison.CurrentCultureIgnoreCase));
    }

    private async Task ToggleAsync()
    {
        if (Disabled) return;
        _blurCts?.Cancel();
        _open = !_open;
        if (_open) _search = string.Empty;
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleFocusOutAsync(FocusEventArgs e)
    {
        _blurCts?.Cancel();
        _blurCts = new CancellationTokenSource();
        var token = _blurCts.Token;
        try
        {
            await Task.Delay(200, token);
            if (_open) { _open = false; await InvokeAsync(StateHasChanged); }
        }
        catch (TaskCanceledException) { }
    }

    private Task OnSearchInput(ChangeEventArgs e)
    {
        _search = e.Value?.ToString() ?? string.Empty;
        return InvokeAsync(StateHasChanged);
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Escape") { _open = false; await InvokeAsync(StateHasChanged); }
    }

    private async Task ToggleItemAsync(TKey key)
    {
        var list = Values?.ToList() ?? new List<TKey>();
        var idx = list.FindIndex(k => EqualityComparer<TKey>.Default.Equals(k, key));
        if (idx >= 0) list.RemoveAt(idx); else list.Add(key);
        await SetValuesAsync(list);
    }

    private async Task ToggleEnumItemAsync(SgEnumItem ei)
    {
        var key = EnumItemToKey(ei);
        await ToggleItemAsync(key);
    }

    private async Task RemoveAsync(TKey key)
    {
        var list = Values?.ToList() ?? new List<TKey>();
        list.RemoveAll(k => EqualityComparer<TKey>.Default.Equals(k, key));
        await SetValuesAsync(list);
    }

    private async Task RemoveEnumItemAsync(SgEnumItem ei)
    {
        var key = EnumItemToKey(ei);
        await RemoveAsync(key);
    }

    private async Task SetValuesAsync(IEnumerable<TKey> next)
    {
        Values = next;
        if (ValuesChanged.HasDelegate) await ValuesChanged.InvokeAsync(next);
        if (_editContext is not null) _editContext.NotifyFieldChanged(_fieldIdentifier);
        await InvokeAsync(StateHasChanged);
    }

    public virtual void Dispose()
    {
        _blurCts?.Cancel();
        _blurCts?.Dispose();
    }
}
