using System;
using System.Collections.Generic;
using System.Linq;

namespace SuperUI.Services.Llm;

/// <summary>
/// Central provider registry used by the UI, service layer and provider catalog.
/// Keep provider metadata here to avoid provider lists drifting between components.
/// </summary>
public static class SgLlmProviderRegistry
{
    public static readonly IReadOnlyList<SgLlmProvider> AllowedProviders = new[]
    {
        SgLlmProvider.OpenRouter,
        SgLlmProvider.OpenAiCompatible,
        SgLlmProvider.Anthropic,
        SgLlmProvider.Ollama,
        SgLlmProvider.LmStudio,
        SgLlmProvider.HuggingFace,
        SgLlmProvider.GigaGpt
    };

    private static readonly IReadOnlyDictionary<SgLlmProvider, SgLlmProviderPreset> _presets = new Dictionary<SgLlmProvider, SgLlmProviderPreset>
    {
        [SgLlmProvider.OpenRouter] = new()
        {
            Provider = SgLlmProvider.OpenRouter,
            Label = "OpenRouter",
            BaseUrl = "https://openrouter.ai/api/v1",
            ApiKeyUrl = "https://openrouter.ai/settings/keys",
            DocsUrl = "https://openrouter.ai/docs",
            IsFree = true,
            RequiresKey = true,
            Notes = "Единый каталог и маршрутизация моделей; есть free-модели."
        },
        [SgLlmProvider.OpenAiCompatible] = new()
        {
            Provider = SgLlmProvider.OpenAiCompatible,
            Label = "OpenAI Compatible",
            BaseUrl = "https://api.openai.com/v1",
            ApiKeyUrl = "https://platform.openai.com/api-keys",
            DocsUrl = "https://developers.openai.com/api/docs/models",
            IsFree = false,
            RequiresKey = true,
            Notes = "Любой endpoint с /chat/completions и /models; настройки нормализуются через Microsoft.Extensions.AI."
        },
        [SgLlmProvider.Anthropic] = new()
        {
            Provider = SgLlmProvider.Anthropic,
            Label = "Anthropic",
            BaseUrl = "https://api.anthropic.com/v1",
            ApiKeyUrl = "https://console.anthropic.com/settings/keys",
            DocsUrl = "https://platform.claude.com/docs/en/about-claude/models/overview",
            IsFree = false,
            RequiresKey = true,
            Notes = "Claude Messages API, thinking и service tier."
        },
        [SgLlmProvider.Ollama] = new()
        {
            Provider = SgLlmProvider.Ollama,
            Label = "Ollama",
            BaseUrl = "http://localhost:11434",
            DocsUrl = "https://ollama.com/",
            IsFree = true,
            RequiresKey = false,
            Notes = "Локальные модели, ключ не нужен."
        },
        [SgLlmProvider.LmStudio] = new()
        {
            Provider = SgLlmProvider.LmStudio,
            Label = "LM Studio",
            BaseUrl = "http://localhost:1234/v1",
            DocsUrl = "https://lmstudio.ai/docs",
            IsFree = true,
            RequiresKey = false,
            Notes = "Локальный OpenAI-compatible server."
        },
        [SgLlmProvider.HuggingFace] = new()
        {
            Provider = SgLlmProvider.HuggingFace,
            Label = "Hugging Face",
            BaseUrl = "https://router.huggingface.co/v1",
            ApiKeyUrl = "https://huggingface.co/settings/tokens",
            DocsUrl = "https://huggingface.co/docs/inference-providers/",
            IsFree = true,
            RequiresKey = true,
            Notes = "Inference Router, OpenAI-compatible API."
        },
        [SgLlmProvider.GigaGpt] = new()
        {
            Provider = SgLlmProvider.GigaGpt,
            Label = "GigaGPT / GigaChat",
            BaseUrl = "https://gigachat.devices.sberbank.ru/api/v1",
            ApiKeyUrl = "https://developers.sber.ru/portal/products/gigachat-api",
            DocsUrl = "https://developers.sber.ru/docs/ru/gigachat/models/main",
            IsFree = false,
            RequiresKey = true,
            Notes = "GigaChat/GigaGPT-compatible chat/completions endpoint."
        }
    };

    public static IReadOnlyList<SgLlmProviderPreset> Presets => AllowedProviders.Select(GetPreset).Where(p => p is not null).Cast<SgLlmProviderPreset>().ToList();

    public static SgLlmProviderPreset? GetPreset(SgLlmProvider provider) =>
        _presets.TryGetValue(provider, out var preset) ? preset : null;

    public static bool IsAllowed(SgLlmProvider provider) => AllowedProviders.Contains(provider);

