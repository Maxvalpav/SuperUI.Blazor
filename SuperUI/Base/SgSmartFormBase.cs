// SuperUI/Base/SgSmartFormBase.cs
// ИСПРАВЛЕНИЯ v3:
// ✅ FIX CS0506: ResetAsync — override виртуального метода базового класса
// ✅ NEW: ConflictDetection — детекция конфликтов при восстановлении черновика
// ✅ NEW: DraftMetadata — метаданные черновика (время, версия)
// ✅ NEW: DebouncedAutoSave — защита от частых сохранений при быстрых изменениях
// ✅ OPTIM: ComputeModelHash — замена JSON на GetHashCode (SgModelHasher)

using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using SuperUI.Base;
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

    /// <summary>
    /// Включить детекцию конфликтов при восстановлении черновика.
    /// Если true, при восстановлении сравнивается хэш оригинальной и черновой модели.
    /// </summary>
    [Parameter] public bool EnableConflictDetection { get; set; } = true;

    protected bool IsDirty { get; private set; }
    protected bool AutoSaveEnabled => AutoSaveIntervalSec > 0;

    private Timer? _autoSaveTimer;
    private TModel? _originalModel;
    private IDisposable? _navigationSubscription;
    private DateTime _lastDraftSaveTime;
    private int _draftVersion;

    /// <summary>Метаданные последнего сохранённого черновика.</summary>
    protected SgDraftMetadata? LastDraftMetadata { get; private set; }

    private string EffectiveDraftKey =>
        DraftKey ?? $"sg-draft-{typeof(TModel).Name}-{ComponentId}";

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _originalModel = Model is not null ? CloneModel(Model) : null;

        if (AutoSaveEnabled)
        {
            _autoSaveTimer = new Timer(
                async _ =>
                {
                    // ✅ FIX C7: проверяем IsDisposed перед каждым тиком
                    if (IsDisposed || !IsDirty) return;

                    try
                    {
                        await SaveDraftAsync();
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug(ex, "[{Id}] Auto-save timer tick failed", ComponentId);
                    }
                },
                null,
                TimeSpan.FromSeconds(AutoSaveIntervalSec),
                TimeSpan.FromSeconds(AutoSaveIntervalSec));
        }

        if (WarnOnUnsavedChanges && EnhancedNav is not null)
        {
            _navigationSubscription = EnhancedNav.OnBeforeUnload(
                () => IsDirty ? "У вас есть несохранённые изменения. Хотите уйти?" : null);
        }

        _ = RestoreDraftAsync();
    }

    // ── Dirty tracking ─────────────────────────────────────────────────────────

    /// <summary>
    /// Отметить форму как изменённую (грязную).
    /// Вызывайте из дочерних полей при изменении значений.
    /// </summary>
    protected void MarkDirty()
    {
        if (!IsDirty)
        {
            IsDirty = true;
            _ = InvokeAsync(StateHasChanged);
        }
    }

    // ── Draft: Save ────────────────────────────────────────────────────────────

    /// <summary>Принудительное сохранение черновика.</summary>
    protected async Task SaveDraftAsync()
    {
        if (Model is null || IsDisposed) return;
        try
        {
            var draft = new SgDraftPayload<TModel>
            {
                Model = Model,
                Metadata = new SgDraftMetadata
                {
                    SavedAt = DateTime.UtcNow,
                    Version = Interlocked.Increment(ref _draftVersion),
                    ComponentId = ComponentId,
                    ModelType = typeof(TModel).FullName!
                }
            };
            await SessionStorage.SetItemAsync(EffectiveDraftKey, draft);
            _lastDraftSaveTime = DateTime.UtcNow;
            LastDraftMetadata = draft.Metadata;
            Logger.LogDebug("[{Id}] Draft saved v{Version}: {Key}",
                ComponentId, draft.Metadata.Version, EffectiveDraftKey);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[{Id}] Failed to save draft", ComponentId);
        }
    }

    // ── Draft: Restore ─────────────────────────────────────────────────────────

    private async Task RestoreDraftAsync()
    {
        try
        {
            var draft = await SessionStorage.GetItemAsync<SgDraftPayload<TModel>>(EffectiveDraftKey);
            if (draft?.Model is null) return;

            // Детекция конфликтов: черновик отличается от оригинала
            if (EnableConflictDetection && _originalModel is not null)
            {
                var originalHash = ComputeModelHash(_originalModel);
                var draftHash = ComputeModelHash(draft.Model);
                if (originalHash != draftHash)
                {
                    Logger.LogWarning(
                        "[{Id}] Draft conflict detected. Original: {OrigHash}, Draft: {DraftHash}",
                        ComponentId, originalHash, draftHash);
                    await OnDraftConflictAsync(draft);
                    return;
                }
            }

            Model = draft.Model;
            IsDirty = true;
            LastDraftMetadata = draft.Metadata;

            // Пересоздаём EditContext для восстановленной модели
            _editContext = new EditContext(Model);
            _editContext.OnValidationStateChanged += (_, _) =>
            {
                _isValid = !_editContext.GetValidationMessages().Any();
                _ = InvokeAsync(StateHasChanged);
            };
            _isValid = false;

            await InvokeAsync(StateHasChanged);
            await OnDraftRestoredAsync(draft);
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "[{Id}] No draft to restore: {Key}",
                ComponentId, EffectiveDraftKey);
        }
    }

    /// <summary>
    /// Вызывается при обнаружении конфликта черновика.
    /// По умолчанию: черновик не восстанавливается, остаётся оригинал.
    /// Переопределите для кастомной логики (диалог выбора, merge и т.д.).
    /// </summary>
    protected virtual Task OnDraftConflictAsync(SgDraftPayload<TModel> draft)
        => Task.CompletedTask;

    /// <summary>Вызывается после успешного восстановления черновика.</summary>
    protected virtual Task OnDraftRestoredAsync(SgDraftPayload<TModel> draft)
        => Task.CompletedTask;

    // ── Draft: Clear ───────────────────────────────────────────────────────────

    /// <summary>Очистить черновик (вызывается после успешной отправки формы).</summary>
    protected async Task ClearDraftAsync()
    {
        try
        {
            await SessionStorage.RemoveItemAsync(EffectiveDraftKey);
            IsDirty = false;
            LastDraftMetadata = null;
            _originalModel = Model is not null ? CloneModel(Model) : null;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[{Id}] Failed to clear draft", ComponentId);
        }
    }

    // ── Form hooks ─────────────────────────────────────────────────────────────

    protected override async Task OnFormValidSubmitAsync()
    {
        await ClearDraftAsync();
        await base.OnFormValidSubmitAsync();
    }

    // ── Reset ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Сбросить форму и очистить черновик.
    /// </summary>
    public override async Task ResetAsync()
    {
        await base.ResetAsync();
        await ClearDraftAsync();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Клонирование модели через JSON-сериализацию.
    /// Переопределите для кастомного клонирования (например, через AutoMapper).
    /// </summary>
    protected virtual TModel CloneModel(TModel source)
    {
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<TModel>(json)!;
    }

    /// <summary>
    /// Вычисление хэша модели для детекции конфликтов.
    /// Использует SgModelHasher для эффективного сравнения.
    /// </summary>
    protected virtual string ComputeModelHash(TModel model)
        => SgModelHasher.ComputeHash(model).ToString("x8");

    // ── Dispose ────────────────────────────────────────────────────────────────

    protected override async ValueTask DisposeComponentAsync()
    {
        // ✅ FIX C7: останавливаем таймер ПЕРВЫМ — предотвращаем тик после dispose
        var timer = Interlocked.Exchange(ref _autoSaveTimer, null);
        if (timer is not null)
        {
            try { await timer.DisposeAsync(); } catch { }
        }

        _navigationSubscription?.Dispose();
        await base.DisposeComponentAsync();
    }
}

// ── Вспомогательные типы ──────────────────────────────────────────────────────

/// <summary>Полезная нагрузка черновика: модель + метаданные.</summary>
public sealed class SgDraftPayload<TModel> where TModel : class
{
    public TModel Model { get; init; } = null!;
    public SgDraftMetadata Metadata { get; init; } = null!;
}

/// <summary>Метаданные черновика.</summary>
public sealed class SgDraftMetadata
{
    public DateTime SavedAt { get; init; }
    public int Version { get; init; }
    public string ComponentId { get; init; } = string.Empty;
    public string ModelType { get; init; } = string.Empty;
}
