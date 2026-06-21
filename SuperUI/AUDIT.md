# SuperUI.Blazor — Code Audit

> Audit of the `SuperUI` component library for logical / architectural errors, incompleteness, stubs and resource leaks.
> Date: 2026-06-20. Scope: `Base/`, `Services/`, `Components/` (Data, AI, Forms, Overlays, Display, Navigation), `wwwroot/*.js`.
> Method: every source file in scope was read (not just grepped). Severities: **CRITICAL** (crash / data corruption / silent wrong results), **HIGH** (broken feature, leak, security), **MEDIUM** (incorrect behaviour in common cases), **LOW** (smell / minor).
>
> Findings marked ✅ were independently re-verified against the source after the audit pass.

---

## 0. Executive summary

The library is large (~370 `.razor`, ~418 `.cs`) and generally well-architected: solid base classes, version-keyed render caches in `SgDataGrid`, correct JS-module coalescing (`SgJsModuleCache`), snapshot-before-dispatch in most collection services. The defects cluster into five themes:

1. **DotNetObjectReference / CancellationTokenSource / linked-CTS leaks** in several services and components.
2. **Default-open overlays get no focus trap** — a single base-class bug that affects every modal/drawer/dialog opened with `Visible=true`.
3. **Parameter-change blindness** — many stateful components read parameters only in `OnInitialized`, so changing `TargetDate`, `AutoPlay`, `ActiveTitle`, etc. after first render does nothing.
4. **LangGraph is an end-to-end stub** shipped with a real-looking UI (checkpoint manager, state inspector) backed by nothing.
5. **LLM key handling & streaming cross-talk** — keys persisted to localStorage / sent to user-controlled URLs; LLM streaming still uses a multicast pattern the RAG service already abandoned.

Top fixes by impact:

| # | Severity | Location | One-liner |
|---|----------|----------|-----------|
| A | CRITICAL | `SgGantt.razor:515` | Unbounded recursion on cyclic deps → StackOverflow (uncatchable) |
| B | CRITICAL | `SgCanvasGrid.razor.cs` | Filter/sort caches alias the same `List<T>`; in-place sort corrupts the filtered cache |
| C | CRITICAL | `SgPivotTable.razor:885` | Roll-up aggregation is O(rows·cols·leaves) string-prefix scan |
| D | HIGH | `SgOverlayComponentBase.cs:161` | Default-open overlays never get focus capture / trap |
| E | HIGH | `SgSelect.razor:856` | `Dispose()` hides base → theme/locale + EditContext subscription leak |
| F | HIGH | `sg-langgraph.js` | Entire LangGraph feature is a non-functional stub |
| G | HIGH | `SgResizeService` / `SgIntersectionService` | `DotNetObjectReference` leaked on every Observe |

---

## 1. Base / Infrastructure

### [HIGH] ✅ `Base/ComponentBases/SgOverlayComponentBase.cs:161-202` — Default-open overlays get no focus trap or focus capture
`OnInteractiveAsync` runs the open cycle for an overlay created with `Visible=true` and sets `_previousVisible = true`, but it only allocates z-index + fires `OnOpening/OnOpened`. The focus capture (`Focus.CaptureAsync()`) and trap install (`Focus.TrapAsync`) live **only** in `OnAfterRenderSafeAsync`'s `Visible && !_previousVisible` branch — which is now skipped because `_previousVisible` is already `true`. Result: a modal/drawer that starts open has no focus trap and no focus-restore on close. (Reported independently by two reviewers.)
**Fix:** Extract the open sequence (z-index + capture + trap + hooks) into one method called by both paths.

### [HIGH] ✅ `Services/SgResizeService.cs:30`, `Services/SgIntersectionService.cs:33` — `DotNetObjectReference` leaked per Observe
`ObserveAsync` creates `var self = DotNetObjectReference.Create(this)` as a local on every call, hands it to JS, and never stores or disposes it. `Unobserve`/`Dispose` only clear `_callbacks`. Every observe leaks a GCHandle that roots the service for the circuit lifetime.
**Fix:** Create one `DotNetObjectReference` field lazily (as `SgNetworkService` does), reuse it, dispose in `DisposeAsync`.

