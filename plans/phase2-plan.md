# Фаза 2 — Публичный контракт (план реализации)

Источник: `plans/ai-components-audit.md` §5 Фаза 2. Порядок и решения согласованы с пользователем.

**Порядок коммитов:** сначала breaking-API (F2-1, F2-5, F2-7), потом docs/css/l10n (F2-3, F2-4, F2-2, F2-6).
**Проверка после каждого коммита:** `dotnet build SuperUI` и `dotnet build SuperUI.Demo` = 0 ошибок.

---

## Коммит 1 — F2-1: rename + перенос LangGraph + [Experimental]

7 компонентов `SuperUI/Components/AI/LangGraph/` → `SuperUI/Components/AI/Experimental/LangGraph/`, с префиксом `Sg` и атрибутом `[Experimental("SUPERUI_LANGGRAPH")]`.

| Старое имя | Новое имя |
|---|---|
| `LangGraphProvider` | `SgLangGraphProvider` |
| `GraphStreamingChat` | `SgLangGraphChat` |
| `LangGraphVisualizer` | `SgLangGraphVisualizer` |
| `StateInspector` | `SgLangGraphStateInspector` |
| `AgentCheckpointManager` | `SgLangGraphCheckpointManager` |
| `BlazorToolExecutor` | `SgLangGraphToolExecutor` |
| `HumanInTheLoopInterrupter` | `SgLangGraphInterrupter` |

Детали:
- `@namespace` всех файлов: `SuperUI.Components.AI.LangGraph` → `SuperUI.Components.AI.Experimental.LangGraph`.
- `[Experimental("SUPERUI_LANGGRAPH")]`: razor-компоненты помечаются через атрибут на partial-классе в `*.razor.cs`. Для тех, у кого ещё нет code-behind, добавить минимальный `*.razor.cs` с `[Experimental]` на `public partial class`. (Это частично пересекается с F2-2.)
- Внутренние перекрёстные ссылки: `[CascadingParameter] LangGraphProvider` → `SgLangGraphProvider`; `Provider.Service.On*` остаётся (сервис не переименовывается). `BlazorToolExecutor.ToolCallEventArgs` → `SgLangGraphToolExecutor.ToolCallEventArgs`.
- Демо `SuperUI.Demo/Components/Pages/LangGraphDemo.razor`: обновить `@using`, все теги, `HandleToolCall(BlazorToolExecutor.ToolCallEventArgs)` → `SgLangGraphToolExecutor.ToolCallEventArgs`. Чтобы демо компилировалось при `[Experimental]`, добавить `#pragma warning disable SUPERUI_LANGGRAPH` в начало страницы (демо осознанно использует экспериментальный API).
- Сервисы `SgLangGraphService`, `SgLangGraphConfig`, `SgLangGraphStep` и т.д. — НЕ трогаем (уже с префиксом, в `SuperUI/Services/AI/`).

## Коммит 2 — F2-5: унификация namespace AI/Llm

Сейчас раскол: `SgChat`, `SgPuterChat`, `SgOllama*`, `SgOpenRouter*` → `SuperUI.Components.Llm`; остальные `SgLlm*` → `SuperUI.Components`.

- Привести **все** `AI/Llm/` к одному namespace `SuperUI.Components.Llm`.
- `SuperUI/_Imports.razor` уже содержит `@using SuperUI.Components.Llm` — компоненты в этом namespace продолжат резолвиться.
- Компоненты, переезжающие из `SuperUI.Components` в `SuperUI.Components.Llm`: проверить, что их кросс-ссылки (`SgLlmHeader`, `SgLlmEmptyState` и пр. внутри других `SgLlm*`) резолвятся — все в одном namespace, ок.
- **Риск breaking:** потребители, писавшие `using SuperUI.Components;` и обращавшиеся к `SgLlmReranker` и т.п., теперь должны добавить `using SuperUI.Components.Llm;`. Поскольку `_Imports` библиотеки это покрывает для razor-потребителей, а демо использует те же импорты — основной риск у внешних C#-потребителей. Документировать в release notes.

## Коммит 3 — F2-7: типизированный SgRagContext

