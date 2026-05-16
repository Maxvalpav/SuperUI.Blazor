# 📋 Blazor Document Extractor Component — План для агента

> **Цель:** Создать универсальный Blazor-компонент, который загружает PDF/Word/изображения, отправляет их в OpenAI API (vision/file-capable модели), автоматически генерирует JSON-схему и форму ввода, позволяет редактировать данные и экспортировать обратно в PDF/Word.

---

## 🗺️ Оглавление

1. [Архитектура проекта](#1-архитектура-проекта)
2. [Стек технологий и зависимости](#2-стек-технологий-и-зависимости)
3. [Структура файлов](#3-структура-файлов)
4. [Пошаговый план реализации](#4-пошаговый-план-реализации)
5. [Детальные инструкции по каждому компоненту](#5-детальные-инструкции-по-каждому-компоненту)
6. [Промпты для OpenAI](#6-промпты-для-openai)
7. [JSON Schema структура](#7-json-schema-структура)
8. [Инструкции для агента (шаг за шагом)](#8-инструкции-для-агента-шаг-за-шагом)
9. [Примеры кода](#9-примеры-кода)
10. [Тест-кейсы](#10-тест-кейсы)

---

## 1. Архитектура проекта

```
┌─────────────────────────────────────────────────────────────────┐
│                    DocumentExtractor Component                   │
│                                                                 │
│  ┌──────────────┐   ┌──────────────┐   ┌─────────────────────┐ │
│  │  File Upload │──▶│  OpenAI API  │──▶│  Schema Generator   │ │
│  │  (PDF/Word/  │   │  (GPT-4o /   │   │  (JSON Schema +     │ │
│  │   Images)    │   │  vision)     │   │   Form Builder)     │ │
│  └──────────────┘   └──────────────┘   └─────────────────────┘ │
│                                                  │              │
│  ┌──────────────────────────────────────────────▼────────────┐ │
│  │                   Dynamic Form Renderer                    │ │
│  │  (text, number, date, select, checkbox, table fields)     │ │
│  └────────────────────────────────────────────────────────────┘ │
│                              │                                   │
│  ┌───────────────────────────▼──────────────────────────────┐  │
│  │                   Export Engine                           │  │
│  │              PDF (iTextSharp / QuestPDF)                  │  │
│  │              Word (DocumentFormat.OpenXml)                │  │
│  └───────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### Поток данных

```
Файл(ы) → Base64/ByteArray → OpenAI API
                                  │
                    JSON Schema + Extracted Data
                                  │
                         Dynamic Form Builder
                                  │
                         User Edits Data
                                  │
                    Export: PDF / Word (same layout)
```

---

## 2. Стек технологий и зависимости

### NuGet пакеты

```xml
<!-- В файл .csproj -->

<!-- PDF генерация -->
<PackageReference Include="QuestPDF" Version="2024.*" />
<!-- ИЛИ альтернатива: -->
<PackageReference Include="iTextSharp.LGPLv2.Core" Version="3.*" />

<!-- Word генерация -->
<PackageReference Include="DocumentFormat.OpenXml" Version="3.*" />

<!-- PDF парсинг (для извлечения текста) -->
<PackageReference Include="PdfPig" Version="0.1.*" />
<!-- ИЛИ -->
<PackageReference Include="iText7" Version="8.*" />

<!-- Word парсинг -->
<PackageReference Include="DocumentFormat.OpenXml" Version="3.*" />

<!-- HTTP клиент для OpenAI -->
<PackageReference Include="OpenAI" Version="2.*" />
<!-- ИЛИ прямые HTTP вызовы через HttpClient -->

<!-- JSON Schema -->
<PackageReference Include="NJsonSchema" Version="11.*" />

<!-- Blazor компоненты -->
<PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="8.*" />

<!-- Файловый диалог -->
<PackageReference Include="BlazorInputFile" Version="0.2.*" />
```

### JavaScript зависимости (в wwwroot/index.html или _Host.cshtml)

```html
<!-- Drag & Drop и File Preview -->
<script src="https://cdnjs.cloudflare.com/ajax/libs/pdf.js/4.0.379/pdf.min.js"></script>
```

---

## 3. Структура файлов

```
📁 YourProject/
├── 📁 Components/
│   └── 📁 DocumentExtractor/
│       ├── 📄 DocumentExtractor.razor          ← Главный компонент
│       ├── 📄 DocumentExtractor.razor.cs       ← Code-behind
│       ├── 📄 DocumentExtractor.razor.css      ← Изолированные стили
│       │
│       ├── 📁 SubComponents/
│       │   ├── 📄 FileUploadZone.razor         ← Drag & Drop загрузка
│       │   ├── 📄 FilePreview.razor            ← Превью загруженных файлов
│       │   ├── 📄 SettingsPanel.razor          ← Настройки OpenAI
│       │   ├── 📄 DynamicFormRenderer.razor    ← Рендер формы по схеме
│       │   ├── 📄 FieldComponents/
│       │   │   ├── 📄 TextField.razor
│       │   │   ├── 📄 NumberField.razor
│       │   │   ├── 📄 DateField.razor
│       │   │   ├── 📄 SelectField.razor
│       │   │   ├── 📄 TableField.razor         ← Для таблиц из документов
│       │   │   ├── 📄 CheckboxField.razor
│       │   │   └── 📄 TextAreaField.razor
│       │   └── 📄 ExportPanel.razor            ← Кнопки экспорта
│       │
│       └── 📁 Models/
│           ├── 📄 DocumentSchema.cs            ← JSON Schema модель
│           ├── 📄 FieldDefinition.cs           ← Определение поля
│           ├── 📄 ExtractedData.cs             ← Извлечённые данные
│           ├── 📄 OpenAiSettings.cs            ← Настройки API
│           └── 📄 ExportOptions.cs             ← Опции экспорта
│
├── 📁 Services/
│   ├── 📄 IDocumentParserService.cs
│   ├── 📄 DocumentParserService.cs            ← Парсинг PDF/Word → текст/base64
│   ├── 📄 IOpenAiService.cs
│   ├── 📄 OpenAiService.cs                    ← Вызовы OpenAI API
│   ├── 📄 ISchemaGeneratorService.cs
│   ├── 📄 SchemaGeneratorService.cs           ← Генерация JSON Schema
│   ├── 📄 IPdfExportService.cs
│   ├── 📄 PdfExportService.cs                 ← Экспорт в PDF
│   ├── 📄 IWordExportService.cs
│   └── 📄 WordExportService.cs                ← Экспорт в Word
│
├── 📁 wwwroot/
│   └── 📁 js/
│       └── 📄 documentExtractor.js            ← JS interop
│
└── 📄 Program.cs                              ← DI регистрация
```

---

## 4. Пошаговый план реализации

```
ЭТАП 1: Подготовка инфраструктуры          [~2 часа]
  └── Шаг 1.1: Создать проект / настроить NuGet
  └── Шаг 1.2: Создать модели данных
  └── Шаг 1.3: Зарегистрировать сервисы в DI

ЭТАП 2: Парсинг документов                 [~3 часа]
  └── Шаг 2.1: PDF → текст + изображения страниц
  └── Шаг 2.2: Word → текст + embedded изображения
  └── Шаг 2.3: Изображения → base64

ЭТАП 3: OpenAI интеграция                  [~3 часа]
  └── Шаг 3.1: SettingsPanel (API Key, модель, endpoint)
  └── Шаг 3.2: Отправка файлов в OpenAI (vision/files)
  └── Шаг 3.3: Парсинг ответа → JSON Schema

ЭТАП 4: Динамическая форма                 [~4 часа]
  └── Шаг 4.1: SchemaGeneratorService
  └── Шаг 4.2: DynamicFormRenderer
  └── Шаг 4.3: Все типы полей (text, number, date, select, table)
  └── Шаг 4.4: Валидация и биндинг данных

ЭТАП 5: Экспорт                            [~4 часа]
  └── Шаг 5.1: PDF экспорт (QuestPDF)
  └── Шаг 5.2: Word экспорт (OpenXML)
  └── Шаг 5.3: Сохранение оригинального форматирования

ЭТАП 6: UI/UX                              [~2 часа]
  └── Шаг 6.1: Главный компонент DocumentExtractor
  └── Шаг 6.2: FileUploadZone (drag & drop)
  └── Шаг 6.3: FilePreview
  └── Шаг 6.4: Прогресс и статусы

ЭТАП 7: Тестирование                       [~2 часа]
  └── Шаг 7.1: Unit тесты сервисов
  └── Шаг 7.2: Интеграционные тесты
  └── Шаг 7.3: E2E тест полного флоу
```

---

## 5. Детальные инструкции по каждому компоненту

---

### 5.1 Модели данных

#### `OpenAiSettings.cs`
```csharp
public class OpenAiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string Model { get; set; } = "gpt-4o";          // модель с поддержкой файлов
    public double Temperature { get; set; } = 0.1;          // низкая для точности
    public int MaxTokens { get; set; } = 4096;
    public string? SystemPrompt { get; set; }               // кастомный промпт
    public bool UseFileApi { get; set; } = false;           // Files API vs vision
    
    // Список поддерживаемых моделей
    public static readonly string[] SupportedModels = 
    {
        "gpt-4o",
        "gpt-4o-mini",
        "gpt-4-turbo",
        "gpt-4-vision-preview",
        "o1",
        "o1-mini"
    };
}
```

#### `FieldDefinition.cs`
```csharp
public enum FieldType
{
    Text, TextArea, Number, Integer,
    Date, DateTime, Boolean,
    Select, MultiSelect,
    Table, Image, Address, Phone, Email
}

public class FieldDefinition
{
    public string Key { get; set; } = string.Empty;      // уникальный ключ
    public string Label { get; set; } = string.Empty;    // метка поля
    public FieldType Type { get; set; }
    public bool Required { get; set; }
    public object? DefaultValue { get; set; }
    public string? Placeholder { get; set; }
    public string? Description { get; set; }
    public int? Order { get; set; }                       // порядок в форме
    public string? Group { get; set; }                    // группа/секция
    
    // Для Select полей
    public List<SelectOption>? Options { get; set; }
    
    // Для Number полей
    public double? Min { get; set; }
    public double? Max { get; set; }
    public string? Format { get; set; }                   // "currency", "percent"
    
    // Для Table полей
    public List<FieldDefinition>? Columns { get; set; }
    
    // Для Text/TextArea
    public int? MaxLength { get; set; }
    public string? Pattern { get; set; }                  // regex паттерн
    
    // Метаданные
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SelectOption
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
```

#### `DocumentSchema.cs`
```csharp
public class DocumentSchema
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DocumentType { get; set; } = string.Empty;  // "invoice", "contract", etc.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public List<FieldDefinition> Fields { get; set; } = new();
    public List<FormSection> Sections { get; set; } = new();   // логические секции
    
    // Оригинальный JSON Schema от OpenAI
    public string? RawJsonSchema { get; set; }
    
    // Метаданные документа
    public Dictionary<string, string> DocumentMetadata { get; set; } = new();
}

public class FormSection
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string> FieldKeys { get; set; } = new();  // ссылки на FieldDefinition.Key
    public int Order { get; set; }
    public bool Collapsible { get; set; } = false;
}
```

#### `ExtractedData.cs`
```csharp
public class ExtractedData
{
    public string SchemaId { get; set; } = string.Empty;
    public DocumentSchema Schema { get; set; } = new();
    
    // Данные: ключ → значение
    public Dictionary<string, object?> Values { get; set; } = new();
    
    // Оригинальные файлы для сохранения форматирования
    public List<UploadedFile> SourceFiles { get; set; } = new();
    
    // Информация об экспорте
    public ExportTemplate? Template { get; set; }
    
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
    public bool IsModified { get; set; } = false;
}

public class UploadedFile
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public long Size { get; set; }
    public FileCategory Category { get; set; }
    
    // Base64 для отправки в OpenAI
    public string Base64Content => Convert.ToBase64String(Content);
}

public enum FileCategory { Pdf, Word, Image, Unknown }

public class ExportTemplate
{
    public ExportFormat Format { get; set; }
    public string? TemplateFileId { get; set; }  // ID исходного файла-шаблона
    public Dictionary<string, string> StyleMap { get; set; } = new();
}

public enum ExportFormat { Pdf, Word, Both }
```

---

### 5.2 DocumentParserService

**Задача:** Принять файл, вернуть текст + массив изображений страниц (для vision).

```
ИНСТРУКЦИИ ДЛЯ АГЕНТА:

1. Для PDF файлов:
   - Использовать PdfPig для извлечения текста
   - Рендерить каждую страницу в изображение через PDFium (или использовать
     внешнюю библиотеку Docnet.Core) для vision-запросов
   - Ограничить количество страниц (например, первые 10) во избежание превышения лимитов API

2. Для Word (.docx) файлов:
   - Использовать DocumentFormat.OpenXml для извлечения текста
   - Извлечь embedded изображения из DocumentFormat.OpenXml WordprocessingDocument
   - Если нет встроенного рендерера Word→Image, использовать LibreOffice CLI как fallback
     через Process.Start("soffice", "--headless --convert-to pdf file.docx")
     и затем конвертировать PDF в изображения

3. Для изображений (.jpg, .png, .webp, .gif):
   - Напрямую конвертировать в base64
   - Проверить размер (OpenAI vision: макс. 20MB на изображение)
   - При необходимости уменьшить разрешение через SixLabors.ImageSharp

4. Интерфейс:
   Task<ParsedDocument> ParseAsync(UploadedFile file);

5. ParsedDocument должен содержать:
   - string ExtractedText — весь текст
   - List<PageImage> Pages — изображения страниц в base64
   - DocumentMetadata Metadata — автор, дата, кол-во страниц
```

---

### 5.3 OpenAiService

**Задача:** Отправить документ в OpenAI и получить JSON Schema + данные.

```
ИНСТРУКЦИИ ДЛЯ АГЕНТА:

СТРАТЕГИЯ ВЫЗОВА API:

Вариант A (Vision/Multimodal - рекомендуется для PDF/изображений):
  - Метод: Chat Completions API с image_url (base64)
  - Endpoint: POST /v1/chat/completions
  - Модели: gpt-4o, gpt-4o-mini, gpt-4-turbo
  - Формат: messages с type "image_url" для каждой страницы

Вариант B (Files API - для больших документов):
  - Шаг 1: POST /v1/files (загрузить файл)
  - Шаг 2: POST /v1/chat/completions с file_id
  - Поддерживает .pdf напрямую в gpt-4o (Responses API)
  - Endpoint: POST /v1/responses (новый Responses API)

РЕАЛИЗАЦИЯ:

public interface IOpenAiService
{
    Task<SchemaExtractionResult> ExtractSchemaAsync(
        List<UploadedFile> files,
        OpenAiSettings settings,
        IProgress<string>? progress = null);
    
    Task<bool> ValidateApiKeyAsync(OpenAiSettings settings);
    Task<List<string>> GetAvailableModelsAsync(OpenAiSettings settings);
}

ЗАПРОС К API (Вариант A):
{
  "model": "gpt-4o",
  "messages": [
    {
      "role": "system",
      "content": "[SYSTEM PROMPT - см. раздел 6]"
    },
    {
      "role": "user",
      "content": [
        {
          "type": "text",
          "text": "Проанализируй документ и верни JSON Schema"
        },
        {
          "type": "image_url",
          "image_url": {
            "url": "data:image/png;base64,{base64_page_1}",
            "detail": "high"
          }
        }
        // ... остальные страницы
      ]
    }
  ],
  "response_format": { "type": "json_object" },
  "temperature": 0.1,
  "max_tokens": 4096
}

ОБРАБОТКА ОШИБОК:
- 429 Rate Limit: exponential backoff, retry 3 раза
- 413 Too Large: автоматически уменьшить изображения
- 401 Unauthorized: вернуть понятное сообщение пользователю
- timeout: 120 секунд для больших документов
```

---

### 5.4 DynamicFormRenderer

**Задача:** По JSON Schema сгенерировать интерактивную форму.

```
ИНСТРУКЦИИ ДЛЯ АГЕНТА:

1. Компонент принимает:
   @Parameter DocumentSchema Schema
   @Parameter Dictionary<string, object?> Values
   @Parameter EventCallback<Dictionary<string, object?>> OnValuesChanged

2. Логика рендеринга:
   - Итерировать по Schema.Sections (если есть) или Schema.Fields
   - Для каждого поля выбрать нужный FieldComponent по FieldType
   - Использовать RenderFragment и динамический switch

3. Типы полей и их компоненты:
   Text       → <input type="text">
   TextArea   → <textarea>
   Number     → <input type="number" step="0.01">
   Integer    → <input type="number" step="1">
   Date       → <input type="date">
   DateTime   → <input type="datetime-local">
   Boolean    → <input type="checkbox"> или toggle
   Select     → <select> с options из FieldDefinition.Options
   MultiSelect→ множественный select или чекбокс-группа
   Table      → динамическая таблица с add/remove строк
   Address    → составное поле (улица, город, индекс, страна)
   Phone      → input с маской
   Email      → <input type="email">

4. TableField — особый компонент:
   - Колонки определяются из FieldDefinition.Columns
   - Каждая строка = словарь ключ→значение
   - Кнопки "Добавить строку" / "Удалить строку"
   - Поддержка сортировки строк

5. Валидация:
   - Required поля — подсветка при пустом значении
   - Pattern — regex проверка
   - Min/Max — для числовых полей
   - Показывать ошибки инлайн под полем

6. Горячие клавиши:
   - Tab — переход между полями
   - Enter в числовых полях — переход к следующему
```

---

### 5.5 ExportService

```
ИНСТРУКЦИИ ДЛЯ АГЕНТА:

PDF ЭКСПОРТ (QuestPDF):

1. Стратегия A — Template-based (если есть оригинальный PDF):
   - Загрузить оригинальный PDF как фон/шаблон
   - Наложить данные из формы поверх (fill form fields если PDF форма)
   - Использовать iText7 для работы с AcroForm полями

2. Стратегия B — Structured (генерация нового PDF):
   - Использовать QuestPDF Document DSL
   - Сгенерировать структуру на основе Schema.Sections
   - Применить стили, соответствующие оригиналу (заголовки, таблицы)

QuestPDF пример структуры:
Document.Create(container =>
{
    container.Page(page =>
    {
        page.Header().Element(ComposeHeader);
        page.Content().Element(ComposeContent);
        page.Footer().Element(ComposeFooter);
    });
});

WORD ЭКСПОРТ (OpenXML):

1. Стратегия A — Template fill:
   - Открыть оригинальный .docx как шаблон
   - Найти Content Controls или bookmark placeholder'ы
   - Заменить данными из формы

2. Стратегия B — Generate:
   - Создать новый WordprocessingDocument
   - Добавить параграфы по Schema.Sections
   - Воспроизвести таблицы через Table/TableRow/TableCell

СКАЧИВАНИЕ ФАЙЛА (Blazor Server):
- Использовать JS Interop для триггера download
- IJSRuntime.InvokeVoidAsync("downloadFile", fileName, base64Content, mimeType)

JS функция в wwwroot/js/documentExtractor.js:
function downloadFile(fileName, base64Content, mimeType) {
    const byteCharacters = atob(base64Content);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
}
```

---

## 6. Промпты для OpenAI

### System Prompt (основной)

```
You are a document analysis expert. Your task is to:
1. Analyze the provided document (PDF pages as images or extracted text)
2. Identify ALL data fields present in the document
3. Return a structured JSON response with two parts:
   a) A JSON Schema describing all fields
   b) The actual data extracted from the document

CRITICAL RULES:
- Return ONLY valid JSON, no markdown, no explanations
- Detect the document language and use it for field labels
- Preserve all data exactly as shown — numbers, dates, names
- For tables: create a "table" type field with column definitions
- Group related fields into logical sections
- Infer field types from content (dates, numbers, emails, phones)
- If a field has a fixed set of values (e.g., status, country), make it "select" type

RESPONSE FORMAT:
{
  "documentType": "invoice|contract|form|report|other",
  "documentTitle": "Human readable title",
  "language": "ru|en|de|...",
  "sections": [
    {
      "key": "section_key",
      "title": "Section Title",
      "order": 1,
      "fieldKeys": ["field1", "field2"]
    }
  ],
  "fields": [
    {
      "key": "unique_snake_case_key",
      "label": "Human readable label",
      "type": "text|textarea|number|integer|date|datetime|boolean|select|multiselect|table|email|phone|address",
      "required": true|false,
      "value": <extracted value>,
      "options": [{"value": "v", "label": "l"}],  // only for select types
      "columns": [...],  // only for table type
      "section": "section_key",
      "order": 1,
      "description": "optional hint",
      "format": "currency|percent|integer"  // optional
    }
  ],
  "extractedData": {
    "field_key": <value>
  }
}
```

### User Prompt (для каждого запроса)

```
Analyze this document and extract all data fields.
Document consists of {pageCount} page(s).
{if text extracted: "Extracted text for reference: {extractedText}"}

Please identify:
1. Document type and title
2. All form fields, labels, and their values
3. Tables with headers and data rows
4. Dates, numbers, names, addresses, etc.
5. Any checkboxes or selections and their state

Return the complete JSON schema and extracted data.
```

---

## 7. JSON Schema структура

### Пример для счёта-фактуры (Invoice)

```json
{
  "documentType": "invoice",
  "documentTitle": "Счёт-фактура",
  "language": "ru",
  "sections": [
    {
      "key": "seller_info",
      "title": "Информация о продавце",
      "order": 1,
      "fieldKeys": ["seller_name", "seller_inn", "seller_address"]
    },
    {
      "key": "buyer_info",
      "title": "Информация о покупателе",
      "order": 2,
      "fieldKeys": ["buyer_name", "buyer_inn", "buyer_address"]
    },
    {
      "key": "invoice_details",
      "title": "Реквизиты",
      "order": 3,
      "fieldKeys": ["invoice_number", "invoice_date", "payment_date"]
    },
    {
      "key": "items",
      "title": "Товары и услуги",
      "order": 4,
      "fieldKeys": ["line_items"]
    },
    {
      "key": "totals",
      "title": "Итого",
      "order": 5,
      "fieldKeys": ["subtotal", "vat_rate", "vat_amount", "total"]
    }
  ],
  "fields": [
    {
      "key": "seller_name",
      "label": "Продавец",
      "type": "text",
      "required": true,
      "value": "ООО Ромашка",
      "section": "seller_info",
      "order": 1
    },
    {
      "key": "invoice_date",
      "label": "Дата счёта",
      "type": "date",
      "required": true,
      "value": "2024-01-15",
      "section": "invoice_details",
      "order": 1
    },
    {
      "key": "line_items",
      "label": "Позиции",
      "type": "table",
      "required": true,
      "section": "items",
      "columns": [
        {"key": "name", "label": "Наименование", "type": "text"},
        {"key": "qty", "label": "Кол-во", "type": "number"},
        {"key": "unit", "label": "Ед.изм.", "type": "text"},
        {"key": "price", "label": "Цена", "type": "number", "format": "currency"},
        {"key": "amount", "label": "Сумма", "type": "number", "format": "currency"}
      ],
      "value": [
        {"name": "Услуга А", "qty": 1, "unit": "шт", "price": 1000, "amount": 1000}
      ]
    },
    {
      "key": "total",
      "label": "Итого к оплате",
      "type": "number",
      "format": "currency",
      "required": true,
      "value": 1200,
      "section": "totals"
    }
  ],
  "extractedData": {
    "seller_name": "ООО Ромашка",
    "invoice_date": "2024-01-15",
    "total": 1200.00
  }
}
```

---


```
АГЕНТ: Создай файлы из раздела 3 (структура файлов) в папке Models/
Начни с: OpenAiSettings.cs → FieldDefinition.cs → DocumentSchema.cs → ExtractedData.cs
Строго следуй коду из раздела 5.1
```

### ШАГ 3: Реализовать DocumentParserService

```
АГЕНТ: Реализуй IDocumentParserService:

Метод ParseAsync(UploadedFile file):
  IF file.ContentType == "application/pdf":
    1. PdfPig: открыть PdfDocument.Open(file.Content)
    2. Для каждой страницы извлечь текст: page.GetWords()
    3. Конвертировать страницы в PNG изображения
       (использовать Docnet.Core или PDFiumCore для рендеринга)
    4. Ограничить до 10 страниц
    
  IF file.ContentType содержит "word" OR ".docx":
    1. WordprocessingDocument.Open(stream, false)
    2. Извлечь текст из Body.Descendants<Text>()
    3. Извлечь изображения из ImagePart
    
  IF file.ContentType начинается с "image/":
    1. Проверить размер через SixLabors.ImageSharp
    2. Если > 5MB — уменьшить до 2048px по длинной стороне
    3. Вернуть как единственную "страницу"
    
  Вернуть ParsedDocument { ExtractedText, Pages, Metadata }
```

### ШАГ 4: Реализовать OpenAiService

```
АГЕНТ: Реализуй OpenAiService:

public async Task<SchemaExtractionResult> ExtractSchemaAsync(...)
{
    // 1. Подготовить сообщения
    var messages = new List<object>();
    
    // 2. System message с промптом из раздела 6
    messages.Add(new { role = "system", content = SYSTEM_PROMPT });
    
    // 3. User message с изображениями
    var contentParts = new List<object>();
    contentParts.Add(new { type = "text", text = userPrompt });
    
    foreach (var page in allPages) // из всех файлов
    {
        contentParts.Add(new {
            type = "image_url",
            image_url = new {
                url = $"data:image/png;base64,{page.Base64}",
                detail = "high"
            }
        });
    }
    
    messages.Add(new { role = "user", content = contentParts });
    
    // 4. HTTP запрос
    var request = new {
        model = settings.Model,
        messages = messages,
        response_format = new { type = "json_object" },
        temperature = settings.Temperature,
        max_tokens = settings.MaxTokens
    };
    
    // 5. Отправить через HttpClient
    // 6. Десериализовать ответ
    // 7. Вернуть SchemaExtractionResult { Schema, ExtractedValues, RawResponse }
}
```

### ШАГ 5: Создать SchemaGeneratorService

```
АГЕНТ: Реализуй SchemaGeneratorService:

public DocumentSchema ParseOpenAiResponse(string jsonResponse)
{
    // 1. JsonDocument.Parse(jsonResponse)
    // 2. Извлечь documentType, documentTitle, language
    // 3. Парсить массив sections → List<FormSection>
    // 4. Парсить массив fields → List<FieldDefinition>
    //    - для каждого поля: map type string → FieldType enum
    //    - для "table" типа: рекурсивно парсить columns
    //    - для "select": парсить options array
    // 5. Вернуть DocumentSchema
}
```

### ШАГ 6: Создать главный компонент DocumentExtractor.razor

```
АГЕНТ: Компонент должен иметь следующие состояния (State Machine):

enum ExtractorState {
    Idle,           → показать FileUploadZone + SettingsPanel
    FilesLoaded,    → показать превью файлов + кнопку "Анализировать"
    Extracting,     → показать прогресс с сообщениями
    SchemaReady,    → показать DynamicForm + кнопки экспорта
    Exporting,      → показать прогресс экспорта
    Error           → показать ошибку с retry
}

Разметка компонента:
<div class="document-extractor">
    @if (State == Idle || State == FilesLoaded)
    {
        <SettingsPanel @bind-Settings="openAiSettings" />
        <FileUploadZone OnFilesSelected="HandleFilesSelected" />
        @if (uploadedFiles.Any())
        {
            <FilePreview Files="uploadedFiles" OnRemove="RemoveFile" />
            <button @onclick="StartExtraction">🔍 Анализировать документ</button>
        }
    }
    
    @if (State == Extracting)
    {
        <ProgressIndicator Message="@progressMessage" />
    }
    
    @if (State == SchemaReady)
    {
        <DynamicFormRenderer 
            Schema="currentSchema"
            Values="currentValues"
            OnValuesChanged="HandleValuesChanged" />
        <ExportPanel OnExportPdf="ExportPdf" OnExportWord="ExportWord" />
    }
    
    @if (State == Error)
    {
        <ErrorPanel Message="@errorMessage" OnRetry="Retry" />
    }
</div>
```

### ШАГ 7: DynamicFormRenderer.razor

```
АГЕНТ: Реализуй с использованием RenderFragment:

@foreach (var section in Schema.Sections.OrderBy(s => s.Order))
{
    <div class="form-section">
        <h3>@section.Title</h3>
        <div class="fields-grid">
            @foreach (var fieldKey in section.FieldKeys)
            {
                var field = Schema.Fields.First(f => f.Key == fieldKey);
                @RenderField(field)
            }
        </div>
    </div>
}

RenderFragment RenderField(FieldDefinition field) => field.Type switch
{
    FieldType.Text      => @<TextField Field="field" @bind-Value="GetValue(field.Key)" />,
    FieldType.TextArea  => @<TextAreaField Field="field" @bind-Value="GetValue(field.Key)" />,
    FieldType.Number    => @<NumberField Field="field" @bind-Value="GetValue(field.Key)" />,
    FieldType.Date      => @<DateField Field="field" @bind-Value="GetValue(field.Key)" />,
    FieldType.Boolean   => @<CheckboxField Field="field" @bind-Value="GetValue(field.Key)" />,
    FieldType.Select    => @<SelectField Field="field" @bind-Value="GetValue(field.Key)" />,
    FieldType.Table     => @<TableField Field="field" @bind-Value="GetValue(field.Key)" />,
    _                   => @<TextField Field="field" @bind-Value="GetValue(field.Key)" />
};
```

### ШАГ 8: SettingsPanel.razor

```
АГЕНТ: Форма настроек с:

1. API Key поле (type="password", с кнопкой показать/скрыть)
2. Base URL (по умолчанию https://api.openai.com/v1, редактируемый для Azure/Proxy)
3. Model selector — выпадающий список + поле ввода для кастомной модели
4. Temperature slider (0.0 - 1.0)
5. Max Tokens number input
6. Кнопка "Проверить соединение" — вызывает ValidateApiKeyAsync
7. Опция "Сохранить настройки" — localStorage через JS Interop
8. Кастомный System Prompt (textarea, collapsible)

JS для сохранения:
function saveSettings(key, value) { localStorage.setItem(key, JSON.stringify(value)); }
function loadSettings(key) { return JSON.parse(localStorage.getItem(key)); }
```

### ШАГ 9: Регистрация в DI

```csharp
// Program.cs
builder.Services.AddScoped<IDocumentParserService, DocumentParserService>();
builder.Services.AddScoped<IOpenAiService, OpenAiService>();
builder.Services.AddScoped<ISchemaGeneratorService, SchemaGeneratorService>();
builder.Services.AddScoped<IPdfExportService, PdfExportService>();
builder.Services.AddScoped<IWordExportService, WordExportService>();

// HttpClient для OpenAI
builder.Services.AddHttpClient<IOpenAiService, OpenAiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(180);
});

// QuestPDF
QuestPDF.Settings.License = LicenseType.Community;
```

### ШАГ 10: JS Interop

```
АГЕНТ: Создай wwwroot/js/documentExtractor.js со следующими функциями:

1. downloadFile(fileName, base64, mimeType) — скачать файл
2. saveSettings(settings) — сохранить в localStorage
3. loadSettings() → объект настроек — загрузить из localStorage
4. readFileAsBase64(fileInput, index) → string — прочитать файл как base64
5. showFilePicker(accept) — триггер выбора файлов
6. previewPdf(base64, containerId) — превью PDF через PDF.js

Подключить в _Host.cshtml или App.razor:
<script src="js/documentExtractor.js"></script>
```

---

## 9. Примеры кода

### FileUploadZone.razor — Drag & Drop

```razor
@* FileUploadZone.razor *@
@inject IJSRuntime JS

<div class="upload-zone @(isDragOver ? "drag-over" : "")"
     @ondragover="HandleDragOver"
     @ondragover:preventDefault="true"
     @ondragleave="HandleDragLeave"
     @ondrop="HandleDrop"
     @ondrop:preventDefault="true">
    
    <InputFile OnChange="HandleFileChange" 
               accept=".pdf,.docx,.doc,.jpg,.jpeg,.png,.webp,.gif"
               multiple
               id="fileInput"
               style="display:none" />
    
    <label for="fileInput" class="upload-label">
        <span class="icon">📄</span>
        <span class="title">Перетащите файлы сюда</span>
        <span class="subtitle">PDF, Word, изображения (JPG, PNG, WebP)</span>
        <button type="button" class="browse-btn" @onclick="OpenFilePicker">
            Выбрать файлы
        </button>
    </label>
    
    <div class="supported-formats">
        Поддерживается: .pdf .docx .doc .jpg .png .webp .gif
    </div>
</div>

@code {
    [Parameter] public EventCallback<List<UploadedFile>> OnFilesSelected { get; set; }
    
    private bool isDragOver = false;
    
    private async Task HandleFileChange(InputFileChangeEventArgs e)
    {
        var files = new List<UploadedFile>();
        foreach (var file in e.GetMultipleFiles(maxAllowedFiles: 10))
        {
            var buffer = new byte[file.Size];
            await file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024).ReadAsync(buffer);
            
            files.Add(new UploadedFile
            {
                FileName = file.Name,
                ContentType = file.ContentType,
                Content = buffer,
                Size = file.Size,
                Category = DetermineCategory(file.ContentType)
            });
        }
        await OnFilesSelected.InvokeAsync(files);
    }
    
    private FileCategory DetermineCategory(string contentType) => contentType switch
    {
        "application/pdf" => FileCategory.Pdf,
        var t when t.Contains("word") || t.Contains("officedocument") => FileCategory.Word,
        var t when t.StartsWith("image/") => FileCategory.Image,
        _ => FileCategory.Unknown
    };
    
    private void HandleDragOver() => isDragOver = true;
    private void HandleDragLeave() => isDragOver = false;
    
    private async Task HandleDrop(DragEventArgs e)
    {
        isDragOver = false;
        // Drag & Drop файлов обрабатывается через JS Interop
        // так как Blazor не даёт прямого доступа к DataTransfer.files
        await JS.InvokeVoidAsync("documentExtractor.handleDrop", e);
    }
}
```

### OpenAiService.cs — HTTP вызов

```csharp
public async Task<SchemaExtractionResult> ExtractSchemaAsync(
    List<UploadedFile> files,
    OpenAiSettings settings,
    IProgress<string>? progress = null)
{
    progress?.Report("Подготовка файлов...");
    
    // Собрать все страницы из всех файлов
    var allPages = new List<PageImage>();
    var allText = new StringBuilder();
    
    foreach (var file in files)
    {
        progress?.Report($"Парсинг {file.FileName}...");
        var parsed = await _parserService.ParseAsync(file);
        allPages.AddRange(parsed.Pages);
        allText.AppendLine(parsed.ExtractedText);
    }
    
    // Ограничить количество страниц (OpenAI лимит)
    var pagesToSend = allPages.Take(15).ToList();
    
    progress?.Report("Отправка в OpenAI...");
    
    // Сформировать content array
    var contentParts = new List<object>
    {
        new {
            type = "text",
            text = $"""
                Analyze this document ({pagesToSend.Count} pages).
                Extracted text for reference:
                {allText.ToString().Truncate(3000)}
                
                Return complete JSON schema and extracted data.
                """
        }
    };
    
    foreach (var page in pagesToSend)
    {
        contentParts.Add(new {
            type = "image_url",
            image_url = new {
                url = $"data:image/png;base64,{page.Base64Content}",
                detail = "high"
            }
        });
    }
    
    var requestBody = new
    {
        model = settings.Model,
        messages = new[]
        {
            new { role = "system", content = GetSystemPrompt(settings) },
            new { role = "user", content = contentParts }
        },
        response_format = new { type = "json_object" },
        temperature = settings.Temperature,
        max_tokens = settings.MaxTokens
    };
    
    _httpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", settings.ApiKey);
    
    if (!string.IsNullOrEmpty(settings.BaseUrl) && 
        settings.BaseUrl != "https://api.openai.com/v1")
    {
        _httpClient.BaseAddress = new Uri(settings.BaseUrl);
    }
    
    var json = JsonSerializer.Serialize(requestBody);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    
    var response = await _httpClient.PostAsync(
        $"{settings.BaseUrl}/chat/completions", content);
    
    response.EnsureSuccessStatusCode();
    
    var responseJson = await response.Content.ReadAsStringAsync();
    var apiResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(responseJson);
    
    var rawSchema = apiResponse!.Choices[0].Message.Content;
    
    progress?.Report("Генерация формы...");
    
    var schema = _schemaGenerator.ParseOpenAiResponse(rawSchema);
    var values = _schemaGenerator.ExtractValues(rawSchema);
    
    return new SchemaExtractionResult
    {
        Schema = schema,
        ExtractedValues = values,
        RawResponse = rawSchema
    };
}
```

### TableField.razor

```razor
@* TableField.razor *@
<div class="table-field">
    <label class="field-label">@Field.Label @(Field.Required ? "*" : "")</label>
    
    <div class="table-wrapper">
        <table>
            <thead>
                <tr>
                    @foreach (var col in Field.Columns!)
                    {
                        <th>@col.Label</th>
                    }
                    <th class="actions-col">Действия</th>
                </tr>
            </thead>
            <tbody>
                @for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    var row = rows[rowIndex];
                    var capturedIndex = rowIndex;
                    <tr>
                        @foreach (var col in Field.Columns!)
                        {
                            <td>
                                @switch (col.Type)
                                {
                                    case FieldType.Number:
                                        <input type="number" 
                                               value="@GetCellValue(row, col.Key)"
                                               @onchange="e => SetCellValue(capturedIndex, col.Key, e.Value)"
                                               class="cell-input" />
                                        break;
                                    case FieldType.Date:
                                        <input type="date"
                                               value="@GetCellValue(row, col.Key)"
                                               @onchange="e => SetCellValue(capturedIndex, col.Key, e.Value)"
                                               class="cell-input" />
                                        break;
                                    default:
                                        <input type="text"
                                               value="@GetCellValue(row, col.Key)"
                                               @onchange="e => SetCellValue(capturedIndex, col.Key, e.Value)"
                                               class="cell-input" />
                                        break;
                                }
                            </td>
                        }
                        <td>
                            <button @onclick="() => RemoveRow(capturedIndex)" 
                                    class="btn-danger-sm">✕</button>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
    
    <button @onclick="AddRow" class="btn-add-row">+ Добавить строку</button>
</div>

@code {
    [Parameter] public FieldDefinition Field { get; set; } = new();
    [Parameter] public object? Value { get; set; }
    [Parameter] public EventCallback<object?> ValueChanged { get; set; }
    
    private List<Dictionary<string, object?>> rows = new();
    
    protected override void OnParametersSet()
    {
        if (Value is List<Dictionary<string, object?>> existingRows)
            rows = existingRows;
        else
            rows = new List<Dictionary<string, object?>>();
    }
    
    private void AddRow()
    {
        var newRow = new Dictionary<string, object?>();
        foreach (var col in Field.Columns!)
            newRow[col.Key] = col.DefaultValue;
        rows.Add(newRow);
        NotifyChanged();
    }
    
    private void RemoveRow(int index)
    {
        rows.RemoveAt(index);
        NotifyChanged();
    }
    
    private object? GetCellValue(Dictionary<string, object?> row, string key) =>
        row.TryGetValue(key, out var val) ? val : null;
    
    private void SetCellValue(int rowIndex, string key, object? value)
    {
        rows[rowIndex][key] = value;
        NotifyChanged();
    }
    
    private async void NotifyChanged() =>
        await ValueChanged.InvokeAsync(rows);
}
```

---

## 10. Тест-кейсы

### Unit тесты

```
АГЕНТ: Создай тесты для:

1. SchemaGeneratorService:
   - Тест: корректный JSON от OpenAI → правильная DocumentSchema
   - Тест: JSON с table полями → правильные Columns
   - Тест: невалидный JSON → понятное исключение

2. DocumentParserService:
   - Тест: PDF с текстом → ExtractedText не пустой
   - Тест: изображение > 5MB → уменьшается
   - Тест: неподдерживаемый формат → исключение с сообщением

3. ExportService:
   - Тест: данные + схема → валидный PDF (не 0 байт)
   - Тест: данные + схема → валидный DOCX (открывается OpenXML)
   - Тест: таблица с 3 строками → 3 строки в PDF

4. OpenAiService (мок):
   - Тест: успешный ответ → SchemaExtractionResult
   - Тест: 429 ошибка → retry 3 раза → исключение
   - Тест: невалидный API key → понятное сообщение
```

### E2E тест-кейсы

```
СЦЕНАРИЙ 1: PDF счёт-фактура
  Input:  sample_invoice.pdf (2 страницы)
  Ожидание:
    - Поля: seller, buyer, invoice_number, date, line_items (table), total
    - line_items имеет минимум 1 строку
    - total — числовое поле
  Output: PDF с теми же полями, заполненными

СЦЕНАРИЙ 2: Word договор
  Input:  contract.docx
  Ожидание:
    - Поля: parties (2 стороны), date, subject, amount, duration
    - Текстовые блоки сохранены как textarea
  Output: Word with filled data

СЦЕНАРИЙ 3: Фото паспорта/формы
  Input:  form_photo.jpg
  Ожидание:
    - Поля из видимой формы распознаны
    - Рукописный текст попытка распознавания

СЦЕНАРИЙ 4: Несколько файлов
  Input:  [page1.jpg, page2.jpg, page3.jpg]
  Ожидание:
    - Все три страницы анализируются как один документ
    - Объединённая схема

СЦЕНАРИЙ 5: Кастомная OpenAI-совместимая модель
  Settings: BaseUrl = "https://my-azure.openai.azure.com/openai/deployments/gpt-4o"
  Input:  document.pdf
  Ожидание: корректный запрос к Azure OpenAI
```

---

## 📝 Дополнительные замечания для агента

### Важные нюансы

```
1. BLAZOR SERVER vs WASM:
   - Для Blazor Server: PDF парсинг работает на сервере — OK
   - Для Blazor WASM: PDF парсинг нужно делать через API endpoint
     (нельзя использовать нативные библиотеки напрямую в браузере)
   - Рекомендация: Blazor Server для простоты

2. ФАЙЛОВЫЕ ЛИМИТЫ:
   - InputFile.maxAllowedSize = 50MB по умолчанию
   - OpenAI Vision: до 20MB на изображение, до 2000 токенов на страницу
   - Решение: автоматически уменьшать страницы до 1024x1024 для detail="low"
     или 2048x2048 для detail="high"

3. БЕЗОПАСНОСТЬ API KEY:
   - НЕ хранить API ключ в коде
   - В localStorage — только с предупреждением пользователю
   - Для продакшена: прокси-сервер (бэкенд хранит ключ, фронт не видит)

4. ОРИГИНАЛЬНОЕ ФОРМАТИРОВАНИЕ PDF:
   - Точное воспроизведение PDF очень сложно
   - Реалистичный подход: воспроизвести структуру (секции, таблицы)
     с похожим форматированием, но не пиксель-в-пиксель
   - Для PDF форм с AcroForm полями — можно заполнять напрямую через iText7

5. МНОГОЯЗЫЧНОСТЬ:
   - OpenAI хорошо работает с русским, немецким, французским
   - Метки полей генерировать на языке документа
   - UI компонента — отдельная локализация

6. СТРИМИНГ ОТВЕТА:
   - Для UX можно использовать SSE (Server-Sent Events) / stream: true
   - Показывать частичный JSON по мере получения
   - Сложно реализовать с response_format: json_object
   - Альтернатива: просто показывать spinner с текстом "Анализирую..."
```

### Рекомендуемый порядок разработки (для агента)

```
День 1:
  ✅ Модели данных (раздел 5.1)
  ✅ DocumentParserService (PDF текст + изображения)
  ✅ OpenAiService (базовый HTTP вызов)

День 2:
  ✅ SchemaGeneratorService (парсинг JSON ответа)
  ✅ SettingsPanel.razor
  ✅ FileUploadZone.razor

День 3:
  ✅ DynamicFormRenderer.razor
  ✅ Все FieldComponents (TextField, NumberField, DateField, etc.)
  ✅ TableField.razor (самый сложный)

День 4:
  ✅ PdfExportService (QuestPDF)
  ✅ WordExportService (OpenXML)
  ✅ ExportPanel.razor

День 5:
  ✅ Главный DocumentExtractor.razor (сборка всего)
  ✅ JS Interop файл
  ✅ DI регистрация
  ✅ Тестирование на реальных документах
```

---

*Документ создан для разработки Blazor компонента DocumentExtractor с интеграцией OpenAI API*
*Версия: 1.0 | Дата: 2025*