    public static string Label(SgLlmProvider provider) => GetPreset(provider)?.Label ?? provider.ToString();

    public static string DefaultBaseUrl(SgLlmProvider provider) => GetPreset(provider)?.BaseUrl ?? string.Empty;

    public static bool RequiresKey(SgLlmProvider provider) => GetPreset(provider)?.RequiresKey ?? true;

    public static string ShortHint(SgLlmProvider provider) => provider switch
    {
        SgLlmProvider.OpenRouter => "маршрутизация и каталог моделей",
        SgLlmProvider.OpenAiCompatible => "через Microsoft.Extensions.AI",
        SgLlmProvider.Anthropic => "Claude Messages API",
        SgLlmProvider.Ollama => "локально, без ключа",
        SgLlmProvider.LmStudio => "локальный OpenAI-compatible сервер",
        SgLlmProvider.HuggingFace => "Inference Router",
        SgLlmProvider.GigaGpt => "GigaChat/GigaGPT API",
        _ => "выберите провайдера"
    };

    public static string ConnectionHint(SgLlmProvider provider) => provider switch
    {
        SgLlmProvider.Ollama => "Ключ не нужен. Укажите адрес Ollama API, обычно http://localhost:11434.",
        SgLlmProvider.LmStudio => "Запустите Local Server в LM Studio. Ключ обычно не нужен, Base URL — http://localhost:1234/v1.",
        SgLlmProvider.OpenAiCompatible => "Любой OpenAI-compatible endpoint. Параметры приводятся к Microsoft.Extensions.AI ChatOptions.",
        SgLlmProvider.HuggingFace => "Используйте Hugging Face token и router endpoint.",
        SgLlmProvider.GigaGpt => "Укажите bearer token/ключ и совместимый endpoint GigaGPT/GigaChat.",
        SgLlmProvider.Anthropic => "Нужен x-api-key; используется /v1/messages.",
        _ => "Адрес API подставляется автоматически при выборе провайдера."
    };

    public static bool NeedsBaseUrl(SgLlmProvider provider) => IsAllowed(provider);

    public static bool SupportsPenalties(SgLlmProvider provider) =>
        provider is SgLlmProvider.OpenAiCompatible or SgLlmProvider.OpenRouter or SgLlmProvider.LmStudio or SgLlmProvider.HuggingFace or SgLlmProvider.GigaGpt or SgLlmProvider.Ollama;

    public static bool SupportsTopKMinP(SgLlmProvider provider) =>
        provider is SgLlmProvider.OpenRouter or SgLlmProvider.Ollama or SgLlmProvider.Anthropic or SgLlmProvider.LmStudio;

    public static bool SupportsReasoning(SgLlmProvider provider) =>
        provider is SgLlmProvider.OpenAiCompatible or SgLlmProvider.OpenRouter or SgLlmProvider.HuggingFace or SgLlmProvider.GigaGpt;

    public static bool SupportsServiceTier(SgLlmProvider provider) =>
        provider is SgLlmProvider.OpenAiCompatible or SgLlmProvider.OpenRouter or SgLlmProvider.Anthropic;

    public static SgLlmProvider DetectProvider(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return SgLlmProvider.None;
        var url = baseUrl.Trim().ToLowerInvariant();
        if (url.Contains("openrouter.ai")) return SgLlmProvider.OpenRouter;
        if (url.Contains("api.anthropic.com")) return SgLlmProvider.Anthropic;
        if (url.Contains("localhost:11434") || url.Contains("127.0.0.1:11434")) return SgLlmProvider.Ollama;
        if (url.Contains("localhost:1234") || url.Contains("127.0.0.1:1234")) return SgLlmProvider.LmStudio;
        if (url.Contains("huggingface.co") || url.Contains("router.huggingface")) return SgLlmProvider.HuggingFace;
        if (url.Contains("gigachat") || url.Contains("sberbank.ru")) return SgLlmProvider.GigaGpt;
        if (url.Contains("openai.com")) return SgLlmProvider.OpenAiCompatible;
        return SgLlmProvider.OpenAiCompatible;
    }

    public static string NormalizeBaseUrl(SgLlmProvider provider, string? baseUrl)
    {
        var url = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl(provider) : baseUrl.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(url)) return url;

