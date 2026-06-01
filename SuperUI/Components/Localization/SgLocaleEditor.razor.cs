using System.Text.Json;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Base.ComponentBases;
using SuperUI.Localization;

namespace SuperUI.Components;

public partial class SgLocaleEditor : SgJsComponentBase
{
    protected override string ModulePath => "./_content/SuperUI/Components/Localization/SgLocaleEditor.razor.js";
    protected override string IdPrefix => "sg-locale";

    private LocalizationService? _svc;
    private Dictionary<string, LocaleEntry> _catalog = new();
    private Dictionary<string, string> _editedValues = new();
    private Dictionary<string, string> _originalOverrides = new();
    private HashSet<string> _expandedDomains = new();
    private string _search = "";
    private bool _hasChanges;
    private bool _savedIndicator;
    private string? _errorMessage;

    private List<string> _domains = new();

    private List<string> _filteredDomains =>
        _domains.Where(d => _filteredEntries.ContainsKey(d) && _filteredEntries[d].Count > 0).ToList();

    private Dictionary<string, List<LocaleEntry>> _filteredEntries
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_search))
                return _catalog.Values.GroupBy(e => e.Domain).ToDictionary(g => g.Key, g => g.ToList());

            var q = _search.Trim().ToLowerInvariant();
            return _catalog.Values
                .Where(e => e.Key.Contains(q, StringComparison.OrdinalIgnoreCase) || e.OriginalValue.Contains(q, StringComparison.OrdinalIgnoreCase))
                .GroupBy(e => e.Domain)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _svc = Localizer as LocalizationService;
        if (_svc is null) return;

        _catalog = _svc.GetCatalog().ToDictionary(kv => kv.Key, kv => kv.Value);
        _domains = _catalog.Values.Select(e => e.Domain).Distinct().OrderBy(d => d).ToList();

        if (_domains.Count > 0)
            _expandedDomains.Add(_domains[0]);
    }

    protected override async ValueTask OnInteractiveAsync()
    {
        var json = await SafeInvokeAsyncGlobal<string>("localStorage.getItem", "sui-locale-overrides");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var overrides = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                _svc?.LoadOverrides(overrides);
                _originalOverrides = new Dictionary<string, string>(overrides);
                _editedValues = new Dictionary<string, string>(overrides);
            }
            catch { }
        }
        StateHasChanged();
    }

    private void ToggleDomain(string domain)
    {
        if (!_expandedDomains.Remove(domain))
            _expandedDomains.Add(domain);
    }

    private void ToggleDomainOnKey(string domain, KeyboardEventArgs e)
    {
        if (e.Key is "Enter" or " ")
            ToggleDomain(domain);
    }

    private void OnEditChanged(string key, object? value)
    {
        _editedValues[key] = value?.ToString() ?? "";
        UpdateHasChanges();
    }

    private void RevertKey(string key)
    {
        _editedValues.Remove(key);
        _svc?.RemoveOverride(key);
        if (_originalOverrides.TryGetValue(key, out var orig))
            _svc?.SetOverride(key, orig);
        UpdateHasChanges();
    }

    private void UpdateHasChanges()
    {
        if (_svc is null) { _hasChanges = false; return; }
        var currentOverrides = _svc.GetOverrides();
        _hasChanges = !AreDictionariesEqual(currentOverrides, _originalOverrides);
    }

    private async Task SaveAllAsync()
    {
        if (_svc is null) return;

        _svc.ClearOverrides();
        foreach (var kv in _editedValues)
        {
            var original = _svc.GetOriginalValue(kv.Key);
            if (kv.Value != original)
                _svc.SetOverride(kv.Key, kv.Value);
        }

        var overrides = _svc.GetOverrides();
        var json = JsonSerializer.Serialize(overrides);
        await SafeInvokeVoidAsyncGlobal("localStorage.setItem", "sui-locale-overrides", json);

        _originalOverrides = new Dictionary<string, string>(overrides);
        _hasChanges = false;

        _savedIndicator = true;
        StateHasChanged();
        await Task.Delay(2000);
        _savedIndicator = false;
        StateHasChanged();
    }

    private async Task DiscardAllAsync()
    {
        if (_svc is null) return;

        _svc.ClearOverrides();
        _editedValues.Clear();
        await SafeInvokeVoidAsyncGlobal("localStorage.removeItem", "sui-locale-overrides");
        _originalOverrides.Clear();
        _hasChanges = false;
    }

    private async Task DownloadOverridesAsync()
    {
        if (_svc is null) return;
        var overrides = _svc.GetOverrides();
        var json = JsonSerializer.Serialize(overrides, new JsonSerializerOptions { WriteIndented = true });
        await SafeInvokeVoidAsync("downloadJson", json, "locale-overrides.json");
    }

    private async Task UploadOverridesAsync()
    {
        _errorMessage = null;
        try
        {
            var json = await SafeInvokeAsync<string>("uploadJson");
            if (string.IsNullOrEmpty(json)) return;

            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (parsed is null) { _errorMessage = "Invalid JSON format"; StateHasChanged(); return; }

            if (_svc is null) return;
            _svc.LoadOverrides(parsed);
            _editedValues = new Dictionary<string, string>(parsed);
            _originalOverrides = new Dictionary<string, string>(parsed);
            _hasChanges = false;

            await SafeInvokeVoidAsyncGlobal("localStorage.setItem", "sui-locale-overrides", json);

            _savedIndicator = true;
            StateHasChanged();
            await Task.Delay(2000);
            _savedIndicator = false;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Upload failed: {ex.Message}";
            StateHasChanged();
        }
    }

    private static bool AreDictionariesEqual(Dictionary<string, string> a, Dictionary<string, string> b)
    {
        if (a.Count != b.Count) return false;
        foreach (var kv in a)
        {
            if (!b.TryGetValue(kv.Key, out var val) || val != kv.Value)
                return false;
        }
        return true;
    }
}