### [HIGH] `Base/Utilities/SgAnimationCoordinator.cs:80-91` — Linked CTS leaked per animation
`Begin/BeginAsync` create `CreateLinkedTokenSource(lifetimeToken)` and never dispose it. Each overlay open/close registers a callback on the lifetime token that survives until the whole coordinator is disposed → unbounded growth on long-lived circuits.
**Fix:** Dispose the linked CTS after the delay, or pass the lifetime token straight to `Task.Delay`.

### [LOW] ✅ `Base/ComponentBases/SgComponentBase.cs:147-168` — Fire-and-forget `InvokeAsync(StateHasChanged)` (assessed, mostly OK)
Theme/locale handlers are `Action`s so awaiting is impossible; the marshal + `_disposed` check are correct. Only gap: exceptions thrown *inside* the async render aren't observed by the surrounding `try/catch`. Low risk — acceptable. Optionally attach a faulted-task continuation that logs.

### [LOW] `Base/Builders/AttributeBuilder.cs:28,79` — `_extra` dictionary stored but never emitted by `Build()` (dead field, silently dropped attributes).
### [LOW] `Base/Builders/StyleBuilder.cs:121-126` — `Add` throws `ArgumentException` on a bad property name during render → crashes `BuildRenderTree` instead of skipping. Skip invalid properties instead.
### [LOW] `Services/SgThemeService.cs:102-107` — Non-atomic init guard; two near-simultaneous `InitializeAsync` can import the module twice. Guard with `SemaphoreSlim`/`Interlocked` (as `SgPuterService` does).

---

## 2. Services / DI

### [HIGH] `ServiceCollectionExtensions.cs:70-98` — HttpClient-dependent services registered without an `HttpClient`
`SgLlmService`, `SgWeatherService`, `SgCbrService`, `SgOpenRouterService`, `SgLlmProxyForwarder`, traceroute/firewall deps all take a plain `HttpClient` via `TryAddScoped`, but `AddSuperUI` never calls `AddHttpClient`. On Blazor Server (no default `HttpClient`) resolving any of them throws `InvalidOperationException`.
**Fix:** Register typed clients (`services.AddHttpClient<SgLlmService>()` …) inside `AddSuperUI`.

### [HIGH] `Services/SgConfirmService.cs:228-239` — Queued confirm dispatched off the renderer Dispatcher
`DispatchNextQueued` runs the next request via `_ = Task.Run(async () => await ExecuteNowAsync(next))`. `ExecuteNowAsync` invokes a Blazor component handler — running component logic on a thread-pool thread violates Blazor Server's threading model ("current thread is not associated with the Dispatcher").
**Fix:** Drain the queue on the original continuation, or marshal through the host's `InvokeAsync`.

### [MEDIUM] `Services/SgConfirmService.cs:149-150` — TOCTOU race shows two dialogs at once
The "active vs enqueue" decision reads `Volatile.Read(ref _activeCount) > 0` outside any lock. Two callers arriving while `_activeCount == 0` both bypass the queue → overlapping dialogs. **Fix:** make the test-and-increment atomic under `_queueGate`.

### [MEDIUM] `Services/AI/SgLangGraphService.cs:34-35` — `_selfRef` / JS instance leaked on re-init. `InitializeAsync` reassigns `_selfRef` + `_instanceId` without disposing the previous ones. **Fix:** dispose old `_selfRef` + JS instance before reassigning; guard re-entrancy.

### [MEDIUM] `Services/SgZIndexService.cs:123-174` — `TopOwnerChanged` raised via `ThreadPool.QueueUserWorkItem`; subscribers calling `StateHasChanged` run off the renderer SyncContext. **Fix:** invoke on the captured Dispatcher.

