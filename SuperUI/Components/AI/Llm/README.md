# SuperUI AI / LLM components

Все компоненты семейства `Sg*Llm*` опираются на единый `ILlmService` и единый
`SgLlmConfig`. Этот документ — короткий гайд для разработчика, который хочет
добавить **новый компонент-консумер** LLM (например, «Sg*ClassifierName*»),
повторно используя инфраструктуру каталога провайдеров, маршрутизации по
задачам, шапки и пустых состояний.

## Состав инфраструктуры

| Слой | Файл | Назначение |
|---|---|---|
| Сервис | `SuperUI/Services/Llm/SgLlmService.cs` | Реализует `ILlmService`. Знает обо всех провайдерах, моделях, эмбеддингах, изображениях, аудио, реранке и tool-calling. |
| Контракт | `SuperUI/Services/Llm/ILlmService.cs` | Публичный API сервиса + DTO (`SgLlmConfig`, `SgLlmChatRequest`, `SgLlmEmbedRequest`, …). |
| Каталог | `SuperUI/Services/Llm/SgLlmProviderRegistry.cs` | 40+ пресетов провайдеров, fallback-модели, normalizer Base URL, capabilities. |
| Настройки | `SuperUI/Components/AI/Llm/SgLlmSettings.razor(.cs)` | Главный экран конфигурации; используется в compact-режиме как поповер из шапки. |
| Шапка | `SuperUI/Components/AI/Llm/SgLlmHeader.razor` | Иконка задачи + статус + маршрут «chat → OpenAI · gpt-5.5» + кнопка ⚙. |
| Empty state | `SuperUI/Components/AI/Llm/SgLlmEmptyState.razor` | Подсказывает следующий шаг («введите ключ», «запустите Ollama», «попробуйте Backend proxy»). |
| Provider picker | `SuperUI/Components/AI/Llm/SgLlmProviderPicker.razor` | Каталог провайдеров с поиском, фильтрами категорий и быстрыми флагами. |
| Download helper | `SuperUI/wwwroot/sg-blob.js` | `downloadBase64`, `downloadBytes`, `downloadText`, `downloadUrl`. |

## Пошаговый рецепт: новый AI-компонент

1. **Файл.** Создайте `SuperUI/Components/AI/Llm/SgLlm<Name>.razor`.
2. **Шапка.** Вставьте `SgLlmHeader` первым элементом — он даёт UX-консистентность.

   ```razor
   <SgLlmHeader Title="Заголовок" Subtitle="Что делает компонент"
                TaskPurpose="@SgLlmTaskPurpose.Chat" />
   ```

   `TaskPurpose` определяет иконку, маршрут и какую модель `ResolveConfigForTask`
   выберет, если у пользователя настроены отдельные модели по задачам.

3. **Конфиг.** Получите эффективную конфигурацию через сервис:

   ```csharp
   var effective = LlmService.ResolveConfigForTask(TaskPurpose);
   await LlmService.InitializeAsync(effective);
   ```

   Так компонент будет автоматически использовать глобальную модель из
   `SgSettings`, а не локальную копию.

4. **Empty state.** Покажите `SgLlmEmptyState`, пока пользователь не отправил
   запрос:

   ```razor
   @if (_result == null && !_busy && string.IsNullOrEmpty(_error))
   {
       <SgLlmEmptyState Icon="📊" Title="Введите запрос"
                        Message="…" />
   }
   ```

5. **Ошибки.** Стандарт — `SgAlert Variant="SgAlertVariant.Danger"` сразу под
   формой запроса. Не глотайте исключения молча.

6. **Скачивание артефактов.** Если компонент возвращает аудио / изображение /
   текст, используйте `sg-blob.js`:

   ```csharp
   private IJSObjectReference? _blobModule;
   private async Task<IJSObjectReference> EnsureBlobModuleAsync()
       => _blobModule ??= await JS.InvokeAsync<IJSObjectReference>(
              "import", "./_content/SuperUI/sg-blob.js");

   var blob = await EnsureBlobModuleAsync();
   await blob.InvokeVoidAsync("downloadBase64", base64, mime, fileName);
   ```

   Не вызывайте `JS.InvokeVoidAsync("eval", ...)` — это XSS-риск.

7. **Подписка на смену конфига.** Если компонент держит локальную копию (как
   `SgDocumentExtractor`), подпишитесь на `LlmService.OnConfigChanged` и не
   забудьте отписаться в `Dispose/DisposeAsync`. Сравнивайте по fingerprint
   (`Provider/Model/Url/Key`), чтобы избежать петель.

8. **Валидация пользовательского ввода.** Если у компонента есть JSON Schema
   или другой формат — валидируйте до отправки (см. `SgLlmStructuredOutput`,
   `SgLlmToolPlayground`). Кнопку «Отправить» блокируйте на невалидных данных.

9. **CSS.** Если правил мало — оставляйте `<style>` в `.razor`. Если много или
   планируете переиспользовать — выносите в bundled `superui-components.css`.

## TaskPurpose

Поддерживаемые значения (`SgLlmTaskPurpose`):

| Значение | Иконка | Когда применять |
|---|---|---|
| `Chat` | 💬 | универсальный чат / completion |
| `Documents` | 📄 | OCR, extraction, RAG-ингест |
| `Vision` | 👁 | анализ изображений |
| `Structured` | 📐 | json-schema, function calling |
| `Embeddings` | 🧬 | векторизация |
| `Rerank` | 📊 | сортировка по релевантности |
| `Images` | 🖼 | text-to-image, edit |
| `Moderation` | 🛡 | safety checks |
| `Speech` | 🎙 | TTS / STT |
| `Video` | 🎬 | видео-анализ |

## Добавление нового провайдера

1. Добавьте значение в `enum SgLlmProvider` **в конец** (LocalStorage-конфиги
   завязаны на порядок).
2. Зарегистрируйте `SgLlmProviderPreset` в `SgLlmProviderRegistry._presets`
   (метаданные: Category, Tags, Icon, Auth, ApiStyle, BaseUrl, ApiKeyUrl,
   DocsUrl, Notes, FreeTierNotes).
3. Добавьте провайдера в `AllowedProviders`.
4. Если endpoint не стандартный — расширьте `NormalizeBaseUrl` и
   `DetectProvider`.
5. Добавьте fallback-модели в `FallbackModels(provider)` (3-6 моделей с
   capability-флагами).
6. Если есть `/v1/models` — добавьте загрузчик в `SgLlmService` и подключите
   в `LoadModelsAsync` switch.
7. Для локальных провайдеров — обновите `LocalCorsHint` в
   `SgLlmSettings.razor.cs`.

## Тестирование

- Юнит-тесты — `bUnit`. Мокайте `ILlmService` и проверяйте, что компонент
  правильно реагирует на `OnConfigChanged`, ошибки и пустые ответы.
- Манипуляции с DOM (типа `JS.InvokeAsync("import", ...)` для sg-blob) в
  bUnit заглушаются `JSInterop.SetupVoid("import", ...)`.

## Антипаттерны

- ❌ Локальная копия `_llmConfig` без синхронизации с
  `LlmService.OnConfigChanged`.
- ❌ `JS.InvokeVoidAsync("eval", "...")`.
- ❌ Прямые HTTP-запросы к провайдеру минуя `SgLlmService` (теряем backend
  proxy, retries, обработку ошибок).
- ❌ Хардкод `BaseUrl` или модели в коде компонента — всегда через `Config`.
- ❌ Игнорирование `_busy` / `_error` — пользователь должен видеть состояние.
