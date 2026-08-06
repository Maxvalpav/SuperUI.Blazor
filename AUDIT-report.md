# Аудит SuperUI.Blazor

Дата: 2026-08-05
Объём: каталоги `SuperUI/Base`, `SuperUI/Services`, `SuperUI/Components`

---

## КРИТИЧНЫЕ

1. **`Services/Llm/SgLlmService.cs:80`** (через `SgDebouncer.RunAsync<T>`) — `SgDebouncer.RunAsync<T>` теряет результат: `_ = await func(ct); ... ContinueWith(_ => default(T)!)` — пользователь получает `default(T)` вместо реального значения. Сломанный API.

2. **`Base/ComponentBases/SgOverlayComponentBase.cs:130`** — `catch (OperationCanceledException) { return; }` без сброса `_isClosing` и вызова `OnClosedAsync` → оверлей **застревает в `IsClosing=true`** навсегда, `CloseAsync` повторно вызвать нельзя.

3. **`Base/ComponentBases/SgOverlayComponentBase.cs:201-224`** — двойной вызов `OnClosingAsync`/`OnClosedAsync` при одновременной смене `Visible=false` и вызове `CloseAsync()`.

4. **`Components/Localization/T.razor:13`** — `Localizer.OnLocaleChanged += StateHasChanged;` прямая подписка без `InvokeAsync` → `InvalidOperationException` в Blazor Server.

5. **`Components/Navigation/SgUrlParam.razor:69`** и **`Components/Analytics/SgClickHeatmap.razor:38`** — `async void` подписки на `NavigationManager.LocationChanged` без проверки `_disposed`. Любое исключение роняет circuit/process.

6. **`Services/Network/SgWeatherService.cs:59-71`** — `lon.ToString()` без `CultureInfo.InvariantCulture` → в `ru-RU` получится `1,5` вместо `1.5`, API возвращает ошибку. Реальный баг на не-EN локалях.

---

## ВЫСОКИЕ

7. **`Base/ComponentBases/SgComponentBase.cs:175`** — `protected bool _disposed;` без `volatile`/`Interlocked` → обработчик `ThemeChanged` может видеть stale значение.

8. **`Base/Utilities/SgJsModuleCache.cs:47`** — `_cache.GetOrAdd` фабрика может вызваться дважды при гонке → утечка `IJSObjectReference` проигравшего модуля.

9. **`Base/Utilities/SgFocusManager.cs:25,144-154`** — `Stack<FocusSnapshot>` без `lock`; `TrapHandle.DisposeAsync` без `try/finally` → утечка JS handle если `release` кидает.

10. **`Base/ComponentBases/SgJsComponentBase.cs:69,72`** и **`SgOverlayComponentBase.cs:53,56,59`** — `= default!` на `[Inject]`-сервисах без null-check в `OnInitialized` → NRE при неверной конфигурации DI.

11. **`Components/Forms/Tree/SgTreeSelect.razor:404,444`** — `_dotNetRef = DotNetObjectReference.Create(this)` дважды без `??=` → утечка первого ref + зомби-callback'и.

12. **`Components/Other/SgSettings.razor`** — нет `IDisposable`/`IAsyncDisposable`, `CancellationTokenSource _savedPillCts` (`:398`) **никогда не диспозится**.

13. **`Components/Display/Theme/SgThemeProvider.razor:41`** — `async void OnThemeChanged` подписан на event; race с dispose → crash.

14. **`Services/Data/SgDexieService.cs:92`** и **`Services/Analytics/SgHeatmapService.cs:104`** — `DisposeAsync` без try/catch для `JSDisconnectedException`/`ObjectDisposedException`/`TaskCanceledException` → Dispose срывается при навигации.

15. **`Services/FeatureFlags/SgFeatureFlagService.cs`** и **`Services/Collaboration/SgSignalRCollaborationProvider.cs`** — **не зарегистрированы в `AddSuperUI`** (ServiceCollectionExtensions).

16. **`Services/Llm/SgLlmService.cs:27`** — `Dictionary _modelCache` без lock — гонка → `InvalidOperationException` при resize.

17. **`Services/SgPageTabsService.cs:8-9`** — `List<SgPageTab>` без lock + `Tabs => _tabs` возвращает оригинал, не снимок → `Collection was modified`.

18. **`Services/SgZIndexService.cs:122`** — `ThreadPool.QueueUserWorkItem` для события → подписчики получают callback ВНЕ sync context Blazor; `StateHasChanged` не отрисует.

19. **`Base/Utilities/SgCssUnit.cs:55`** — `,` разрешена в number-части, но `InvariantCulture` парсит как разделитель тысяч → `"1,5px"` становится `15px`.

20. **Многочисленные `catch { }` / `catch (Exception ex)` глушат всё**, включая `OutOfMemoryException`/`ThreadAbortException` — `SgJsComponentBase.cs:254`, `SgOverlayComponentBase.cs:140,217`, `SgLlmService.cs` (32 вхождения).

---

## СРЕДНИЕ / НИЗКИЕ

- **DI lifetime несоответствия**: `SgToastService`, `SgNotificationService`, `SgConfirmService`, `SgEventAggregator` — комментарии говорят Singleton, реально Scoped.
- **`SgLayout/SgShowAt.razor:81`, `Data/Virtual/SgVirtualList.razor.cs:379`** — `StateHasChanged` напрямую из JSInvokable без `InvokeAsync`.
- **Timer race**: `SgCarousel`, `SgProgress`, `SgStatistic`, `SgPerformanceMonitor`, `SgRecorder`, `SgNativeBarcodeScanner` — нет `_disposed` проверки в тике.
- **`SgProgress.razor:949`** — fire-and-forget `_jsModule.DisposeAsync()` в sync `Dispose`.
- **Большие `@code`**: SgSettings.razor (~600 строк), SgJsonSchemaForm.razor (1700+), SgHttpApiTesterFull.razor — вынести в partial class.
- **`SgAnimationCoordinator.cs:52`, `SgThemeService.cs:122`** — `eval` для `matchMedia` (CSP-риск).
- **`CssBuilder.cs:151`** — `$"{existing} {toAdd}"` противоречит комментарию "Zero-allocation".
- **`StyleBuilder.cs:62`** — `AddStyleFromEnum` добавляет `--sg-foo:;` (пустое значение — невалидный CSS).
- **`SgRenderMode.cs:96-118`** — хрупкий reflection на internal `RendererInfo` Blazor → может сломаться при обновлении.
- **`SgWeatherService` / `SgTracerouteService`** — методы без `CancellationToken`.
- **Много `Console.WriteLine`** вместо `ILogger` (SgWeatherService, SgCbrService, SgLlmService:1097).

---

## Топ-5 что фиксить в первую очередь

1. `T.razor` — обернуть `StateHasChanged` в `() => InvokeAsync(StateHasChanged)`.
2. `SgUrlParam.razor` / `SgClickHeatmap.razor` — `async void` → `async Task` + `_ = InvokeAsync(...)`.
3. `SgWeatherService` — `ToString(CultureInfo.InvariantCulture)`.
4. `SgDebouncer.RunAsync<T>` — вернуть реальный результат `func`.
5. `SgOverlayComponentBase:130` — сбрасывать `_isClosing` в `catch OperationCanceledException`.