### [MEDIUM] `Services/Collaboration/SgSignalRCollaborationProvider.cs:21-45` — **Stub presented as a SignalR provider.** `ConnectAsync` is `await Task.CompletedTask`, send methods only echo to local subscribers, `DisposeAsync` empty, `OnPresenceChanged` never raised. **Fix:** implement `HubConnection` or mark test-only and don't register as default.

### [MEDIUM] `Services/SgSettingsService.cs:63,70` — `ResetAllSettingsAsync`/`ClearCacheAsync` call `localStorage.clear()`, wiping **all** origin data (host app + other SuperUI keys). **Fix:** delete only owned `sui-*` keys (use `SgStorageService.RemoveByPrefixAsync`).

### [MEDIUM] `Services/SgPageTabsService.cs:9` — `Tabs => _tabs` exposes the live backing list; enumeration during `OpenTab`/`RemoveTab` throws. **Fix:** return a snapshot / lock (like the other collection services).

### [MEDIUM] `Services/SgEventAggregator.cs:54-71` — `Publish` `try/catch` only traps faults before the first `await`; post-await faults become unobserved. Also the XML doc says "Singleton" while DI registers it **Scoped** — following the doc would leak handlers across circuits. **Fix:** await/continue-with-log handlers; reconcile doc with registration.

### [MEDIUM] `Services/FeatureFlags/SgFeatureFlagService.cs` — `_flags` is a plain `Dictionary` mutated without synchronization, and the service is **never registered** in `AddSuperUI`. **Fix:** `ConcurrentDictionary` + register (or document).

### [LOW] `Services/Network/SgTracerouteService.cs:38-112` — Fabricated hops (`new Random()`, synthetic latency, hard-coded `192.168.1.1`) surfaced as a real traceroute. Only the GeoIP endpoints are real. Rename/document as a simulation.
### [LOW] `Services/SgConfirmService.cs:64-68` — Injected `IOptions<SuperUiOptions>` read into a local and never used (dead dependency).
### [LOW] Multiple services (`SgLlmService:1088,1212`, `SgWeatherService:83`, `SgCbrService:70`) log errors via `Console.WriteLine` instead of the injected `ILogger` → invisible to the host pipeline.

---

## 3. Data components

### [CRITICAL] ✅ `Components/Data/Gantt/SgGantt.razor:515-528` & `349-359` — Unbounded recursion on cyclic dependencies
`MoveTaskWithDependencies` (and `MarkCriticalRecursive`) recurse through dependents/predecessors with **no visited set**. A dependency cycle (A↔B — the editor only blocks self-references) causes infinite recursion → **StackOverflow**, which cannot be caught and kills the process.
**Fix:** thread a `HashSet<string>` of visited task IDs through both recursions.

### [CRITICAL] `Components/Data/Canvas/SgCanvasGrid.razor.cs:789-815` — Filter/sort caches alias the same list; in-place sort corrupts the filtered cache
`ProcessDataInBackground` assigns `_sortedCache = filtered` where `filtered` may *be* the shared `_filteredCache` reference, then calls `filtered.Sort(...)` **in place** — mutating `_filteredCache`. A later unsorted read (cache hit) returns sorted order.
**Fix:** copy before sorting (`filtered = new List<TItem>(filtered)`); never alias `_filteredCache` into the sortable list.

### [CRITICAL] `Components/Data/Pivot/SgPivotTable.razor:885-905` — Roll-up aggregation is O(rowPaths · colPaths · valueFields · leaves)
For each (rowPath, colPath, valueField) triple it linearly scans all `leafResults` with `StartsWith` prefix checks. 200×100×500 ≈ 10M comparisons per recalc, on every parameter change.
**Fix:** accumulate bottom-up — for each leaf, walk its own ancestor paths into a `Dictionary<(rAnc,cAnc,field), acc>`. O(leaves · depthR · depthC).

### [HIGH] `Components/Data/DataGrid/SgDataGridData.cs:1685-1736` — Tree-filter mutates `_expandedTreeNodes` inside the memoized `GetFilteredRows()`
`AddTreeItemRecursive` writes expansion state from inside a version-cached getter. The auto-expand side effect only fires on cache-miss, so render output depends on cache state (a "read" with hidden persistent side effects).
**Fix:** compute auto-expand-on-filter separately / store it in the cached result; never mutate expansion state from a memoized getter.

