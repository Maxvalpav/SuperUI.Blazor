namespace SuperUI.Services.Llm;

/// <summary>
/// Содержит описания и подсказки для параметров LLM на русском языке.
/// </summary>
public static class SgLlmTooltips
{
    public static string Temperature => "Контролирует случайность ответа. Низкие значения (0.2) делают ответ точным и предсказуемым, высокие (0.8+) — творческим и разнообразным.";
    
    public static string TopP => "Nucleus sampling. Учитываются только токены, суммарная вероятность которых составляет P. Альтернатива температуре.";
    
    public static string MaxTokens => "Максимальное количество токенов, которое модель может сгенерировать в одном ответе.";
    
    public static string StopSequences => "Список строк, при появлении которых модель прекратит генерацию. Обычно это \\n\\n или маркеры диалога.";
    
    public static string PresencePenalty => "Наказывает модель за использование уже упомянутых тем. Положительные значения увеличивают вероятность перехода к новым темам.";
    
    public static string FrequencyPenalty => "Наказывает модель за дословное повторение слов. Снижает вероятность повторения одних и тех же фраз.";
    
    public static string Seed => "Если задано, модель будет стараться выдавать детерминированный (одинаковый) результат для одного и того же запроса.";
    
    public static string ResponseFormat => "Формат ответа. 'json_object' или 'json_schema' заставляют модель возвращать валидный JSON.";
    
    public static string ParallelToolCalls => "Разрешить модели вызывать несколько инструментов одновременно (например, два поиска в Google).";
    
    public static string StreamUsage => "Включает отправку статистики использования (токены) в потоке ответов.";
    
    public static string ReasoningEffort => "Для моделей с рассуждением (o1, deepseek-r1): уровень усилий, затрачиваемых на 'раздумья' перед ответом.";
    
    public static string Verbosity => "Уровень детализации процесса рассуждения модели.";
    
    public static string AnthropicThinking => "Включает режим 'расширенного мышления' для моделей Claude 3.7+.";
    
    public static string ThinkingBudget => "Лимит токенов, которые модель может потратить на внутренние рассуждения.";
    
    public static string OnlyFreeModels => "Скрывает платные модели и запрещает их использование для экономии средств.";
    
    public static string WarnOnPaidModels => "Показывать предупреждение, если выбрана платная модель.";
    
    public static string DailyTokenLimit => "Максимальное количество токенов, которое разрешено потратить за текущие сутки во всех компонентах.";
    
    public static string RequestTokenLimit => "Максимальное количество токенов на один запрос. Помогает избежать слишком длинных и дорогих ответов.";

    public static string Provider => "Выберите сервис, через который будут идти запросы (OpenRouter, Ollama, OpenAI и др.).";

    public static string Model => "Конкретная нейросеть (например, GPT-4o, Claude 3.5 Sonnet, DeepSeek V3).";

    public static string BaseUrl => "Адрес API провайдера. Для локальных сервисов (Ollama) это обычно localhost.";

    public static string ApiKey => "Ваш персональный ключ доступа к сервису. Не передается на сервер, если не включен Backend Proxy.";

    public static string SystemPrompt => "Глобальная инструкция для модели, определяющая её поведение, тон и ограничения.";

    public static string UseAdvanced => "Показать тонкие настройки генерации (температура, штрафы, формат вывода).";
}
