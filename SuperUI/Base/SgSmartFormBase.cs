// SuperUI/Base/SgSmartFormBase.cs
// НОВЫЙ: Умная форма с авто-сохранением, детекцией грязных полей и конфликтами

using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SuperUI.Base.Services;

namespace SuperUI.Base;

/// <summary>
/// Умная форма с авто-сохранением черновика в sessionStorage,
/// детекцией несохранённых изменений и предупреждением при уходе.
/// </summary>
/// <typeparam name="TModel">Тип модели формы.</typeparam>
public abstract class SgSmartFormBase<TModel> : SgFormBase<TModel>
    where TModel : class, new()
{
    [Inject] protected ISessionStorage SessionStorage { get; set; } = null!;
    [Inject] protected IEnhancedNavigationService? EnhancedNav { get; set; }

    [Parameter] public int AutoSaveIntervalSec { get; set; } = 30;
    [Parameter] public string? DraftKey { get; set; }
    [Parameter] public bool WarnOnUnsavedChanges { get; set; } = true;

    protected bool IsDirty { get; private set; }
    protected bool AutoSaveEnabled => AutoSaveIntervalSec > 0;

    private Timer? _autoSaveTimer;
    private TModel? _originalModel;
    private IDisposable? _navigationSubscription;
    private string EffectiveDraftKey => DraftKey ?? $"sg-draft-{typeof(TModel).Name}-{ComponentId}";

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _originalModel = Model is not null ? CloneModel(Model) : null;

        if (AutoSaveEnabled)
        {
            _autoSaveTimer = new Timer(async _ =>
            {
                if (!IsDisposed && IsDirty)
                    await SaveDraftAsync();
            }, null, TimeSpan.FromSeconds(AutoSaveIntervalSec),
               TimeSpan.FromSeconds(AutoSaveIntervalSec));
        }

        if (WarnOnUnsavedChanges && EnhancedNav is not null)
        {
            _navigationSubscription = EnhancedNav.OnBeforeUnload(() =>
                IsDirty ? "У вас есть несохранённые изменения. Хотите уйти?" : null);
        }

        _ = RestoreDraftAsync();
    }

    protected void MarkDirty()
    {
        if (!IsDirty)
        {
            IsDirty = true;
            _ = InvokeAsync(StateHasChanged);
        }
    }

    protected async Task SaveDraftAsync()
    {
        if (Model is null || IsDisposed) return;
        try
        {
            await SessionStorage.SetItemAsync(EffectiveDraftKey, Model);
            Logger.LogDebug("[{Id}] Draft saved: {Key}", ComponentId, EffectiveDraftKey);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[{Id}] Failed to save draft", ComponentId);
        }
    }

    private async Task RestoreDraftAsync()
    {
        try
        {
            var draft = await SessionStorage.GetItemAsync<TModel>(EffectiveDraftKey);
            if (draft is not null)
            {
                Model = draft;
                IsDirty = true;
                _editContext = new EditContext(Model);
                _editContext.OnValidationStateChanged += (s, e) =>
                {
                    _isValid = !_editContext.GetValidationMessages().Any();
                    _ = InvokeAsync(StateHasChanged);
                };
                _isValid = false;
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "[{Id}] No draft to restore: {Key}", ComponentId, EffectiveDraftKey);
        }
    }

    protected async Task ClearDraftAsync()
    {
        try
        {
            await SessionStorage.RemoveItemAsync(EffectiveDraftKey);
            IsDirty = false;
            _originalModel = Model is not null ? CloneModel(Model) : null;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[{Id}] Failed to clear draft", ComponentId);
        }
    }

    protected override async Task OnFormValidSubmitAsync()
    {
        await ClearDraftAsync();
        await base.OnFormValidSubmitAsync();
    }

    public override async Task ResetAsync()
    {
        await base.ResetAsync();
        await ClearDraftAsync();
    }

    protected virtual TModel CloneModel(TModel source)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(source);
        return System.Text.Json.JsonSerializer.Deserialize<TModel>(json)!;
    }

    protected override async ValueTask DisposeComponentAsync()
    {
        _autoSaveTimer?.Dispose();
        _navigationSubscription?.Dispose();
        await base.DisposeComponentAsync();
    }
}