### [HIGH] `Components/Data/DataGrid/SgDataGridData.cs:1602-1623` — Virtualized visible-rows cache not keyed on scroll position
`GetVisibleRows()` caches by items/filter/sort/columns version only — not `_scrollTop`/viewport. `OnScrollAsync` hacks around it by forcing `_visibleRowsCacheItemsVersion = -1`; any other caller gets stale rows.
**Fix:** include scroll/viewport/row-height in the cache key when virtualization is active.

### [HIGH] `Components/Data/DataGrid/SgDataGridUI.cs:612-641` — Shift-range select silently no-ops across pages/virtual windows
Range select uses `GetVisibleRows().IndexOf(...)`; if the anchor is on another page, index is `-1` and the shift-click does nothing with no feedback. **Fix:** resolve indices against `GetFilteredSortedRows()` (the full ordered set).

### [HIGH] `Components/Data/Pivot/SgPivotTable.razor:540-552,907` — User sort silently reset every recalc
`CalculateTableAsync` always rebuilds `_rowPaths = …OrderBy(x => x)`, discarding any `ToggleSort`/`SortData`. Sort appears broken on any re-render. **Fix:** re-apply active `_sortCol`/`_sortAsc` at the end of recalc.

### [MEDIUM] `Components/Data/Table/SgTable.razor.cs:157,180-214` — `FilteredAndSortedItems` is uncached; full filter+sort+`ToList` runs 4×/render
Read by `TotalCount → TotalPages → CurrentPage → PagedItems` plus razor, each re-running the whole pipeline with per-cell `ToLowerInvariant()` allocations. **Fix:** memoize per (items, search, sort) generation; use `IndexOf(…, OrdinalIgnoreCase)`.

### [MEDIUM] `Components/Data/Table/SgTable.razor.cs:295-322` — Column resize is a JS-interop busy loop: while resizing, every `OnAfterRenderAsync` calls `pollResizeEvent` then `StateHasChanged()` → render → poll. **Fix:** push model via `DotNetObjectReference` pointermove callback.

### [MEDIUM] `Components/Data/Pivot/SgPivotTable.razor:956,1080` — `Convert.ToDouble(val)` unguarded inside the background `Task.Run`; a non-numeric value field throws, leaving `_isCalculating=true` forever (permanent spinner) and an unobserved exception. **Fix:** `TryConvert` + `finally { _isCalculating = false; }`.

### [MEDIUM] `Components/Data/Pivot/SgPivotTable.razor:781-793` — `CalculateHash` samples only `Items.Take(100)` + count; edits beyond row 100 don't trigger recalc → stale aggregates. **Fix:** expose explicit `Refresh()` / item-revision, or hash all.

### [MEDIUM] `Components/Data/Virtual/SgVirtualList.razor.cs:56-59 vs 108-112` — Duplicate contradictory params `PreserveScrollOnItemsChange` and `PreserveScrollPositionOnItemsChange` (one sets the flag, the other does the restore) → half-applied behaviour. **Fix:** collapse to one.

### [MEDIUM] `Components/Data/Kanban/SgKanban.razor:362-365` — New task `Order = count(tasks)` can collide with an existing sparse max+gap order → duplicate `Order`, nondeterministic sort. **Fix:** normalize order within a column after add/move.

### [MEDIUM] Hard-coded Russian UI strings bypass the localizer and (worse) are used as filter keys: `SgCanvasGrid.razor.cs:741,1047,1456` `"(Пусто)"`, `SgKanban.razor:206` `"Цвет"`, `SgTreeDataGrid.razor:50` `"Нет данных"`. Changing locale orphans stored filters. **Fix:** route through `Localizer`; use a culture-neutral sentinel for the empty-filter key.