        if (provider == SgLlmProvider.OpenRouter && !url.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase))
        {
            if (url.Contains("openrouter.ai", StringComparison.OrdinalIgnoreCase)) return "https://openrouter.ai/api/v1";
        }

        if ((provider == SgLlmProvider.OpenAiCompatible || provider == SgLlmProvider.LmStudio || provider == SgLlmProvider.HuggingFace || provider == SgLlmProvider.GigaGpt)
            && !url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            && !url.EndsWith("/openai", StringComparison.OrdinalIgnoreCase))
        {
            if (provider == SgLlmProvider.OpenAiCompatible && url.Contains("api.openai.com", StringComparison.OrdinalIgnoreCase)) return "https://api.openai.com/v1";
            if (provider == SgLlmProvider.LmStudio && (url.Contains("localhost:1234", StringComparison.OrdinalIgnoreCase) || url.Contains("127.0.0.1:1234", StringComparison.OrdinalIgnoreCase))) return url + "/v1";
            if (provider == SgLlmProvider.HuggingFace && url.Contains("huggingface", StringComparison.OrdinalIgnoreCase)) return "https://router.huggingface.co/v1";
            if (provider == SgLlmProvider.GigaGpt && (url.Contains("gigachat", StringComparison.OrdinalIgnoreCase) || url.Contains("sberbank.ru", StringComparison.OrdinalIgnoreCase))) return "https://gigachat.devices.sberbank.ru/api/v1";
        }

        return url;
    }

    public static List<SgLlmModelInfo> FallbackModels(SgLlmProvider provider) => provider switch
    {
        // OpenRouter fallback refreshed against https://openrouter.ai/api/v1/models (May 2026).
        // Runtime still calls /models first; these are only the offline/failure defaults.
        SgLlmProvider.OpenRouter => new()
        {
            Model("openai/gpt-5.5", "OpenAI GPT-5.5", provider, true, true, true, true, 1_050_000, true),
            Model("anthropic/claude-opus-4.7-fast", "Claude Opus 4.7 Fast", provider, true, true, true, true, 1_000_000, true),
            Model("google/gemini-3.5-flash", "Gemini 3.5 Flash", provider, true, true, true, false, 1_048_576, true),
            Model("qwen/qwen3.7-max", "Qwen3.7 Max", provider, false, true, true, true, 1_000_000, false),
            Model("x-ai/grok-4.3", "Grok 4.3", provider, true, true, true, true, 1_000_000, false),
            Model("mistralai/mistral-medium-3-5", "Mistral Medium 3.5", provider, true, true, true, false, 262_144, false),
            Model("deepseek/deepseek-v4-flash:free", "DeepSeek V4 Flash Free", provider, false, true, true, true, 1_048_576, false, true),
            Model("nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free", "Nemotron 3 Nano Omni Free", provider, true, true, false, true, 256_000, false, true),
            Model("google/gemma-4-31b-it:free", "Gemma 4 31B Free", provider, true, true, true, false, 262_144, false, true),
            Model("qwen/qwen3-coder:free", "Qwen3 Coder Free", provider, false, true, false, false, 1_048_576, false, true)
        },
        // OpenAI official docs currently recommend GPT-5.5; GPT-5.4 mini/nano for latency/cost.
        SgLlmProvider.OpenAiCompatible => new()
        {
            Model("gpt-5.5", "GPT-5.5", provider, true, true, true, true, 1_000_000, true),
            Model("gpt-5.5-pro", "GPT-5.5 Pro", provider, true, true, true, true, 1_000_000, false),
            Model("gpt-5.4", "GPT-5.4", provider, true, true, true, true, 1_000_000, true),
            Model("gpt-5.4-mini", "GPT-5.4 mini", provider, true, true, true, true, 400_000, true),
            Model("gpt-5.4-nano", "GPT-5.4 nano", provider, true, true, true, true, 400_000, false),
            Model("gpt-5.2-chat-latest", "GPT-5.2 Chat Latest", provider, true, true, true, true, 400_000, false)
        },
        // Anthropic official model overview: Opus 4.7, Sonnet 4.6, Haiku 4.5.
        SgLlmProvider.Anthropic => new()
        {
            Model("claude-opus-4-7", "Claude Opus 4.7", provider, true, true, true, true, 1_000_000, true),
            Model("claude-sonnet-4-6", "Claude Sonnet 4.6", provider, true, true, true, true, 1_000_000, true),
            Model("claude-haiku-4-5", "Claude Haiku 4.5", provider, true, true, true, true, 200_000, true),
            Model("claude-opus-4-6", "Claude Opus 4.6", provider, true, true, true, true, 1_000_000, false),
            Model("claude-opus-4-1", "Claude Opus 4.1", provider, true, true, true, true, 200_000, false)
        },
        // Ollama library pages checked for these IDs; local /api/tags is still authoritative.
        SgLlmProvider.Ollama => new()
        {
            Model("qwen3.6", "Qwen3.6", provider, true, true, false, true, 256_000, true, true),
            Model("kimi-k2.6:cloud", "Kimi K2.6 Cloud", provider, true, true, false, true, 256_000, true, true),
            Model("deepseek-v4-pro:cloud", "DeepSeek V4 Pro Cloud", provider, false, true, false, true, 1_000_000, true, true),
            Model("gemma4:31b", "Gemma 4 31B", provider, true, true, true, false, 262_144, false, true),
            Model("devstral-small", "Devstral Small", provider, false, true, false, false, 128_000, false, true),
            Model("gpt-oss:120b", "GPT-OSS 120B", provider, false, true, true, true, 131_072, false, true)
        },
        SgLlmProvider.LmStudio => new()
        {
            Model("local-model", "LM Studio loaded model", provider, false, true, false, false, 0, true, true),
            Model("qwen/qwen3.6-35b-a3b", "Qwen3.6 35B A3B", provider, true, true, true, true, 262_144, true, true),
            Model("deepseek-ai/deepseek-v4-flash", "DeepSeek V4 Flash", provider, false, true, true, true, 1_000_000, false, true),
            Model("moonshotai/kimi-k2.6", "Kimi K2.6", provider, true, true, false, true, 256_000, false, true),
            Model("google/gemma-4-31b-it", "Gemma 4 31B IT", provider, true, true, true, false, 262_144, false, true),
            Model("openai/gpt-oss-120b", "GPT-OSS 120B", provider, false, true, true, true, 131_072, false, true)
        },
        // Hugging Face router /v1/models checked May 2026.
        SgLlmProvider.HuggingFace => new()
        {
            Model("deepseek-ai/DeepSeek-V4-Pro", "DeepSeek V4 Pro", provider, false, true, true, true, 1_048_576, true),
            Model("deepseek-ai/DeepSeek-V4-Flash", "DeepSeek V4 Flash", provider, false, true, true, true, 1_048_576, true),
            Model("moonshotai/Kimi-K2.6", "Kimi K2.6", provider, true, true, false, true, 262_144, false),
            Model("Qwen/Qwen3.6-35B-A3B", "Qwen3.6 35B A3B", provider, true, true, true, true, 262_144, false),
            Model("google/gemma-4-31B-it", "Gemma 4 31B IT", provider, true, true, true, false, 262_144, false),
            Model("zai-org/GLM-5.1", "GLM-5.1", provider, false, true, true, true, 202_752, false),
            Model("MiniMaxAI/MiniMax-M2.7", "MiniMax M2.7", provider, false, true, true, false, 204_800, false),
            Model("openai/gpt-oss-120b", "GPT-OSS 120B", provider, false, true, true, true, 131_072, false)
        },
        // Official GigaChat docs list 2 Max/Pro/Lite generation models; GET /models is used at runtime.
        SgLlmProvider.GigaGpt => new()
        {
            Model("GigaChat-2-Max", "GigaChat 2 Max", provider, true, true, true, false, 128_000, true),
            Model("GigaChat-2-Pro", "GigaChat 2 Pro", provider, true, true, true, false, 128_000, true),
            Model("GigaChat-2-Lite", "GigaChat 2 Lite", provider, false, true, true, false, 128_000, false),
            Model("GigaChat-2-Max-preview", "GigaChat 2 Max Preview", provider, true, true, true, false, 128_000, false),
            Model("GigaChat-2-Pro-preview", "GigaChat 2 Pro Preview", provider, true, true, true, false, 128_000, false),
            Model("GigaChat-Plus", "GigaChat Plus", provider, false, true, true, false, 131_000, false)
        },
        _ => new()
    };

    private static SgLlmModelInfo Model(
        string id,
        string name,
        SgLlmProvider provider,
        bool vision,
        bool tools,
        bool json,
        bool reasoning,
        int context,
        bool recommended,
        bool free = false) => new()
        {
            Id = id,
            Name = name,
            Provider = provider,
            ProviderLabel = Label(provider),
            SupportsVision = vision,
            SupportsTools = tools,
            SupportsJsonSchema = json,
            SupportsReasoning = reasoning,
            ContextWindow = context,
            IsRecommended = recommended,
            IsFree = free,
            Description = BuildDescription(provider, context, vision, tools, json, reasoning, free)
        };

    private static string BuildDescription(SgLlmProvider provider, int context, bool vision, bool tools, bool json, bool reasoning, bool free)
    {
        var caps = new List<string>();
        if (context > 0) caps.Add($"ctx {context:n0}");
        if (vision) caps.Add("vision");
        if (tools) caps.Add("tools");
        if (json) caps.Add("json");
        if (reasoning) caps.Add("reasoning");
        if (free) caps.Add("free");
        return string.Join(" · ", caps);
    }
}