`SgRagProvider` сейчас отдаёт два именованных каскада: `Name="RagService"` (SgRagService) и `Name="RagReadyState"`.

- Ввести `public record SgRagContext(SgRagService Service, bool IsReady)` (или с полем ready-state).
- `SgRagProvider` отдаёт `<CascadingValue Value="_context">` (типизированный, без Name).
- 7 потребителей (`SgRagChat`, `SgRagSearchPanel`, `SgRagVectorDbPanel`, `SgRagDocumentUploader`, `SgRagChunkConfigurator`, `SgRagSaveLoadDb`, `SgRagModelLoader`): `[CascadingParameter(Name="RagService")] SgRagService? _ragService` → `[CascadingParameter] SgRagContext? _ragContext`, обращения `_ragService.X` → `_ragContext?.Service.X`.
- **Совместимость:** сохранить старые именованные каскады параллельно ОДИН минор как `[Obsolete]`-путь? Решить при реализации; по умолчанию — заменить (компоненты внутренние, каскад — деталь реализации провайдера, наружу не торчит как публичный контракт).

## Коммит 4 — F2-3: XML-доки + включить CS1591 (вся библиотека)

⚠️ **Объём: 7218 предупреждений CS1591** при снятии `NoWarn`. Распределение по семьям:
Other 1156, Data 500, Maps 332, AI 280, HttpApiTester 252, Overlays 248, Charts 180, DocumentExtractor 176, Layout 166, Network 116, Forms 108, Navigation 94, Display 62, Feedback 6.

Это очень крупная механическая работа на тысячи строк. План — поэтапно, по семьям компонентов, каждая семья = отдельный коммит:
1. Снять `<NoWarn>$(NoWarn);CS1591</NoWarn>` в `SuperUI.csproj` (или оставить до конца, чтобы не ломать CI промежуточно — решить).
2. Идти семья за семьёй, добавляя `/// <summary>` к публичным типам, `[Parameter]`, событиям, enum-значениям, публичным методам/свойствам.
3. После каждой семьи — сборка с 0 CS1591 в ней.
4. В конце — убедиться, что вся библиотека собирается без CS1591, обновить CLAUDE.md (там сейчас неверно сказано «1591 enabled»).

**Рекомендация:** учитывая объём, делать F2-3 отдельной серией сессий после остальных пунктов Фазы 2. Начать с AI (280) как продолжение аудита, затем остальные семьи.

## Коммит 5 — F2-4: миграция CSS

Перенести в `wwwroot/superui-components.css`:
- Inline `<style>` из 5 LangGraph-файлов (теперь Experimental/).
- `.razor.css`: `SgChat`, `SgPuterChat`, `SgRagChat`, `SgRagChunkConfigurator`, `SgRagDocumentUploader`, `SgRagModelLoader`, `SgRagSaveLoadDb`, `SgRagSearchPanel`, `SgRagVectorDbPanel`.
- Удалить `.razor.css` файлы после переноса (библиотека шипит один бандл по дизайну).
- Проверить отсутствие коллизий классов; при необходимости — префикс.

## Коммит 6 — F2-2: code-behind LangGraph

Вынести инлайн-`@code` в `*.razor.cs` для LangGraph-компонентов (минимум Provider и Chat). Частично уже сделано в F2-1 (там, где добавляли `[Experimental]` через code-behind).

## Коммит 7 — F2-6: локализация hardcoded ru-строк

Через `ISuperUILocalizer`:
- `SgLlmEmptyState` (строки :67, 76-80, 86-88, 96).
- LangGraph chat (`«Введите сообщение…»`, `«Агент думает»`).
- LangGraph interrupter (`«Требуется одобрение»`).
- Добавить ключи в ru-RU и en-US ресурсы.

---

## Открытый вопрос к пользователю

F2-3 — это **7218** доков. Подтвердить, что делаем всю библиотеку (а не только AI ~280), и что готовы к серии коммитов/сессий под это. До подтверждения — выполняю Коммиты 1-3 (breaking-API) + 5-7 (css/code-behind/l10n), а F2-3 оставляю последним крупным заходом.