### [LOW] `Components/Data/Gantt/SgGantt.razor:448-461` — "Export PNG" toolbar button is a stub: it `eval`s a script that only shows an `alert`. Also uses `JS.InvokeVoidAsync("eval", …)` (CSP risk). Implement or hide; never `eval`.
### [LOW] `Components/Data/DataGrid/SgDataGridUI.cs:428-431` — `HandleChooserFocusOutAsync` is an empty `await Task.CompletedTask` → column chooser never closes on blur.
### [LOW] `Components/Data/Table/SgTable.razor.cs:463-479` — `_selectAll` is a sticky bool, desyncs across pages/filter. Derive from `PagedItems.All(IsSelected)`.
### [LOW] `Components/Data/DataGrid/DataDecimation.cs` — LTTB utility lives under DataGrid but is only used by Charts (misplaced).

---

## 4. AI components

> **LangGraph (`Components/AI/Experimental/LangGraph/*`, `Services/AI/SgLangGraphService.cs`, `wwwroot/sg-langgraph.js`) is an end-to-end stub.** The JS file self-declares "This is a stub implementation", never imports `@langchain/langgraph`, and the engine is a hand-rolled while-loop over C# callbacks. The LLM and RAG services are genuinely implemented; findings below are real bugs within them.

### [HIGH] `wwwroot/sg-langgraph.js:1-177` — Complete stub, not a graph engine. No `StateGraph`, no compile, no checkpointer/persistence. All 7 LangGraph razor components (incl. "Checkpoint Manager", "State Inspector") are a non-functional facade. **Fix:** implement a real bridge or clearly gate as demo-only.
### [HIGH] `sg-langgraph.js:90-135` — Entry node hardcoded to `"start"`; a graph whose nodes are e.g. `agent`/`tools` exits immediately producing only "Workflow completed." Derive entry from config.
### [HIGH] `Services/AI/SgLangGraphService.cs:87-94` + `sg-langgraph.js:151-163` — Tool-calling path is dead code: `OnToolCallInternal` is never invoked from JS, so `SgLangGraphToolExecutor`/`OnToolCall` can never fire.
### [LOW] `sg-langgraph.js:91` — Loop guard counts *distinct* nodes (`visitedNodes.size > 100`); a 2-node cycle `a→b→a` (size=2) loops forever. Track total iterations.

### [HIGH] `Services/Llm/SgLlmService.cs:489-497` + `SgChat.razor.cs:55,196` — Streaming cross-talk between concurrent chats
`ChatAsync` streams via service-wide `OnTokenReceived`/`OnChatComplete` with a fixed stream id `"default-stream"`. Every `SgChat` subscribes to the same events → two chats/sessions interleave tokens. The RAG service was refactored to per-stream routers; the LLM service was not.
**Fix:** adopt the per-stream-id channel routing from `SgRagService.StreamCoreAsync`.

### [HIGH] `Services/Llm/SgLlmService.cs` / `wwwroot/sg-rag.js:1652` — API key sent to user-controlled `BaseUrl` with no allow-list
`baseUrl` is fully config-controlled and persisted in localStorage; `Authorization: Bearer ${apiKey}` is attached to `${baseUrl}/chat/completions`. A mis-set/malicious BaseUrl exfiltrates the key. Google paths also put the key in the **query string** (`?key=`, lines 519/863/1503) → leaks into history/proxy logs. **Fix:** validate BaseUrl host against the selected provider; move Google key to a header.

### [MEDIUM] `Services/Llm/SgLlmService.cs:39-40,103,129` — API keys persisted to localStorage in plaintext (`PersistApiKey` defaults `true`); `ImportProfilesJsonAsync` skips `SanitizeForStorage`. **Fix:** default `false`; sanitize the import path.

### [MEDIUM] `Services/Llm/SgLlmService.cs:1198-1208` — Embeddings batch assumes response order matches input order; the OpenAI response carries `index` (not even parsed in the DTO) and isn't guaranteed ordered → silently misaligned RAG vectors. **Fix:** parse and sort by `index`.

