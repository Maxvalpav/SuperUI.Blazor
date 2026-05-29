# AI Components Audit & Remediation Plan

**Дата:** 2026-05-28
**Скоуп:** `SuperUI/Components/AI/` — три раздела: `Llm/`, `Rag/`, `LangGraph/`.
**Метод:** параллельный аудит трёх агентов с чтением всех файлов раздела. Найденные проблемы сгруппированы по серьёзности; в конце — приоритезированный план работ.

---

## 0. Общий вердикт

| Раздел | Состояние | Главные проблемы |
|---|---|---|
| `AI/Llm` | Шаблонная копипаста с одинаковыми багами в большинстве компонентов. `SgChat` отполирован, остальные — нет. | Утечка handler'ов в `SgLlmEmptyState`, проглатывание исключений, утечки `IJSObjectReference`, `@bind-Value` на `[Parameter]`, отсутствие XML-доков. |
| `AI/Rag` | Богатый, цельный по доменной модели. JS-интероп — узкое место. | Race condition в стриминге (один multicast event на N caller'ов), busy-wait polling, `Dispose()` без `IDisposable`, `eval`-загрузка, тяжёлый base64 на UI-потоке. |
| `AI/LangGraph` | Прототип. Не готов к публикации в NuGet. | Нарушение префикса `Sg`, инлайн `<style>`, `async void`, `DisposeAsync` сервиса из DI, заглушки в Checkpoint Manager, отсутствие XML-доков, инлайн-логика без code-behind. |

Самое важное: **LangGraph не должен ехать в публичный NuGet под текущими именами** — переименование позже = major-bump. Решение принять **до** следующего минора.

---

## 1. Critical bugs (исправлять первыми)

### 1.1 Утечки event-handler'ов / неработающий Dispose

- `SgLlmEmptyState.razor` — `public void Dispose()` без `@implements IDisposable`. Blazor его не вызывает. **Pinned handler** на синглтоне `ILlmService` при каждом размонтировании.
- `SgRagModelLoader.razor:367` — тот же баг (`@implements IDisposable` отсутствует, три handler'а из `OnInitialized:268-270` живут навсегда).
- `SgRagSaveLoadDb.razor:176` — то же + `System.Timers.Timer` остаётся живым на всю страницу.
- `LangGraph/GraphStreamingChat.razor` — `Provider.StepReceived += HandleStep` без `IDisposable`.
- `LangGraph/LangGraphVisualizer.razor:72-84` — то же.
- `LangGraph/StateInspector.razor:42-46` — анонимная лямбда, отписаться нельзя в принципе.
- `LangGraph/AgentCheckpointManager.razor:54` — то же.
- `LangGraph/HumanInTheLoopInterrupter.razor:55-67` — то же.

**Действие:** один проход «add `@implements IDisposable` + сохранить delegate как поле + отписать в `Dispose`».

### 1.2 `async void` и fire-and-forget

- `LangGraph/LangGraphProvider.razor:52,60,67,76` — `HandleStep`, `HandleError`, `HandleInitialized`, `HandleInterrupt` все `async void`. `StateHasChanged` после `await` без `InvokeAsync` упадёт `InvalidOperationException`-ом при вызове из не-renderer-потока.
- `SgRagSaveLoadDb.razor:88` — `_autoTimer.Elapsed += async (_, _) => …`. Любое исключение крашит процесс на Server-хосте.
- `SgChat.razor.cs:207-242`, `SgPuterChat.razor.cs:98-117` — `InvokeAsync(async () => { … })` — Task на пол, исключения теряются.

### 1.3 Гонки данных и multicast race

- `SgRagService.AskStreamAsync` (`:364-421`) и `ChatDirectStreamAsync` (`:429-481`) — лямбды подписываются на **сервис-wide multicast events** (`OnStreamToken/OnStreamComplete/OnError`). `streamId` создаётся, но JS-сторона его не возвращает. Два параллельных стрима = смешанные токены, первый complete рубит оба, ошибка в одном рубит другой.
- `LangGraph/GraphStreamingChat.razor:78,98-100` — `_messages` мутируется из service-события (вероятно вне renderer-потока) без `InvokeAsync`.
- `LangGraph/LangGraphProvider.razor:31-35` — две инстанции `LangGraphProvider` (например, prerender + interactive) одновременно подписываются и при Dispose отписывают чужие handler'ы.

### 1.4 Уничтожение чужого ресурса

- `LangGraph/LangGraphProvider.razor:93-101` — `await LangGraphService.DisposeAsync()` в `DisposeAsync` компонента. **Сервис из DI** — следующий рендер страницы получит disposed-сервис.

### 1.5 Двусторонний биндинг через `@bind-Value` на `[Parameter]`

- `SgOllamaDashboard.razor:36` — `<SgTextBox @bind-Value="BaseUrl" />` мутирует параметр без `BaseUrlChanged`.
- `SgOpenRouterDashboard.razor:45` — то же для `ApiKey`.
- `SgLlmSettings.razor.cs:11-13` — `Config` мутируется в-place через всё тело.

### 1.6 Проглатывание исключений (нет `catch`)

- `SgLlmReranker.razor:71-91`, `SgLlmImageStudio.razor:103-126`, `SgLlmSpeaker.razor:73-96`, `SgLlmTranscriber.razor:105-127`, `SgOllamaModelPicker.LoadModelsAsync:137-154`, `SgOpenRouterModelPicker.LoadModels`. Кнопка перестаёт крутиться, и больше ничего не происходит — нет `_error`, нет toast'а.
- `LangGraph/BlazorToolExecutor.razor:17-22` — если `OnToolCall.InvokeAsync` бросает, модель видит «успех» с пустым результатом.
- `SgRagSaveLoadDb.LoadSnapshotsAsync:159` — `catch {}`, пользователь видит пустой список.

### 1.7 Безопасность

- `SgRagChat.razor:582-583`, `SgRagSaveLoadDb.razor:103-104` — `JS.InvokeVoidAsync("eval", $"…{fileName}…")`. Сейчас `fileName` из `DateTime.Now`, но шаблон — XSS-sink на будущее. Заменить на `downloadBlob(name, mime, base64)` в `sg-rag.js`.

### 1.8 Заглушки, выдающие себя за работающие фичи

- `LangGraph/AgentCheckpointManager.razor:67-87` — `SaveCheckpointAsync`, `LoadCheckpointAsync`, `ResetThreadAsync`, `RestoreToStep` — пустые. Кнопка «Save» молча ничего не делает.
- `SgRagDocumentUploader.razor:147-151` — `OnDrop` сбрасывает только `_isDragging`, файлы не читаются. Drop-зона приглашает дроп, который игнорируется.
- `SgRagFilterBuilder.razor` — обёртка над `SgFilterBuilder` без проброса фильтра в `SearchAsync`/`AskAsync`.
- `SgRagAnalyticsPanel.razor:46-53` — `Summary` принимается параметром, но `SgRagService` ничего не логирует.
- `SgRagConversationMemory.cs` — нигде не зарегистрирован, summary растёт неограниченно, `_messages` не очищается.

### 1.9 Прочее по списку

- `SgLlmTranscriber/VisionAnalyzer/ImageStudio/VideoAnalyzer` — `MemoryStream.ToArray()` + Base64 на UI-потоке для файлов до 200 МБ. WASM зависает на секунды.
- `SgRagService.IngestFileAsync:224-227` — то же для 100 МБ PDF.
- `SgRagChat.razor:346-349` — то же для аттачей чата (20 МБ).
- `SgRagService.cs:410, :470` — `Task.Delay(16, CancellationToken.None)` в IAsyncEnumerable, не реагирует на отмену, 60 fps busy-wait.
- `SgOpenRouterKeyInfo.razor:101-107` и `SgOpenRouterModelDetails.razor:161-171` — HTTP-запрос на каждый keystroke `ApiKey`/`ModelId` без debounce.
- `LangGraph/AgentCheckpointManager` — `_history` растёт неограниченно (full state object на каждый step).
- `SgPuterChat.razor.cs:33-64` — выдуманные ID моделей (`claude-opus-4.7-thinking`, `gpt-5.5-high`, `gemini-3.1-pro`). При вызове Puter — 404 / Unknown Model.

---

## 2. Architecture issues

### 2.1 Нарушения CLAUDE.md (контракт проекта)

| Правило | Нарушение |
|---|---|
| Префикс `Sg` для всех публичных компонентов | Все 7 файлов `AI/LangGraph/` — без префикса. |
| Code-behind `Sg<Name>.razor.cs` для нетривиальной логики | Весь LangGraph — 30–90 строк C# инлайн. `LangGraphProvider` — 102 строки. |
| XML-доки на всю публичную поверхность (CS1591) | `AI/Llm/` — почти везде отсутствуют (`SgChat`, embedder, moderator, reranker, speaker, structured, studio, tool playground, transcriber, video/vision analyzer, image studio, dashboards, model pickers, provider picker, Puter chat). `AI/Rag/` — `SgRagService` events + `ListCollectionsAsync`/`CreateCollectionAsync`/`DeleteCollectionAsync`/`ListDocumentsAsync`/`GetDocumentAsync`/`ClearCollectionAsync`, все property `SgRagDocument`/`SgRagChunk`/`SgRagSearchHit`/`SgRagChatMessage`/`SgRagAnswer`/`SgRagSnapshotInfo`/`SgRagCollectionInfo`, enum `SgRagDocumentFormat`/`SgRagSnapshotKind`. `LangGraph/` — везде. |
| Bundled CSS в `superui-components.css` | `SgChat.razor.css`, `SgPuterChat.razor.css` + многочисленные `SgRag*.razor.css` + инлайн `<style>` блоки в каждом `LangGraph/*.razor`. |
| en-US + ru-RU через `ISuperUILocalizer` | Hardcoded ru: `SgLlmEmptyState` (`:67, 76-80, 86-88, 96`), `LangGraph/GraphStreamingChat` («Введите сообщение…», «Агент думает»), `LangGraph/HumanInTheLoopInterrupter` («Требуется одобрение»). |

### 2.2 Несогласованность пространств имён

- `SgChat` + Ollama/OpenRouter/Puter → `SuperUI.Components.Llm`.
- Остальные LLM-компоненты → `SuperUI.Components`.
Один и тот же раздел требует двух `using`.

### 2.3 Дублирование

- `SgChat` и `SgPuterChat` переписывают один и тот же JS-импорт, markdown-рендер, blob-downloader, image-URL escaping, dispose. Вынести в `SgChatBase` или хелперы `sg-llm.js`.
- `SgRagChat` дублирует логику model picker / mode toggle / attachments / streaming — у `AI/Llm/` уже есть `SgLlmProviderPicker`, `SgOpenRouterModelPicker`, `SgLlmEmbedder`, `SgLlmReranker`. Вытащить `SgLlmProviderConfig` и переиспользовать пикеры.
- `SgRagModelLoader` инлайнит свой провайдер-пикер.

### 2.4 God-сервисы

- `SgRagService` ≈750 строк: модели, документы, поиск, RAG, прямой чат, persistence, snapshots, reranker, export. Разделить: `SgRagModelManager`, `SgRagIndexer`, `SgRagSearcher`, `SgRagPersistence` с общей ссылкой на JS-модуль.
- `SgLlmSettings.LoadModelsAsync` — switch на 140 строк по провайдерам. Заменить на стратегии в `SgLlmProviderRegistry`.

### 2.5 Cascading контракт

- `SgRagProvider` использует именованный cascade `Name="RagService"` — забыл совпадение строки = тихо сломанный UI. Заменить на типизированный `SgRagContext` record (service + ready-state).
- LangGraph-консьюмеры зависят от `[CascadingParameter] LangGraphProvider` и молча no-op'ят при `null`. Минимум — выкидывать explicit ошибку в `OnParametersSet`.

### 2.6 Поверхностное состояние LangGraph

- Заглушки + нестабильные сигнатуры событий + дублирование `EventCallback` + `event Action` для одних и тех же сигналов. Кандидат на `Components/AI/Experimental/LangGraph/` + `[Experimental("SUPERUI_LANGGRAPH")]` до стабилизации API.

### 2.7 Чужой контракт

- `SgPuterChat` обходит `ILlmService` / `SgLlmConfig` и зовёт `SgPuterService` напрямую — то самое, против чего предостерегает README.
- `SgLlmSettings.Config` мутируется in-place в десятках мест, родитель видит промежуточные состояния втихую.

---

## 3. UX issues

1. **Нет cancel-кнопки** в долгих операциях: `SgRagSearchPanel`, `SgLlmReranker/Transcriber/Speaker/ImageStudio/VideoAnalyzer/VisionAnalyzer`, embedding в `SgRagDocumentUploader`. Долгие LLM-вызовы нельзя прервать.
2. **Нет prompt-confirm на деструктив**: `SgRagVectorDbPanel` — `Clear collection`, `Delete document`; `SgRagSaveLoadDb` — `Import(merge:false)`. `SgConfirmHost` уже в проекте.
3. **Нет error-surface** при провале: `SgRagSaveLoadDb.LoadSnapshots`, `SgOllamaModelPicker.LoadModels`, `SgLlmTranscriber/Speaker/ImageStudio` — кнопка просто перестаёт крутиться.
4. **Чат без autoscroll**: `_messagesRef` захвачен, но `scrollIntoView` не вызывается (`LangGraph/GraphStreamingChat`, `SgRagChat`).
5. **Чат без виртуализации**: `SgChat`, `SgPuterChat`, `SgRagChat` — `foreach` по всем сообщениям; на 200+ сообщений рендеринг провисает.
6. **Markdown re-encode на каждый рендер** (`SgChat.razor.cs:337-341`) для незакешированных user-сообщений.
7. **Enter без `e.IsComposing` и `preventDefault`** в `SgChat`, `SgPuterChat`, `LangGraph/GraphStreamingChat` — IME-пользователи (CJK) отправляют половину набранного.
8. **Textarea `rows="1"` без auto-resize** в `SgChat`.
9. **Модалка interrupter** в LangGraph без Esc, focus-trap, `aria-modal`; очередь interrupt'ов перезаписывается.
10. **`StateInspector` (LangGraph)** хардкодит тёмную тему (`#1e1e1e`), игнорирует CSS-переменные библиотеки.
11. **`SgRagChat` mode switcher** двусмысленный (клик на активный режим визуально что-то делает, фактически — нет).
12. **`SgLlmImageStudio._qualities`** смешивает словари DALL-E 3 (`standard/hd`) и gpt-image-1 (`low/medium/high`) — половина комбинаций гарантированно падает.
13. **Drag-and-drop в `SgRagDocumentUploader`** — UI приглашает дроп, который игнорируется.
14. **Аттачи PDF/DOCX в чате** — иконка без превью / счётчика страниц.
15. **`SgLlmEmptyState`** — все подсказки только на русском, минуя `ISuperUILocalizer`.
16. **`SgRagSearchPanel`** — нет empty-state до первого запроса (пустой блок).
17. **`SgRagDocumentUploader`** — прогресс без ETA на больших PDF.
18. **`SgRagSaveLoadDb`** — снимки без timestamp в UI (`CreatedAt` теряется парсером в `SgRagService.cs:708-713`).
19. **Аria-доступность** по всему `AI/Llm/`: history items без `role=listbox`, `✕` без `aria-label`, стрелки не навигируют, ARIA-live спамит при стриминге.
20. **`SgRagChat._input`** очищается до `await SendAsync` — при ошибке вопрос теряется.
21. **Spinner вида «Агент думает: {node}…»** — машинный node-id показывается пользователю.

---

## 4. Optimization opportunities

1. **Multicast event → per-stream Channel** (`SgRagService.AskStreamAsync` / `ChatDirectStreamAsync`): заменить busy-wait + multicast на `Channel<string>` на стрим. Решает гонку и убирает 60 fps polling.
2. **Throttle прогресс-событий**: `SgRagDocumentUploader` рендерится на каждый чанк (4000-чанковый PDF = 4000 рендеров). Throttle до ~10 Hz.
3. **JS-стриминг markdown как diff**: `SgRagChat.RenderMarkdownAsync` сериализует весь текст и зовёт `marked` каждые 6 токенов — 50+ interop-call'ов на 2 КБ ответа. Рендерить дельту на JS-стороне.
4. **Кэш `JsonSerializerOptions`** в `StateInspector` и `HumanInTheLoopInterrupter` (сейчас `new` на каждый рендер).
5. **Кэш HTML-разметки сообщений** в `SgChat` (см. critical bug 1.4) и `SgRagChat._htmlCache` (которая никогда не выселяется — LRU/eviction).
6. **Виртуализация** длинных списков: `SgRagVectorDbPanel` (collections + documents), `SgRagSearchPanel._results`, чаты.
7. **`SgLlmEmbedder` cosine matrix** — `O(N²)` пересчитывается на каждый рендер. Кэшировать после `RunAsync`.
8. **`SgOpenRouterModelDetails.GetModelsAsync`** — загрузка каталога моделей на каждый change `ModelId`. Кэшировать на уровне сервиса.
9. **`SgOllamaModelPicker.Filter`** — `m.Name.ToLower().Contains(query)` аллоцирует строку на элемент на keystroke. `IndexOf(query, StringComparison.OrdinalIgnoreCase)`.
10. **Lazy-mount табов**: `SgLlmStudio` рендерит все вкладки в одном render-pass; каждый вкладка стартует JS-импорт + event-подписки.
11. **`LangGraphVisualizer`** перерендеривает `SgDiagram` на каждый step, хотя меняется только `_currentNode`. Override `ShouldRender` либо параметризованный template узла.
12. **`SgRagChat._htmlCache` не сбрасывается на streaming-сообщении** — UI «откатывается» назад на следующий тик (см. critical bug в форме UX-баг).
13. **`StateInspector.JsonState`** сериализует на каждый рендер. Кэшировать по reference-eq `step.State` + cap depth/length.
14. **`SgRagService.OnStreamComplete/Token`** — провайдер должен сравнивать новый `ReadyState` с предыдущим record-equality, чтобы не дёргать `OnStateChanged` при равенстве.
15. **Streaming buffer churn** в LangGraph chat: `lastMsg.Text = step.Content` на каждый токен → full string allocation + полный рендер. `StringBuilder` + 30–60ms throttle.
16. **Base64 на UI-потоке** для больших файлов — заменить на `IJSStreamReference` / direct `byte[]` интероп (Blazor 8+).
17. **CancellationToken через `ILlmService`/`SgRagService`** — сейчас нигде не пробрасывается; перезагрузка страницы / переключение сессии не отменяет запрос → горение токенов.

---

## 5. План работ по приоритету

### Фаза 0 — оперативные блокеры (~1 день)

Безопасные, изолированные правки. Делать первыми.

- [ ] **F0-1** `SgLlmEmptyState`: добавить `@implements IDisposable`. 1 строка.
- [ ] **F0-2** `SgRagModelLoader.razor`: то же. 1 строка.
- [ ] **F0-3** `SgRagSaveLoadDb.razor`: то же + остановить timer в Dispose.
- [ ] **F0-4** `LangGraphProvider.DisposeAsync`: удалить `await LangGraphService.DisposeAsync()`. 1 строка.
- [ ] **F0-5** Заглушки checkpoint manager (`Save/Load/Reset/RestoreToStep`): либо реализовать, либо удалить кнопки + пометить компонент `[Experimental]`.
- [ ] **F0-6** Drop-zone в `SgRagDocumentUploader`: либо реализовать чтение `DragEventArgs.DataTransfer`, либо убрать визуальный affordance.
- [ ] **F0-7** `SgOllamaDashboard`, `SgOpenRouterDashboard`: заменить `@bind-Value` на `Value=`/`ValueChanged=` для `BaseUrl`/`ApiKey`.
- [ ] **F0-8** `SgPuterChat`: заменить выдуманный hardcoded model list на загрузку из сервиса (или сократить до известных ID).

### Фаза 1 — корректность (1–2 недели)

- [x] **F1-1** Подписки → `IDisposable`/`IAsyncDisposable` по всем 6 LangGraph-компонентам. *(коммит 62ba568)*
- [x] **F1-2** `async void` в `LangGraphProvider` → `async Task` с `InvokeAsync(...)` и try/catch. *(коммит 62ba568)*
- [x] **F1-3** `SgRagService` стриминг: `streamId` в JS callback'и + per-stream `Channel<string>` вместо multicast + busy-wait. *(коммит 3929a7c)*
- [x] **F1-4** `catch (Exception ex) { _error = ex.Message; … }` в `RunAsync` всех 5 компонентов `AI/Llm/` (`SgLlmReranker`, `SgLlmImageStudio`, `SgLlmSpeaker`, `SgLlmTranscriber`, `SgOllamaModelPicker.LoadModelsAsync`). Reranker и ModelPicker также получили поле `_error` + error-surface; ImageStudio.EditAsync тоже обёрнут.
- [x] **F1-5** `SgLlmImageStudio` + `SgLlmSpeaker`: реализовать `IAsyncDisposable` для `_blobModule` (по образцу `SgChat.razor.cs:416-442`).
- [x] **F1-6** `BlazorToolExecutor`: обернуть `OnToolCall.InvokeAsync` в try/catch, вернуть структурированный error-JSON в графовый сервис.
- [x] **F1-7** `SgRagSaveLoadDb._autoTimer.Elapsed`: убрать `async void`, обернуть try/catch, `InvokeAsync(StateHasChanged)`. Именованный handler `OnAutoSnapshotTimer`, отписка в Dispose.
- [x] **F1-8** `Enter`-handlers: добавить `e.IsComposing` guard (`SgChat`, `SgPuterChat`, `LangGraph/GraphStreamingChat`). `KeyboardEventArgs.IsComposing` доступен в .NET 9/10 — `@onkeydown:preventDefault` не понадобился.
- [x] **F1-9** `SgOpenRouterKeyInfo` / `SgOpenRouterModelDetails`: debounce 400ms (`DebounceMs` параметр) через `CancellationTokenSource` + `Task.Delay`, `IDisposable`, плюс `catch` на провал запроса.
- [x] **F1-10** `eval`-загрузки (`SgRagChat`×2, `SgRagSaveLoadDb`) → `downloadBlob(name, mime, base64)` в `sg-rag.js`. Бонус: `scrollToBottom(selector)` заменил третий `eval` в `SgRagChat`.
- [x] **F1-11** `LangGraphProvider`: ~~убрать дублирование `EventCallback` + `event Action`~~ — по факту это не дублирование, а два разных канала (razor-атрибут для родителя vs in-code подписка для детей). Решение (по согласованию): свести к `EventCallback`, удалить relay-слой `event Action` (`StepReceived`/`ErrorReceived`/`InitializedReceived`/`InterruptReceived`); 5 дочерних компонентов теперь подписываются напрямую на `Provider.Service.On*` с `InvokeAsync`-обёрткой.

### Фаза 2 — публичный контракт (3–5 дней)

- [ ] **F2-1** Переименовать LangGraph-компоненты: `SgLangGraphProvider`, `SgLangGraphChat`, `SgLangGraphVisualizer`, `SgLangGraphStateInspector`, `SgLangGraphCheckpointManager`, `SgLangGraphToolExecutor`, `SgLangGraphInterrupter`. Перенести под `Components/AI/Experimental/LangGraph/`. Пометить `[Experimental("SUPERUI_LANGGRAPH")]`.
- [ ] **F2-2** Сделать code-behind `*.razor.cs` для LangGraph-компонентов (`LangGraphProvider`, `GraphStreamingChat` минимум).
- [ ] **F2-3** XML-доки: проход по всем `[Parameter]` / событиям / public type'ам в `AI/Llm/` и `AI/Rag/`. Перепроверить, что CS1591 включено и считается за warning.
- [ ] **F2-4** Перенести инлайн `<style>` блоки LangGraph и `.razor.css` файлы `SgChat`/`SgPuterChat`/`SgRag*` в `wwwroot/superui-components.css`.
- [ ] **F2-5** Унифицировать namespace `AI/Llm/`: один `SuperUI.Components.Llm` для всего.
- [ ] **F2-6** Локализация: пропустить hardcoded ru-строки через `ISuperUILocalizer` (`SgLlmEmptyState`, `LangGraph/GraphStreamingChat`, `LangGraph/HumanInTheLoopInterrupter`).
- [ ] **F2-7** `SgRagProvider`: заменить `CascadingValue Name="RagService"` на типизированный `SgRagContext` record.

### Фаза 3 — UX (1 неделя)

- [ ] **F3-1** Cancel-кнопки + `CancellationToken` через `ILlmService` / `SgRagService` для всех долгих операций (search, embed, generate, transcribe, synthesize, image gen, video/vision analyze, rerank).
- [ ] **F3-2** Confirm-prompt через `SgConfirmHost` для деструктивных действий: `ClearCollection`, `DeleteDocument`, `Import(merge:false)`, `DeleteSession` (чат).
- [ ] **F3-3** Error-surface во всех `LoadXxx` без `catch`: `SgRagSaveLoadDb.LoadSnapshots`, `SgOllamaModelPicker.LoadModels`, `SgLlmModerator.Models` и т.д.
- [ ] **F3-4** Autoscroll в чатах: использовать захваченный `ref` + `scrollIntoView` на новое сообщение/токен.
- [ ] **F3-5** Виртуализация: `SgChat`/`SgPuterChat`/`SgRagChat` (`Virtualize` > 50 msg), `SgRagVectorDbPanel` (collections/documents), `SgRagSearchPanel._results`.
- [ ] **F3-6** Auto-resize textarea в `SgChat`/`SgPuterChat`/`LangGraph/GraphStreamingChat`.
- [ ] **F3-7** Модалка interrupter: Esc, focus-trap, `aria-modal`, очередь interrupt'ов вместо перезаписи.
- [ ] **F3-8** `StateInspector`: уважать CSS-переменные темы, JSON-tree вместо raw text, cap depth/length с «show all».
- [ ] **F3-9** Aria по `SgChat`: `role=listbox` для history, `aria-label` для всех иконок, `aria-pressed` для mode-toggle, throttle `aria-live`.
- [ ] **F3-10** `SgLlmImageStudio._qualities`: подобрать словарь по выбранной модели.
- [ ] **F3-11** `SgRagDocumentUploader`: показать ETA + bytes/sec.
- [ ] **F3-12** `SgRagChat._input`: очищать после успешного `await SendAsync`, не до.

### Фаза 4 — оптимизация (рекомендуемо, 1–2 недели)

- [ ] **F4-1** Throttle прогресс-событий до 10 Hz в `SgRagDocumentUploader`.
- [ ] **F4-2** Кэш HTML-encode для user-сообщений (`SgChat`) + LRU/eviction для `_htmlCache` в `SgRagChat`.
- [ ] **F4-3** Hoist `static readonly JsonSerializerOptions` в `StateInspector`, `HumanInTheLoopInterrupter`, `SgRagService`.
- [ ] **F4-4** `SgLlmEmbedder` cosine matrix: кэшировать после `RunAsync`.
- [ ] **F4-5** Сервис-уровневый кэш каталога моделей в `OpenRouterService` / `OllamaService`.
- [ ] **F4-6** `LangGraphVisualizer`: `ShouldRender` override; рендерить только при смене схемы; highlight через параметр узла, без перестроения графа.
- [ ] **F4-7** Streaming buffer в LangGraph chat: `StringBuilder` + 30–60ms throttle.
- [ ] **F4-8** `SgLlmStudio`: lazy-mount табов.
- [ ] **F4-9** Большие файлы → `IJSStreamReference` / прямой `byte[]` интероп вместо Base64 на UI-потоке (`SgLlmTranscriber/VisionAnalyzer/ImageStudio/VideoAnalyzer`, `SgRagService.IngestFile`, `SgRagChat` attachments).
- [ ] **F4-10** `SgLlmSettings.LoadModelsAsync`: switch → стратегии в `SgLlmProviderRegistry`.
- [ ] **F4-11** Кэш markdown-стриминга как дельты в `SgRagChat.RenderMarkdownAsync` (JS-side rendering).
- [ ] **F4-12** Аудитнуть hardcoded `BaseUrl = "https://openrouter.ai/api/v1"` в `SgChat._config` — пользователь без ключа упрётся в 401 по умолчанию.

### Фаза 5 — рефакторинг (опционально)

- [ ] **F5-1** Выделить `SgChatBase` для общей логики `SgChat` / `SgPuterChat` (markdown-рендер, blob-скачивание, image-URL safe-encoding, dispose).
- [ ] **F5-2** Разделить `SgRagService` на `SgRagModelManager` / `SgRagIndexer` / `SgRagSearcher` / `SgRagPersistence`.
- [ ] **F5-3** Завершить или удалить: `SgRagConversationMemory`, `SgRagFilterBuilder`, `SgRagAnalyticsPanel`.
- [ ] **F5-4** Объединить `SgPuterService` под зонтик `ILlmService` (`SgPuterProvider`) либо явно задокументировать исключение.
- [ ] **F5-5** Переиспользовать `SgLlmProviderPicker` / `SgOpenRouterModelPicker` внутри `SgRagChat` / `SgRagModelLoader`.

---

## 6. Top-5 quick wins (минимум усилий — максимум эффекта)

1. **F0-1..F0-4** — четыре однострочных правки убирают четыре реальных утечки/краша (LlmEmptyState, RagModelLoader, RagSaveLoadDb, LangGraphProvider.DisposeAsync).
2. **F1-3** — `Channel<string>` вместо multicast + busy-wait в `SgRagService` чинит race-condition и убирает 60 fps polling одним рефакторингом.
3. **F1-4** — пять `catch`-блоков в RunAsync'ах убирают «кнопка перестала крутиться» в половине `AI/Llm/`.
4. **F1-10** — заменить `eval`-загрузку на `downloadBlob` в `sg-rag.js` — закрывает CSP/XSS-форму одним помощником.
5. **F2-1 + F2-3** — переименование LangGraph-компонентов с префиксом `Sg` и проход XML-доков **до** следующего release-bump'а: избегаем major-bump.

---

## Приложение А. Дополнительные находки

- `SgRagChat.razor:149` и `SgRagChunkConfigurator.razor:68` — `Substring(0, Math.Min(len, 120))` может разорвать UTF-16 surrogate pair.
- `SgRagService.cs:708-713` — парсер snapshot'а не заполняет `CreatedAt` из JS-payload'а → UI всегда показывает «сейчас».
- `SgRagChat._messagesRef` не используется (`razor:283`); auto-scroll через `querySelector` ломается при нескольких чатах на странице.
- `SgChat.razor.cs:24` — `_messages` геттер возвращает новый `List<>()` при отсутствии сессии — потенциальный data-loss при дальнейших правках.
- `SgRagChat.RenderMarkdownAsync` может зацепить «JS-interop not available at this time» во время prerender — добавить guard.

## Приложение Б. Файлы, к которым возвращаться при работе

- `SuperUI/Components/AI/Llm/SgChat.razor.cs` — образец правильной обработки JS-модулей и Dispose. Использовать как референс при правке Image/Speaker.
- `SuperUI/Components/AI/Llm/README.md` — содержит явные anti-pattern'ы, которые сами же `SgPuterChat`/`SgLlmReranker` нарушают.
- `SuperUI/wwwroot/sg-rag.js` — добавить `downloadBlob(name, mime, base64)` и `streamId`-aware callback'и (F1-3, F1-10).
- `SuperUI/wwwroot/superui-components.css` — целевой файл для F2-4 (миграция всех `.razor.css` / inline `<style>`).