### [MEDIUM] `Services/AI/Rag/SgRagService.cs:460-490` — `StreamCoreAsync` `finally` awaits the JS task with no timeout and only completes the stream on the JS callback / fault — a success with no callback hangs the `finally`. **Fix:** add a timeout / complete on JS-task success as a fallback.

### [MEDIUM] `Services/Llm/SgLlmProviderRegistry.cs:963` — Mixed `&&`/`||` without parens in provider detection (`A || github && models`); fragile, can mis-detect Azure `…/models` URLs. Parenthesize.
### [MEDIUM] `Services/Llm/SgLlmProviderRegistry.cs:998-999` — Unknown hosts default to `OpenAiCompatible`, then `NormalizeBaseUrl` may append `/v1` and mangle custom servers. Return a "custom" variant instead.
### [MEDIUM] `Services/Llm/SgLlmService.cs:563,698,843` — Catalog calls use the shared `HttpClient` with no timeout/CT; some `catch { return fallback; }` swallow everything with no log → silently show stale model lists.

### [LOW] `Services/Llm/SgLlmService.cs:1454-1490` — Vision/image/TTS/moderation/rerank hardcode OpenAI route + `Bearer` + `/v1`; Anthropic (`x-api-key`), Azure (`api-key`+`api-version`), Google will 401/404.
### [LOW] `Services/Llm/SgLlmService.cs:1739-1758` — `JsonDocument.Parse(...).RootElement` captured without `using`; RootElement used after the document is GC-able (UB). Use `Deserialize<JsonElement>`.
### [LOW] `Services/Llm/SgPuterService.cs:83-151` — Most interop methods (`Txt2ImgAsync`, `KvSet/Get`, `FsRead/Write`, `Notify`…) lack the `JSDisconnectedException`/`TaskCanceledException` guards that `ChatAsync` has → unhandled throws on teardown.
### [LOW] `wwwroot/sg-rag.js:1599-1601` — Legacy multicast `OnStreamTokenCallback` still fired alongside the per-stream callback → reintroduces the cross-talk the refactor removed.

---

## 5. Forms / Overlays / Display / Navigation

### [HIGH] ✅ `Components/Forms/Select/SgSelect.razor:856` — `Dispose()` hides base → theme/locale + EditContext subscription leak
`SgSelect` declares `public void Dispose()` **without `override`**, hiding `SgInputBase<TValue>.Dispose()` (which unsubscribes `EditContext.OnValidationStateChanged` and calls `SgComponentBase.Dispose()` → theme/locale unsubscribe). Because the re-declared method re-implements `IDisposable`, the framework's dispose call lands on `SgSelect.Dispose()` and **never reaches base** → leaks the EditContext handler *and* the theme/locale handlers. (Verified: base `Dispose` at `SgInputBase.cs:119`; subscription at `:65`.) Additionally `SgSelect` creates its own `SgEditContextBinder` that double-subscribes alongside the base.
**Fix:** make `Dispose` an `override` calling `base.Dispose()`; drop the redundant `_binder` or stop the base from subscribing.

### [HIGH] `Components/Overlays/Tooltip/SgTooltip.razor.cs:15` — Tooltip traps keyboard focus on hover
Inherits `SgOverlayComponentBase` whose `UseFocusTrap` defaults `true`; every hover-show traps focus on the trigger, breaking tab order for a non-modal element. **Fix:** `protected override bool UseFocusTrap => false;`.

### [MEDIUM] `Components/Forms/Radio/SgRadioGroup.razor:85,216` — `FocusAsync()` targets an unbound `_inputRef` (no `@ref` in markup) → no-op/throws. **Fix:** bind `@ref` to the first enabled radio.

### [MEDIUM] `Components/Navigation/Tab/SgTabs.razor:168-225` — Arrow-key nav never moves DOM focus (roving tabindex). After Arrow, focus is stranded on the now-`tabindex=-1` tab. **Fix:** `await tab.TabElementRef.FocusAsync()` after activate.
### [MEDIUM] `Components/Navigation/Tab/SgTabs.razor:121-126` — `ActiveTitle` only seeded in `Register`; no `OnParametersSet` sync, so programmatic active-tab changes are ignored.

### [MEDIUM] `Components/Display/Countdown/SgCountdown.razor.cs:198-210` — `TargetDate`/`InitialTimeLeft` read only in `OnInitialized`; changing them later does nothing. **Fix:** detect changes in `OnParametersSet`, recompute, restart timer.
### [MEDIUM] `Components/Display/Carousel/SgCarousel.razor:114-117` — AutoPlay/Interval timer started only in `OnInitialized`; toggling `AutoPlay` or changing `Interval` later has no effect (and AutoPlay→false doesn't stop it). **Fix:** reconcile timer in `OnParametersSet`.

### [MEDIUM] `Components/Display/Notification/SgNotificationPanel.razor.cs:24,228-245` — `_searchCts` recreated per keystroke but the component isn't `IDisposable` → CTS + pending `Task.Delay` leak. **Fix:** implement `IDisposable`, dispose previous CTS before reassigning.
### [MEDIUM] `Components/Display/Notification/SgNotificationPanel.razor.cs:448-467` — Swipe-to-dismiss: `OnTouchMove` overwrites the start-X slot with the delta, so each move measures relative to the previous delta → `dx < -80` test wrong. **Fix:** keep start-X separately, `dx = currentX - startX`.

### [MEDIUM] `Components/Forms/Select/SgSelect.razor:730-746` — When grouped (`GroupSelector`), render order (grouped) differs from `_activeIndex` order (flat `GetFilteredItems()`), so Enter selects the wrong item. **Fix:** index rendering and selection off one shared ordered list.
### [MEDIUM] `Components/Forms/Masked/SgMaskedInput.razor:434-441` — `SelectOnFocus` only refocuses, never selects text. **Fix:** call a select on focus.
### [MEDIUM] `Components/Overlays/Drawer/SgDrawer.razor.cs:237-243` — `OnClose` (documented "begins closing") actually fires in `OnClosedAsync` together with `OnClosed`. **Fix:** move `OnClose` to `OnClosingAsync`.

### [LOW] `Components/Display/Notification/SgNotificationToast.razor:69-76` — `DismissAsync` `Task.Delay(300)` not tied to `_cts`; can `StateHasChanged`/callback after disposal.
### [LOW] `Components/Display/Carousel/SgCarousel.razor:73-76` — `GoTo` writes the `ActiveIndex` parameter directly; without two-way binding a parent render resets it mid-rotation. Track in a private field.
### [LOW] `Components/Forms/Masked/SgMaskedInput.razor:402-651` — Cursor/selection/paste all via `JS.InvokeAsync("eval", …)` of interpolated script → CSP-fragile. Move to a typed JS module function.
### [LOW] `Components/Overlays/Dropdown/SgDropdown.razor.cs:306-335` — Space/Enter activation lacks `preventDefault` → page scrolls on Space.

---

## 6. Recommended remediation order

1. **Crashes / corruption first:** Gantt recursion (A), Canvas cache aliasing (B), Pivot unguarded `Convert.ToDouble`.
2. **Leaks:** `SgSelect.Dispose` (E), Resize/Intersection `DotNetObjectReference` (G), `SgAnimationCoordinator` linked CTS, `SgNotificationPanel` CTS.
3. **Accessibility correctness:** overlay default-open focus trap (D), tooltip focus trap, tab keyboard focus.
4. **Security:** LLM key-to-BaseUrl exfiltration, key-in-query-string, plaintext persistence default.
5. **Perf:** Pivot roll-up (C), `SgTable` uncached pipeline, DataGrid scroll-cache key, Table resize busy loop.
6. **Decide & gate:** LangGraph (ship a real bridge or mark demo-only), `SgSignalRCollaborationProvider`, `SgTracerouteService` simulation labelling.
7. **DI:** register `HttpClient` typed clients; register/`ConcurrentDictionary` the feature-flag service.
8. **Parameter-change blindness sweep:** Countdown, Carousel, Tabs `ActiveTitle`, Pivot sort/hash.
