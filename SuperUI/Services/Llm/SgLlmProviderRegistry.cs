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
    // Curated short list shown in the provider picker. The full SgLlmProvider enum stays
    // append-only (numeric ordinals are persisted in LocalStorage — never re-number or remove
    // enum members), but only these providers are surfaced in the UI. To re-expose a hidden
    // provider, add it back here and ensure it has a _presets entry + a LoadModelsAsync branch.
    public static readonly IReadOnlyList<SgLlmProvider> AllowedProviders = new[]
    {
        // --- Frontier ---
        SgLlmProvider.OpenAiCompatible,   // OpenAI (api.openai.com) + OpenAI-compatible
        SgLlmProvider.Anthropic,
        SgLlmProvider.Google,             // kept for preset coverage
        // --- OpenAI-compatible aggregator / coding agent ---
        SgLlmProvider.OpenCode,
        // --- Local ---
        SgLlmProvider.LmStudio,
        SgLlmProvider.LlamaCpp,
        // --- Russian ---
        SgLlmProvider.GigaGpt,
        SgLlmProvider.YandexGpt,
        // --- Custom OpenAI-compatible endpoint ---
        SgLlmProvider.OpenAiCompatibleCustom
    };

    private static readonly IReadOnlyDictionary<SgLlmProvider, SgLlmProviderPreset> _presets = new Dictionary<SgLlmProvider, SgLlmProviderPreset>
    {
        // ===== Frontier =====
        [SgLlmProvider.OpenAiCompatible] = new()
        {
            Provider = SgLlmProvider.OpenAiCompatible,
            Label = "OpenAI",
            BaseUrl = "https://api.openai.com/v1",
            ApiKeyUrl = "https://platform.openai.com/api-keys",
            DocsUrl = "https://developers.openai.com/api/docs/models",
            IsFree = false,
            RequiresKey = true,
            Icon = "🤖",
            Category = SgLlmProviderCategory.Frontier,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Vision, SgLlmProviderTag.Tools, SgLlmProviderTag.Reasoning, SgLlmProviderTag.Embeddings, SgLlmProviderTag.Images, SgLlmProviderTag.Audio, SgLlmProviderTag.Agentic },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsEmbeddings = true, SupportsImages = true, SupportsAudioStt = true, SupportsAudioTts = true,
            SupportsReasoningModels = true, SupportsTools = true, SupportsVision = true,
            Notes = "GPT-5.5, o-series; chat/completions + responses API."
        },
        [SgLlmProvider.OpenCode] = new()
        {
            Provider = SgLlmProvider.OpenCode,
            Label = "OpenCode",
            BaseUrl = "https://opencode.ai/zen/v1",
            ApiKeyUrl = "https://opencode.ai/auth",
            DocsUrl = "https://opencode.ai/docs",
            IsFree = false,
            RequiresKey = true,
            Icon = "🧑‍💻",
            Category = SgLlmProviderCategory.OpenRouting,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Tools, SgLlmProviderTag.Agentic },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true,
            Notes = "OpenCode Zen — OpenAI-compatible gateway for coding agents."
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
            Icon = "🅰️",
            Category = SgLlmProviderCategory.Frontier,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Vision, SgLlmProviderTag.Tools, SgLlmProviderTag.Reasoning, SgLlmProviderTag.Agentic },
            Auth = SgLlmAuthStyle.XApiKey,
            ApiStyle = SgLlmApiStyle.AnthropicMessages,
            SupportsTools = true, SupportsVision = true, SupportsReasoningModels = true,
            Notes = "Claude Messages API; extended thinking + service tier."
        },
        [SgLlmProvider.Google] = new()
        {
            Provider = SgLlmProvider.Google,
            Label = "Google Gemini",
            BaseUrl = "https://generativelanguage.googleapis.com/v1beta",
            ApiKeyUrl = "https://aistudio.google.com/apikey",
            DocsUrl = "https://ai.google.dev/gemini-api/docs",
            IsFree = true,
            RequiresKey = true,
            Icon = "🌈",
            Category = SgLlmProviderCategory.Frontier,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Vision, SgLlmProviderTag.Tools, SgLlmProviderTag.Reasoning, SgLlmProviderTag.Embeddings, SgLlmProviderTag.Images, SgLlmProviderTag.Audio, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.GoogleGemini,
            SupportsEmbeddings = true, SupportsImages = true, SupportsAudioStt = true,
            SupportsReasoningModels = true, SupportsTools = true, SupportsVision = true,
            FreeTierNotes = "Бесплатный тариф Gemini API в AI Studio.",
            Notes = "Gemini API v1beta — chat, embeddings, image gen."
        },
        [SgLlmProvider.XAi] = new()
        {
            Provider = SgLlmProvider.XAi,
            Label = "xAI Grok",
            BaseUrl = "https://api.x.ai/v1",
            ApiKeyUrl = "https://console.x.ai/",
            DocsUrl = "https://docs.x.ai/",
            IsFree = false,
            RequiresKey = true,
            Icon = "❌",
            Category = SgLlmProviderCategory.Frontier,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Tools, SgLlmProviderTag.Reasoning, SgLlmProviderTag.Vision },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true, SupportsReasoningModels = true, SupportsVision = true,
            Notes = "Grok 4 — OpenAI-compatible /v1/chat/completions."
        },

        // ===== Open routing / aggregators =====
        [SgLlmProvider.OpenRouter] = new()
        {
            Provider = SgLlmProvider.OpenRouter,
            Label = "OpenRouter",
            BaseUrl = "https://openrouter.ai/api/v1",
            ApiKeyUrl = "https://openrouter.ai/settings/keys",
            DocsUrl = "https://openrouter.ai/docs",
            IsFree = true,
            RequiresKey = true,
            Icon = "🛣",
            Category = SgLlmProviderCategory.OpenRouting,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Free, SgLlmProviderTag.Vision, SgLlmProviderTag.Tools, SgLlmProviderTag.Reasoning, SgLlmProviderTag.Agentic },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true, SupportsReasoningModels = true, SupportsVision = true,
            FreeTierNotes = "Десятки бесплатных моделей с суффиксом :free.",
            Notes = "Единый каталог и маршрутизация моделей; есть free-модели."
        },
        [SgLlmProvider.TogetherAi] = new()
        {
            Provider = SgLlmProvider.TogetherAi,
            Label = "Together AI",
            BaseUrl = "https://api.together.xyz/v1",
            ApiKeyUrl = "https://api.together.ai/settings/api-keys",
            DocsUrl = "https://docs.together.ai/",
            IsFree = false,
            RequiresKey = true,
            Icon = "🤝",
            Category = SgLlmProviderCategory.OpenRouting,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Tools, SgLlmProviderTag.Vision, SgLlmProviderTag.Embeddings, SgLlmProviderTag.Images },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsEmbeddings = true, SupportsImages = true, SupportsTools = true, SupportsVision = true,
            Notes = "OpenAI-compatible router для open-source моделей."
        },
        [SgLlmProvider.Fireworks] = new()
        {
            Provider = SgLlmProvider.Fireworks,
            Label = "Fireworks AI",
            BaseUrl = "https://api.fireworks.ai/inference/v1",
            ApiKeyUrl = "https://fireworks.ai/account/api-keys",
            DocsUrl = "https://docs.fireworks.ai/",
            IsFree = false,
            RequiresKey = true,
            Icon = "🎆",
            Category = SgLlmProviderCategory.OpenRouting,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Tools, SgLlmProviderTag.Vision, SgLlmProviderTag.Embeddings, SgLlmProviderTag.Images, SgLlmProviderTag.Audio },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsEmbeddings = true, SupportsImages = true, SupportsAudioStt = true,
            SupportsTools = true, SupportsVision = true,
            Notes = "Серверлесс OSS inference, OpenAI-compatible."
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
            Icon = "🤗",
            Category = SgLlmProviderCategory.OpenRouting,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Free, SgLlmProviderTag.Tools, SgLlmProviderTag.Vision, SgLlmProviderTag.Reasoning },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true, SupportsVision = true, SupportsReasoningModels = true,
            FreeTierNotes = "Бесплатный inference-роутер с rate-limit.",
            Notes = "Inference Router, OpenAI-compatible API."
        },
        [SgLlmProvider.Replicate] = new()
        {
            Provider = SgLlmProvider.Replicate,
            Label = "Replicate",
            BaseUrl = "https://api.replicate.com/v1",
            ApiKeyUrl = "https://replicate.com/account/api-tokens",
            DocsUrl = "https://replicate.com/docs",
            IsFree = false,
            RequiresKey = true,
            Icon = "🌀",
            Category = SgLlmProviderCategory.OpenRouting,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Images, SgLlmProviderTag.Audio, SgLlmProviderTag.Vision },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsImages = true, SupportsAudioTts = true, SupportsAudioStt = true, SupportsVision = true,
            Notes = "Каталог моделей с pay-per-second."
        },
        [SgLlmProvider.AiMlApi] = new()
        {
            Provider = SgLlmProvider.AiMlApi,
            Label = "AI/ML API",
            BaseUrl = "https://api.aimlapi.com/v1",
            ApiKeyUrl = "https://aimlapi.com/app",
            DocsUrl = "https://docs.aimlapi.com/",
            IsFree = false,
            RequiresKey = true,
            Icon = "🧠",
            Category = SgLlmProviderCategory.OpenRouting,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Tools, SgLlmProviderTag.Vision },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true, SupportsVision = true,
            Notes = "200+ моделей через единый OpenAI-compatible endpoint."
        },
        [SgLlmProvider.Novita] = new()
        {
            Provider = SgLlmProvider.Novita,
            Label = "Novita AI",
            BaseUrl = "https://api.novita.ai/v3/openai",
            ApiKeyUrl = "https://novita.ai/dashboard/key",
            DocsUrl = "https://novita.ai/docs/guides/llm-api",
            IsFree = false,
            RequiresKey = true,
            Icon = "✨",
            Category = SgLlmProviderCategory.OpenRouting,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Tools, SgLlmProviderTag.Vision, SgLlmProviderTag.Images },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true, SupportsVision = true, SupportsImages = true,
            Notes = "OSS-inference серверлесс, OpenAI-compatible."
        },

        // ===== Fast inference =====
        [SgLlmProvider.Groq] = new()
        {
            Provider = SgLlmProvider.Groq,
            Label = "Groq",
            BaseUrl = "https://api.groq.com/openai/v1",
            ApiKeyUrl = "https://console.groq.com/keys",
            DocsUrl = "https://console.groq.com/docs",
            IsFree = true,
            RequiresKey = true,
            Icon = "⚡",
            Category = SgLlmProviderCategory.FastInference,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Free, SgLlmProviderTag.Tools, SgLlmProviderTag.Reasoning, SgLlmProviderTag.Audio },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true, SupportsReasoningModels = true, SupportsAudioStt = true,
            FreeTierNotes = "Щедрый бесплатный тариф с rate-limit.",
            Notes = "LPU-инференс на сверхвысокой скорости."
        },
        [SgLlmProvider.Cerebras] = new()
        {
            Provider = SgLlmProvider.Cerebras,
            Label = "Cerebras",
            BaseUrl = "https://api.cerebras.ai/v1",
            ApiKeyUrl = "https://cloud.cerebras.ai/",
            DocsUrl = "https://inference-docs.cerebras.ai/",
            IsFree = true,
            RequiresKey = true,
            Icon = "🧬",
            Category = SgLlmProviderCategory.FastInference,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Free, SgLlmProviderTag.Tools, SgLlmProviderTag.Reasoning },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true, SupportsReasoningModels = true,
            FreeTierNotes = "Free-tier на wafer-scale ускорителях.",
            Notes = "Топовая throughput на Llama/Qwen."
        },
        [SgLlmProvider.SambaNova] = new()
        {
            Provider = SgLlmProvider.SambaNova,
            Label = "SambaNova",
            BaseUrl = "https://api.sambanova.ai/v1",
            ApiKeyUrl = "https://cloud.sambanova.ai/",
            DocsUrl = "https://docs.sambanova.ai/",
            IsFree = true,
            RequiresKey = true,
            Icon = "💎",
            Category = SgLlmProviderCategory.FastInference,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Free, SgLlmProviderTag.Tools, SgLlmProviderTag.Reasoning },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true, SupportsReasoningModels = true,
            FreeTierNotes = "Free-tier на RDU.",
            Notes = "RDU-инференс открытых моделей."
        },
        [SgLlmProvider.DeepSeek] = new()
        {
            Provider = SgLlmProvider.DeepSeek,
            Label = "DeepSeek",
            BaseUrl = "https://api.deepseek.com/v1",
            ApiKeyUrl = "https://platform.deepseek.com/api_keys",
            DocsUrl = "https://api-docs.deepseek.com/",
            IsFree = false,
            RequiresKey = true,
            Icon = "🐋",
            Category = SgLlmProviderCategory.FastInference,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Tools, SgLlmProviderTag.Reasoning },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true, SupportsReasoningModels = true,
            Notes = "DeepSeek-V3/R1 — недорогой OpenAI-compatible."
        },
        [SgLlmProvider.Lepton] = new()
        {
            Provider = SgLlmProvider.Lepton,
            Label = "Lepton AI",
            BaseUrl = "https://api.lepton.ai/api/v1",
            ApiKeyUrl = "https://dashboard.lepton.ai/",
            DocsUrl = "https://www.lepton.ai/docs",
            IsFree = false,
            RequiresKey = true,
            Icon = "🪶",
            Category = SgLlmProviderCategory.FastInference,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Tools, SgLlmProviderTag.Vision },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true, SupportsVision = true,
            Notes = "Серверлесс OSS-инференс."
        },
        [SgLlmProvider.DeepInfra] = new()
        {
            Provider = SgLlmProvider.DeepInfra,
            Label = "DeepInfra",
            BaseUrl = "https://api.deepinfra.com/v1/openai",
            ApiKeyUrl = "https://deepinfra.com/dash/api_keys",
            DocsUrl = "https://deepinfra.com/docs",
            IsFree = false,
            RequiresKey = true,
            Icon = "🔻",
            Category = SgLlmProviderCategory.FastInference,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Tools, SgLlmProviderTag.Vision, SgLlmProviderTag.Embeddings, SgLlmProviderTag.Images },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true, SupportsVision = true, SupportsEmbeddings = true, SupportsImages = true,
            Notes = "Pay-per-token серверлесс OSS-моделей."
        },

        // ===== Local =====
        [SgLlmProvider.Ollama] = new()
        {
            Provider = SgLlmProvider.Ollama,
            Label = "Ollama",
            BaseUrl = "http://localhost:11434",
            DocsUrl = "https://ollama.com/",
            IsFree = true,
            RequiresKey = false,
            Icon = "🦙",
            Category = SgLlmProviderCategory.Local,
            Tags = { SgLlmProviderTag.Local, SgLlmProviderTag.Free, SgLlmProviderTag.Tools, SgLlmProviderTag.Vision, SgLlmProviderTag.Embeddings },
            Auth = SgLlmAuthStyle.None,
            ApiStyle = SgLlmApiStyle.OllamaNative,
            SupportsTools = true, SupportsVision = true, SupportsEmbeddings = true,
            Notes = "Локальные модели, ключ не нужен. CORS: OLLAMA_ORIGINS."
        },
        [SgLlmProvider.LmStudio] = new()
        {
            Provider = SgLlmProvider.LmStudio,
            Label = "LM Studio",
            BaseUrl = "http://localhost:1234/v1",
            DocsUrl = "https://lmstudio.ai/docs",
            IsFree = true,
            RequiresKey = false,
            Icon = "🖥",
            Category = SgLlmProviderCategory.Local,
            Tags = { SgLlmProviderTag.Local, SgLlmProviderTag.Free, SgLlmProviderTag.Tools, SgLlmProviderTag.Vision, SgLlmProviderTag.Embeddings },
            Auth = SgLlmAuthStyle.None,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true, SupportsVision = true, SupportsEmbeddings = true,
            Notes = "Локальный OpenAI-compatible server."
        },
        [SgLlmProvider.Vllm] = new()
        {
            Provider = SgLlmProvider.Vllm,
            Label = "vLLM",
            BaseUrl = "http://localhost:8000/v1",
            DocsUrl = "https://docs.vllm.ai/",
            IsFree = true,
            RequiresKey = false,
            Icon = "🐝",
            Category = SgLlmProviderCategory.Local,
            Tags = { SgLlmProviderTag.Local, SgLlmProviderTag.Free, SgLlmProviderTag.Tools, SgLlmProviderTag.Vision },
            Auth = SgLlmAuthStyle.None,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true, SupportsVision = true,
            Notes = "Высокопроизводительный сервер; для браузера запустите с --allow-cors."
        },
        [SgLlmProvider.LlamaCpp] = new()
        {
            Provider = SgLlmProvider.LlamaCpp,
            Label = "llama.cpp",
            BaseUrl = "http://localhost:8080/v1",
            DocsUrl = "https://github.com/ggerganov/llama.cpp/tree/master/examples/server",
            IsFree = true,
            RequiresKey = false,
            Icon = "🐑",
            Category = SgLlmProviderCategory.Local,
            Tags = { SgLlmProviderTag.Local, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.None,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            Notes = "GGUF-сервер. CORS: --api-cors."
        },
        [SgLlmProvider.Jan] = new()
        {
            Provider = SgLlmProvider.Jan,
            Label = "Jan",
            BaseUrl = "http://localhost:1337/v1",
            DocsUrl = "https://jan.ai/docs",
            IsFree = true,
            RequiresKey = false,
            Icon = "📦",
            Category = SgLlmProviderCategory.Local,
            Tags = { SgLlmProviderTag.Local, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.None,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            Notes = "Open-source desktop с OpenAI-compatible сервером."
        },
        [SgLlmProvider.Gpt4All] = new()
        {
            Provider = SgLlmProvider.Gpt4All,
            Label = "GPT4All",
            BaseUrl = "http://localhost:4891/v1",
            DocsUrl = "https://docs.gpt4all.io/",
            IsFree = true,
            RequiresKey = false,
            Icon = "🪂",
            Category = SgLlmProviderCategory.Local,
            Tags = { SgLlmProviderTag.Local, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.None,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            Notes = "Десктопное приложение Nomic с OpenAI-compatible сервером."
        },
        [SgLlmProvider.KoboldCpp] = new()
        {
            Provider = SgLlmProvider.KoboldCpp,
            Label = "KoboldCpp",
            BaseUrl = "http://localhost:5001/v1",
            DocsUrl = "https://github.com/LostRuins/koboldcpp/wiki",
            IsFree = true,
            RequiresKey = false,
            Icon = "🐙",
            Category = SgLlmProviderCategory.Local,
            Tags = { SgLlmProviderTag.Local, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.None,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            Notes = "Запустите с --openai-compatibility, иначе чат-роуты не доступны."
        },
        [SgLlmProvider.OobaboogaTgWebUi] = new()
        {
            Provider = SgLlmProvider.OobaboogaTgWebUi,
            Label = "text-generation-webui",
            BaseUrl = "http://localhost:5000/v1",
            DocsUrl = "https://github.com/oobabooga/text-generation-webui",
            IsFree = true,
            RequiresKey = false,
            Icon = "🧪",
            Category = SgLlmProviderCategory.Local,
            Tags = { SgLlmProviderTag.Local, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.None,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            Notes = "Включите OpenAI extension в Oobabooga."
        },
        [SgLlmProvider.TabbyApi] = new()
        {
            Provider = SgLlmProvider.TabbyApi,
            Label = "TabbyAPI",
            BaseUrl = "http://localhost:5000/v1",
            DocsUrl = "https://github.com/theroyallab/tabbyAPI",
            IsFree = true,
            RequiresKey = false,
            Icon = "🐈",
            Category = SgLlmProviderCategory.Local,
            Tags = { SgLlmProviderTag.Local, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.None,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            Notes = "Локальный сервер exl2; OpenAI-compatible."
        },
        [SgLlmProvider.Llamafile] = new()
        {
            Provider = SgLlmProvider.Llamafile,
            Label = "llamafile",
            BaseUrl = "http://localhost:8080/v1",
            DocsUrl = "https://github.com/Mozilla-Ocho/llamafile",
            IsFree = true,
            RequiresKey = false,
            Icon = "📁",
            Category = SgLlmProviderCategory.Local,
            Tags = { SgLlmProviderTag.Local, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.None,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            Notes = "Mozilla — single-binary сервер. CORS включён по умолчанию."
        },
        [SgLlmProvider.WebLlm] = new()
        {
            Provider = SgLlmProvider.WebLlm,
            Label = "WebLLM",
            BaseUrl = string.Empty,
            DocsUrl = "https://webllm.mlc.ai/",
            IsFree = true,
            RequiresKey = false,
            Icon = "🌐",
            Category = SgLlmProviderCategory.Local,
            Tags = { SgLlmProviderTag.Local, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.None,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            Notes = "MLC WebLLM работает прямо в браузере (WebGPU)."
        },

        // ===== Free =====
        [SgLlmProvider.CloudflareWorkersAi] = new()
        {
            Provider = SgLlmProvider.CloudflareWorkersAi,
            Label = "Cloudflare Workers AI",
            BaseUrl = "https://api.cloudflare.com/client/v4/accounts/{account_id}/ai",
            ApiKeyUrl = "https://dash.cloudflare.com/profile/api-tokens",
            DocsUrl = "https://developers.cloudflare.com/workers-ai/",
            IsFree = true,
            RequiresKey = true,
            Icon = "☁",
            Category = SgLlmProviderCategory.Free,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Free, SgLlmProviderTag.Tools, SgLlmProviderTag.Vision, SgLlmProviderTag.Embeddings, SgLlmProviderTag.Images },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.CloudflareWorkersAi,
            SupportsTools = true, SupportsVision = true, SupportsEmbeddings = true, SupportsImages = true,
            FreeTierNotes = "10 000 Neurons/день бесплатно.",
            Notes = "Замените {account_id} в Base URL на ваш Account ID."
        },
        [SgLlmProvider.GitHubModels] = new()
        {
            Provider = SgLlmProvider.GitHubModels,
            Label = "GitHub Models",
            BaseUrl = "https://models.inference.ai.azure.com",
            ApiKeyUrl = "https://github.com/settings/tokens",
            DocsUrl = "https://docs.github.com/en/github-models",
            IsFree = true,
            RequiresKey = true,
            Icon = "🐱",
            Category = SgLlmProviderCategory.Free,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Free, SgLlmProviderTag.Tools, SgLlmProviderTag.Vision, SgLlmProviderTag.Reasoning },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true, SupportsVision = true, SupportsReasoningModels = true,
            FreeTierNotes = "Бесплатно для разработчиков с GitHub PAT.",
            Notes = "OpenAI-compatible маршрут для каталога Marketplace."
        },
        [SgLlmProvider.Pollinations] = new()
        {
            Provider = SgLlmProvider.Pollinations,
            Label = "Pollinations",
            BaseUrl = "https://text.pollinations.ai/openai",
            DocsUrl = "https://pollinations.ai/",
            IsFree = true,
            RequiresKey = false,
            Icon = "🌸",
            Category = SgLlmProviderCategory.Free,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Free, SgLlmProviderTag.Images },
            Auth = SgLlmAuthStyle.None,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsImages = true,
            FreeTierNotes = "Полностью бесплатно, без ключа.",
            Notes = "Анонимный шлюз; ограничен по rate."
        },
        [SgLlmProvider.GlhfChat] = new()
        {
            Provider = SgLlmProvider.GlhfChat,
            Label = "glhf.chat",
            BaseUrl = "https://glhf.chat/api/openai/v1",
            ApiKeyUrl = "https://glhf.chat/users/settings/api",
            DocsUrl = "https://glhf.chat/",
            IsFree = true,
            RequiresKey = true,
            Icon = "🎮",
            Category = SgLlmProviderCategory.Free,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            FreeTierNotes = "Бесплатно для OSS-моделей.",
            Notes = "OpenAI-compatible router OSS-моделей."
        },
        [SgLlmProvider.Targon] = new()
        {
            Provider = SgLlmProvider.Targon,
            Label = "Targon",
            BaseUrl = "https://api.targon.com/v1",
            DocsUrl = "https://docs.targon.com/",
            IsFree = true,
            RequiresKey = false,
            Icon = "🎯",
            Category = SgLlmProviderCategory.Free,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.None,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            FreeTierNotes = "Public-бесплатный путь без ключа.",
            Notes = "Bittensor-роутер."
        },
        [SgLlmProvider.Chutes] = new()
        {
            Provider = SgLlmProvider.Chutes,
            Label = "Chutes",
            BaseUrl = "https://chutes-api.chutes.ai/v1",
            ApiKeyUrl = "https://chutes.ai/app/api",
            DocsUrl = "https://docs.chutes.ai/",
            IsFree = true,
            RequiresKey = true,
            Icon = "🪂",
            Category = SgLlmProviderCategory.Free,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            FreeTierNotes = "Бесплатные модели после регистрации.",
            Notes = "Open inference на Bittensor."
        },

        // ===== Russian =====
        [SgLlmProvider.GigaGpt] = new()
        {
            Provider = SgLlmProvider.GigaGpt,
            Label = "GigaGPT / GigaChat",
            BaseUrl = "https://gigachat.devices.sberbank.ru/api/v1",
            ApiKeyUrl = "https://developers.sber.ru/portal/products/gigachat-api",
            DocsUrl = "https://developers.sber.ru/docs/ru/gigachat/models/main",
            IsFree = false,
            RequiresKey = true,
            Icon = "🇷🇺",
            Category = SgLlmProviderCategory.Russian,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Russian, SgLlmProviderTag.Tools, SgLlmProviderTag.Embeddings, SgLlmProviderTag.Images },
            Auth = SgLlmAuthStyle.OAuth,
            ApiStyle = SgLlmApiStyle.GigaChat,
            SupportsTools = true, SupportsEmbeddings = true, SupportsImages = true,
            RegionsNotes = "RU only.",
            Notes = "GigaChat/GigaGPT-compatible chat/completions endpoint."
        },
        [SgLlmProvider.YandexGpt] = new()
        {
            Provider = SgLlmProvider.YandexGpt,
            Label = "YandexGPT",
            BaseUrl = "https://llm.api.cloud.yandex.net/foundationModels/v1",
            ApiKeyUrl = "https://console.cloud.yandex.ru/",
            DocsUrl = "https://yandex.cloud/ru/docs/foundation-models/",
            IsFree = false,
            RequiresKey = true,
            Icon = "🇷🇺",
            Category = SgLlmProviderCategory.Russian,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Russian, SgLlmProviderTag.Embeddings },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsEmbeddings = true,
            RegionsNotes = "RU only.",
            Notes = "Yandex Cloud Foundation Models."
        },

        // ===== Specialty =====
        [SgLlmProvider.Cohere] = new()
        {
            Provider = SgLlmProvider.Cohere,
            Label = "Cohere",
            BaseUrl = "https://api.cohere.com/v2",
            ApiKeyUrl = "https://dashboard.cohere.com/api-keys",
            DocsUrl = "https://docs.cohere.com/",
            IsFree = true,
            RequiresKey = true,
            Icon = "🟣",
            Category = SgLlmProviderCategory.Specialty,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Tools, SgLlmProviderTag.Embeddings, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.CohereV2,
            SupportsTools = true, SupportsEmbeddings = true,
            FreeTierNotes = "Trial-ключ для разработки.",
            Notes = "Command R+/R; rerank, embed."
        },
        [SgLlmProvider.Mistral] = new()
        {
            Provider = SgLlmProvider.Mistral,
            Label = "Mistral AI",
            BaseUrl = "https://api.mistral.ai/v1",
            ApiKeyUrl = "https://console.mistral.ai/api-keys/",
            DocsUrl = "https://docs.mistral.ai/",
            IsFree = false,
            RequiresKey = true,
            Icon = "🌬",
            Category = SgLlmProviderCategory.Specialty,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Tools, SgLlmProviderTag.Vision, SgLlmProviderTag.Embeddings },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true, SupportsVision = true, SupportsEmbeddings = true,
            Notes = "La Plateforme — chat, embed, code."
        },
        [SgLlmProvider.Perplexity] = new()
        {
            Provider = SgLlmProvider.Perplexity,
            Label = "Perplexity",
            BaseUrl = "https://api.perplexity.ai",
            ApiKeyUrl = "https://www.perplexity.ai/settings/api",
            DocsUrl = "https://docs.perplexity.ai/",
            IsFree = false,
            RequiresKey = true,
            Icon = "🔎",
            Category = SgLlmProviderCategory.Specialty,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Tools, SgLlmProviderTag.Reasoning },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true, SupportsReasoningModels = true,
            Notes = "Sonar — online-модели с цитатами."
        },
        [SgLlmProvider.VoyageAi] = new()
        {
            Provider = SgLlmProvider.VoyageAi,
            Label = "Voyage AI",
            BaseUrl = "https://api.voyageai.com/v1",
            ApiKeyUrl = "https://dash.voyageai.com/api-keys",
            DocsUrl = "https://docs.voyageai.com/",
            IsFree = true,
            RequiresKey = true,
            Icon = "⛵",
            Category = SgLlmProviderCategory.Specialty,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Embeddings, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsEmbeddings = true,
            FreeTierNotes = "200M токенов/мес бесплатно.",
            Notes = "Лучшие embeddings и reranker для RAG."
        },
        [SgLlmProvider.JinaAi] = new()
        {
            Provider = SgLlmProvider.JinaAi,
            Label = "Jina AI",
            BaseUrl = "https://api.jina.ai/v1",
            ApiKeyUrl = "https://jina.ai/?sui=apikey",
            DocsUrl = "https://jina.ai/embeddings/",
            IsFree = true,
            RequiresKey = true,
            Icon = "🌿",
            Category = SgLlmProviderCategory.Specialty,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Embeddings, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsEmbeddings = true,
            FreeTierNotes = "1M токенов бесплатно по trial-ключу.",
            Notes = "Embeddings + reranker."
        },
        [SgLlmProvider.Nomic] = new()
        {
            Provider = SgLlmProvider.Nomic,
            Label = "Nomic Atlas",
            BaseUrl = "https://api-atlas.nomic.ai/v1",
            ApiKeyUrl = "https://atlas.nomic.ai/cli-login",
            DocsUrl = "https://docs.nomic.ai/",
            IsFree = true,
            RequiresKey = true,
            Icon = "🗺",
            Category = SgLlmProviderCategory.Specialty,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Embeddings, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsEmbeddings = true,
            FreeTierNotes = "Бесплатно для исследователей.",
            Notes = "nomic-embed-text — open embeddings."
        },
        [SgLlmProvider.AssemblyAi] = new()
        {
            Provider = SgLlmProvider.AssemblyAi,
            Label = "AssemblyAI",
            BaseUrl = "https://api.assemblyai.com/v2",
            ApiKeyUrl = "https://www.assemblyai.com/app",
            DocsUrl = "https://www.assemblyai.com/docs",
            IsFree = true,
            RequiresKey = true,
            Icon = "🎙",
            Category = SgLlmProviderCategory.Specialty,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Audio, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsAudioStt = true,
            FreeTierNotes = "Бесплатные часы транскрипции.",
            Notes = "Speech-to-text и LeMUR."
        },
        [SgLlmProvider.Deepgram] = new()
        {
            Provider = SgLlmProvider.Deepgram,
            Label = "Deepgram",
            BaseUrl = "https://api.deepgram.com/v1",
            ApiKeyUrl = "https://console.deepgram.com/",
            DocsUrl = "https://developers.deepgram.com/",
            IsFree = true,
            RequiresKey = true,
            Icon = "🎧",
            Category = SgLlmProviderCategory.Specialty,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Audio, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsAudioStt = true, SupportsAudioTts = true,
            FreeTierNotes = "$200 кредитов на старт.",
            Notes = "Realtime STT и TTS (Aura)."
        },
        [SgLlmProvider.ElevenLabs] = new()
        {
            Provider = SgLlmProvider.ElevenLabs,
            Label = "ElevenLabs",
            BaseUrl = "https://api.elevenlabs.io/v1",
            ApiKeyUrl = "https://elevenlabs.io/app/settings/api-keys",
            DocsUrl = "https://elevenlabs.io/docs",
            IsFree = true,
            RequiresKey = true,
            Icon = "🎤",
            Category = SgLlmProviderCategory.Specialty,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Audio, SgLlmProviderTag.Free },
            Auth = SgLlmAuthStyle.XApiKey,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsAudioTts = true, SupportsAudioStt = true,
            FreeTierNotes = "10 000 символов TTS/мес.",
            Notes = "Лучший TTS, голосовое клонирование."
        },
        [SgLlmProvider.OpenAiCompatibleCustom] = new()
        {
            Provider = SgLlmProvider.OpenAiCompatibleCustom,
            Label = "Custom OpenAI-compatible",
            BaseUrl = string.Empty,
            DocsUrl = null,
            IsFree = false,
            RequiresKey = true,
            Icon = "🛠",
            Category = SgLlmProviderCategory.Specialty,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Specialty },
            Auth = SgLlmAuthStyle.Bearer,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            Notes = "Любой собственный endpoint с /chat/completions и /models."
        },

        // ===== Azure =====
        [SgLlmProvider.AzureOpenAi] = new()
        {
            Provider = SgLlmProvider.AzureOpenAi,
            Label = "Azure OpenAI",
            BaseUrl = "https://{resource}.openai.azure.com",
            ApiKeyUrl = "https://portal.azure.com/",
            DocsUrl = "https://learn.microsoft.com/azure/ai-services/openai/",
            IsFree = false,
            RequiresKey = true,
            Icon = "🅰",
            Category = SgLlmProviderCategory.Azure,
            Tags = { SgLlmProviderTag.Cloud, SgLlmProviderTag.Tools, SgLlmProviderTag.Vision, SgLlmProviderTag.Reasoning, SgLlmProviderTag.Embeddings, SgLlmProviderTag.Images },
            Auth = SgLlmAuthStyle.AzureApiKey,
            ApiStyle = SgLlmApiStyle.OpenAiChat,
            SupportsTools = true, SupportsVision = true, SupportsReasoningModels = true,
            SupportsEmbeddings = true, SupportsImages = true,
            Notes = "Замените {resource} в Base URL; нужно указать deployment и api-version."
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
        provider is SgLlmProvider.OpenAiCompatible or SgLlmProvider.OpenRouter or SgLlmProvider.LmStudio
            or SgLlmProvider.HuggingFace or SgLlmProvider.GigaGpt or SgLlmProvider.Ollama
            or SgLlmProvider.TogetherAi or SgLlmProvider.Fireworks or SgLlmProvider.DeepSeek
            or SgLlmProvider.Groq or SgLlmProvider.Cerebras or SgLlmProvider.SambaNova
            or SgLlmProvider.Mistral or SgLlmProvider.XAi or SgLlmProvider.Perplexity
            or SgLlmProvider.Lepton or SgLlmProvider.DeepInfra or SgLlmProvider.AiMlApi
            or SgLlmProvider.Novita or SgLlmProvider.Vllm or SgLlmProvider.LlamaCpp
            or SgLlmProvider.Jan or SgLlmProvider.Gpt4All or SgLlmProvider.KoboldCpp
            or SgLlmProvider.OobaboogaTgWebUi or SgLlmProvider.TabbyApi or SgLlmProvider.Llamafile
            or SgLlmProvider.GitHubModels or SgLlmProvider.GlhfChat or SgLlmProvider.Chutes
            or SgLlmProvider.Targon or SgLlmProvider.OpenAiCompatibleCustom or SgLlmProvider.AzureOpenAi;

    public static bool SupportsTopKMinP(SgLlmProvider provider) =>
        provider is SgLlmProvider.OpenRouter or SgLlmProvider.Ollama or SgLlmProvider.Anthropic
            or SgLlmProvider.LmStudio or SgLlmProvider.Google or SgLlmProvider.Vllm
            or SgLlmProvider.LlamaCpp or SgLlmProvider.Jan or SgLlmProvider.Gpt4All
            or SgLlmProvider.KoboldCpp or SgLlmProvider.OobaboogaTgWebUi or SgLlmProvider.TabbyApi
            or SgLlmProvider.Llamafile or SgLlmProvider.Mistral or SgLlmProvider.Cohere;

    public static bool SupportsReasoning(SgLlmProvider provider) =>
        provider is SgLlmProvider.OpenAiCompatible or SgLlmProvider.OpenRouter or SgLlmProvider.HuggingFace
            or SgLlmProvider.GigaGpt or SgLlmProvider.DeepSeek or SgLlmProvider.XAi
            or SgLlmProvider.Groq or SgLlmProvider.Cerebras or SgLlmProvider.SambaNova
            or SgLlmProvider.Google or SgLlmProvider.Perplexity or SgLlmProvider.GitHubModels
            or SgLlmProvider.AzureOpenAi or SgLlmProvider.TogetherAi or SgLlmProvider.Fireworks;

    public static bool SupportsServiceTier(SgLlmProvider provider) =>
        provider is SgLlmProvider.OpenAiCompatible or SgLlmProvider.OpenRouter or SgLlmProvider.Anthropic
            or SgLlmProvider.AzureOpenAi;

    public static SgLlmProvider DetectProvider(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl)) return SgLlmProvider.None;
        var url = baseUrl.Trim().ToLowerInvariant();

        // Cloud providers (order matters — most specific first)
        if (url.Contains("openrouter.ai")) return SgLlmProvider.OpenRouter;
        if (url.Contains("api.anthropic.com")) return SgLlmProvider.Anthropic;
        if (url.Contains("generativelanguage.googleapis.com") || url.Contains("googleapis.com/google.ai")) return SgLlmProvider.Google;
        if (url.Contains("api.x.ai")) return SgLlmProvider.XAi;
        if (url.Contains("api.together.xyz") || url.Contains("together.ai")) return SgLlmProvider.TogetherAi;
        if (url.Contains("fireworks.ai")) return SgLlmProvider.Fireworks;
        if (url.Contains("huggingface.co") || url.Contains("router.huggingface")) return SgLlmProvider.HuggingFace;
        if (url.Contains("replicate.com")) return SgLlmProvider.Replicate;
        if (url.Contains("aimlapi.com")) return SgLlmProvider.AiMlApi;
        if (url.Contains("novita.ai")) return SgLlmProvider.Novita;
        if (url.Contains("api.groq.com")) return SgLlmProvider.Groq;
        if (url.Contains("api.cerebras")) return SgLlmProvider.Cerebras;
        if (url.Contains("sambanova.ai")) return SgLlmProvider.SambaNova;
        if (url.Contains("api.deepseek.com")) return SgLlmProvider.DeepSeek;
        if (url.Contains("lepton.ai")) return SgLlmProvider.Lepton;
        if (url.Contains("deepinfra.com")) return SgLlmProvider.DeepInfra;

        // Free / specialty cloud
        if (url.Contains("cloudflare.com") && url.Contains("/ai")) return SgLlmProvider.CloudflareWorkersAi;
        if (url.Contains("models.inference.ai.azure.com") || url.Contains("github") && url.Contains("models")) return SgLlmProvider.GitHubModels;
        if (url.Contains("pollinations")) return SgLlmProvider.Pollinations;
        if (url.Contains("glhf.chat")) return SgLlmProvider.GlhfChat;
        if (url.Contains("targon.com")) return SgLlmProvider.Targon;
        if (url.Contains("chutes.ai")) return SgLlmProvider.Chutes;

        // Russian
        if (url.Contains("gigachat") || url.Contains("sberbank.ru")) return SgLlmProvider.GigaGpt;
        if (url.Contains("yandex.cloud") || url.Contains("yandex.net") || url.Contains("api.cloud.yandex")) return SgLlmProvider.YandexGpt;

        // Specialty
        if (url.Contains("api.cohere")) return SgLlmProvider.Cohere;
        if (url.Contains("mistral.ai")) return SgLlmProvider.Mistral;
        if (url.Contains("perplexity.ai")) return SgLlmProvider.Perplexity;
        if (url.Contains("voyageai.com")) return SgLlmProvider.VoyageAi;
        if (url.Contains("jina.ai")) return SgLlmProvider.JinaAi;
        if (url.Contains("nomic.ai") || url.Contains("api-atlas.nomic")) return SgLlmProvider.Nomic;
        if (url.Contains("assemblyai.com")) return SgLlmProvider.AssemblyAi;
        if (url.Contains("deepgram.com")) return SgLlmProvider.Deepgram;
        if (url.Contains("elevenlabs.io")) return SgLlmProvider.ElevenLabs;

        // Azure
        if (url.Contains(".openai.azure.com")) return SgLlmProvider.AzureOpenAi;

        // Local — by well-known port mappings
        if (url.Contains("localhost:11434") || url.Contains("127.0.0.1:11434")) return SgLlmProvider.Ollama;
        if (url.Contains("localhost:1234") || url.Contains("127.0.0.1:1234")) return SgLlmProvider.LmStudio;
        if (url.Contains("localhost:8000") || url.Contains("127.0.0.1:8000")) return SgLlmProvider.Vllm;
        if (url.Contains("localhost:8080") || url.Contains("127.0.0.1:8080")) return SgLlmProvider.LlamaCpp;
        if (url.Contains("localhost:1337") || url.Contains("127.0.0.1:1337")) return SgLlmProvider.Jan;
        if (url.Contains("localhost:4891") || url.Contains("127.0.0.1:4891")) return SgLlmProvider.Gpt4All;
        if (url.Contains("localhost:5001") || url.Contains("127.0.0.1:5001")) return SgLlmProvider.KoboldCpp;
        if (url.Contains("localhost:5000") || url.Contains("127.0.0.1:5000")) return SgLlmProvider.OobaboogaTgWebUi;

        // Frontier last (catches generic api.openai.com)
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

        if ((provider == SgLlmProvider.OpenAiCompatible || provider == SgLlmProvider.LmStudio
                || provider == SgLlmProvider.HuggingFace || provider == SgLlmProvider.GigaGpt
                || provider == SgLlmProvider.TogetherAi || provider == SgLlmProvider.Mistral
                || provider == SgLlmProvider.DeepSeek || provider == SgLlmProvider.XAi
                || provider == SgLlmProvider.AiMlApi
                || provider == SgLlmProvider.Vllm || provider == SgLlmProvider.LlamaCpp
                || provider == SgLlmProvider.Jan || provider == SgLlmProvider.Gpt4All
                || provider == SgLlmProvider.KoboldCpp || provider == SgLlmProvider.OobaboogaTgWebUi
                || provider == SgLlmProvider.TabbyApi || provider == SgLlmProvider.Llamafile
                || provider == SgLlmProvider.Lepton || provider == SgLlmProvider.Chutes
                || provider == SgLlmProvider.YandexGpt || provider == SgLlmProvider.JinaAi
                || provider == SgLlmProvider.VoyageAi || provider == SgLlmProvider.Nomic
                || provider == SgLlmProvider.OpenAiCompatibleCustom)
            && !url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            && !url.EndsWith("/openai", StringComparison.OrdinalIgnoreCase))
        {
            if (provider == SgLlmProvider.OpenAiCompatible && url.Contains("api.openai.com", StringComparison.OrdinalIgnoreCase)) return "https://api.openai.com/v1";
            if (provider == SgLlmProvider.LmStudio && (url.Contains("localhost:1234", StringComparison.OrdinalIgnoreCase) || url.Contains("127.0.0.1:1234", StringComparison.OrdinalIgnoreCase))) return url + "/v1";
            if (provider == SgLlmProvider.HuggingFace && url.Contains("huggingface", StringComparison.OrdinalIgnoreCase)) return "https://router.huggingface.co/v1";
            if (provider == SgLlmProvider.GigaGpt && (url.Contains("gigachat", StringComparison.OrdinalIgnoreCase) || url.Contains("sberbank.ru", StringComparison.OrdinalIgnoreCase))) return "https://gigachat.devices.sberbank.ru/api/v1";
        }

        // Provider-specific endpoint normalization
        if (provider == SgLlmProvider.Groq && !url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) return "https://api.groq.com/openai/v1";
        if (provider == SgLlmProvider.Cerebras && !url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase) && url.Contains("cerebras", StringComparison.OrdinalIgnoreCase)) return "https://api.cerebras.ai/v1";
        if (provider == SgLlmProvider.SambaNova && !url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) return "https://api.sambanova.ai/v1";
        if (provider == SgLlmProvider.Fireworks && url.Contains("fireworks", StringComparison.OrdinalIgnoreCase) && !url.Contains("/inference/v1")) return "https://api.fireworks.ai/inference/v1";
        if (provider == SgLlmProvider.DeepInfra && url.Contains("deepinfra", StringComparison.OrdinalIgnoreCase) && !url.Contains("/v1/openai")) return "https://api.deepinfra.com/v1/openai";
        if (provider == SgLlmProvider.Novita && url.Contains("novita", StringComparison.OrdinalIgnoreCase) && !url.Contains("/v3/openai")) return "https://api.novita.ai/v3/openai";
        if (provider == SgLlmProvider.Replicate && !url.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) return "https://api.replicate.com/v1";

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

        // --- Stage 3.5: fallback model lists for newly added providers ---
        SgLlmProvider.Google => new()
        {
            Model("gemini-2.5-pro", "Gemini 2.5 Pro", provider, true, true, true, true, 2_000_000, true),
            Model("gemini-2.5-flash", "Gemini 2.5 Flash", provider, true, true, true, true, 1_000_000, true, true),
            Model("gemini-2.5-flash-lite", "Gemini 2.5 Flash Lite", provider, true, true, true, false, 1_000_000, false, true),
            Model("gemini-2.0-flash", "Gemini 2.0 Flash", provider, true, true, true, false, 1_048_576, false, true),
            Model("text-embedding-005", "text-embedding-005 (embed)", provider, false, false, false, false, 0, false, true)
        },
        SgLlmProvider.XAi => new()
        {
            Model("grok-4", "Grok 4", provider, true, true, true, true, 256_000, true),
            Model("grok-4-fast", "Grok 4 Fast", provider, true, true, true, true, 256_000, true),
            Model("grok-3", "Grok 3", provider, false, true, true, true, 131_072, false),
            Model("grok-2-vision", "Grok 2 Vision", provider, true, true, true, false, 32_768, false)
        },
        SgLlmProvider.TogetherAi => new()
        {
            Model("meta-llama/Llama-3.3-70B-Instruct-Turbo", "Llama 3.3 70B Turbo", provider, false, true, true, false, 131_072, true),
            Model("Qwen/Qwen2.5-72B-Instruct-Turbo", "Qwen2.5 72B Turbo", provider, false, true, true, false, 131_072, true),
            Model("deepseek-ai/DeepSeek-V3", "DeepSeek V3", provider, false, true, true, true, 131_072, true),
            Model("mistralai/Mixtral-8x22B-Instruct-v0.1", "Mixtral 8x22B", provider, false, true, true, false, 65_536, false)
        },
        SgLlmProvider.Fireworks => new()
        {
            Model("accounts/fireworks/models/llama-v3p3-70b-instruct", "Llama 3.3 70B", provider, false, true, true, false, 131_072, true),
            Model("accounts/fireworks/models/qwen2p5-72b-instruct", "Qwen 2.5 72B", provider, false, true, true, false, 32_768, true),
            Model("accounts/fireworks/models/deepseek-v3", "DeepSeek V3", provider, false, true, true, true, 131_072, true),
            Model("accounts/fireworks/models/mixtral-8x22b-instruct", "Mixtral 8x22B", provider, false, true, true, false, 65_536, false)
        },
        SgLlmProvider.Replicate => new()
        {
            Model("meta/llama-3-70b-instruct", "Llama 3 70B", provider, false, true, false, false, 8_000, true),
            Model("mistralai/mixtral-8x7b-instruct-v0.1", "Mixtral 8x7B", provider, false, true, false, false, 32_000, true),
            Model("black-forest-labs/flux-1.1-pro", "Flux 1.1 Pro (image)", provider, false, false, false, false, 0, true),
            Model("openai/whisper", "Whisper (STT)", provider, false, false, false, false, 0, false)
        },
        SgLlmProvider.AiMlApi => new()
        {
            Model("gpt-4o", "GPT-4o (via AI/ML)", provider, true, true, true, false, 128_000, true),
            Model("claude-3-5-sonnet-20241022", "Claude 3.5 Sonnet (via AI/ML)", provider, true, true, true, false, 200_000, true),
            Model("meta-llama/Llama-3.3-70B-Instruct-Turbo", "Llama 3.3 70B Turbo", provider, false, true, true, false, 131_072, false),
            Model("deepseek-ai/DeepSeek-V3", "DeepSeek V3", provider, false, true, true, true, 131_072, false)
        },
        SgLlmProvider.Novita => new()
        {
            Model("meta-llama/llama-3.3-70b-instruct", "Llama 3.3 70B", provider, false, true, true, false, 131_072, true),
            Model("qwen/qwen-2.5-72b-instruct", "Qwen 2.5 72B", provider, false, true, true, false, 131_072, true),
            Model("deepseek/deepseek-v3", "DeepSeek V3", provider, false, true, true, true, 131_072, true)
        },
        SgLlmProvider.Groq => new()
        {
            Model("llama-3.3-70b-versatile", "Llama 3.3 70B Versatile", provider, false, true, true, false, 131_072, true, true),
            Model("llama-3.1-8b-instant", "Llama 3.1 8B Instant", provider, false, true, true, false, 131_072, true, true),
            Model("mixtral-8x7b-32768", "Mixtral 8x7B", provider, false, true, true, false, 32_768, false, true),
            Model("deepseek-r1-distill-llama-70b", "DeepSeek R1 Distill 70B", provider, false, true, true, true, 131_072, true, true),
            Model("whisper-large-v3", "Whisper Large v3 (STT)", provider, false, false, false, false, 0, false, true)
        },
        SgLlmProvider.Cerebras => new()
        {
            Model("llama3.3-70b", "Llama 3.3 70B", provider, false, true, true, false, 131_072, true, true),
            Model("llama-4-scout-17b-16e-instruct", "Llama 4 Scout 17B", provider, true, true, true, false, 131_072, true, true),
            Model("qwen-3-32b", "Qwen 3 32B", provider, false, true, true, true, 131_072, true, true),
            Model("deepseek-r1-distill-llama-70b", "DeepSeek R1 Distill 70B", provider, false, true, true, true, 131_072, false, true)
        },
        SgLlmProvider.SambaNova => new()
        {
            Model("Meta-Llama-3.3-70B-Instruct", "Llama 3.3 70B", provider, false, true, true, false, 131_072, true, true),
            Model("Meta-Llama-3.1-405B-Instruct", "Llama 3.1 405B", provider, false, true, true, false, 16_384, true, true),
            Model("Qwen2.5-72B-Instruct", "Qwen 2.5 72B", provider, false, true, true, false, 32_768, false, true),
            Model("DeepSeek-R1-Distill-Llama-70B", "DeepSeek R1 70B", provider, false, true, true, true, 32_768, false, true)
        },
        SgLlmProvider.DeepSeek => new()
        {
            Model("deepseek-chat", "DeepSeek Chat (V3)", provider, false, true, true, false, 65_536, true),
            Model("deepseek-reasoner", "DeepSeek Reasoner (R1)", provider, false, true, true, true, 65_536, true)
        },
        SgLlmProvider.Lepton => new()
        {
            Model("llama3-1-405b", "Llama 3.1 405B", provider, false, true, true, false, 131_072, true),
            Model("qwen2-5-72b", "Qwen 2.5 72B", provider, false, true, true, false, 131_072, false),
            Model("mistral-large", "Mistral Large", provider, false, true, true, false, 32_768, false)
        },
        SgLlmProvider.DeepInfra => new()
        {
            Model("meta-llama/Llama-3.3-70B-Instruct", "Llama 3.3 70B", provider, false, true, true, false, 131_072, true),
            Model("Qwen/Qwen2.5-72B-Instruct", "Qwen 2.5 72B", provider, false, true, true, false, 32_768, true),
            Model("deepseek-ai/DeepSeek-V3", "DeepSeek V3", provider, false, true, true, true, 131_072, true)
        },

        // Local — defaults assume a generic /v1/models response will fill these in;
        // until then we surface the most common loadable IDs.
        SgLlmProvider.Vllm => new()
        {
            Model("meta-llama/Meta-Llama-3.1-8B-Instruct", "Llama 3.1 8B", provider, false, true, false, false, 8_192, true, true),
            Model("Qwen/Qwen2.5-7B-Instruct", "Qwen 2.5 7B", provider, false, true, false, false, 32_768, true, true)
        },
        SgLlmProvider.LlamaCpp => new()
        {
            Model("local-model", "llama.cpp loaded model", provider, false, false, false, false, 0, true, true)
        },
        SgLlmProvider.Jan => new()
        {
            Model("llama3.2-3b-instruct", "Llama 3.2 3B", provider, false, true, false, false, 8_192, true, true),
            Model("qwen2.5-7b-instruct", "Qwen 2.5 7B", provider, false, true, false, false, 32_768, true, true)
        },
        SgLlmProvider.Gpt4All => new()
        {
            Model("Llama 3.2 3B Instruct", "Llama 3.2 3B", provider, false, false, false, false, 8_192, true, true),
            Model("Phi-3 Mini Instruct", "Phi 3 Mini", provider, false, false, false, false, 4_096, false, true)
        },
        SgLlmProvider.KoboldCpp => new()
        {
            Model("local-model", "KoboldCpp loaded model", provider, false, false, false, false, 0, true, true)
        },
        SgLlmProvider.OobaboogaTgWebUi => new()
        {
            Model("loaded-model", "Oobabooga loaded model", provider, false, false, false, false, 0, true, true)
        },
        SgLlmProvider.TabbyApi => new()
        {
            Model("loaded-exl2-model", "TabbyAPI loaded exl2", provider, false, false, false, false, 0, true, true)
        },
        SgLlmProvider.Llamafile => new()
        {
            Model("LLaMA_CPP", "llamafile loaded model", provider, false, false, false, false, 0, true, true)
        },
        SgLlmProvider.WebLlm => new()
        {
            Model("Llama-3.1-8B-Instruct-q4f32_1-MLC", "Llama 3.1 8B (WebGPU)", provider, false, false, false, false, 8_192, true, true),
            Model("Phi-3.5-mini-instruct-q4f32_1-MLC", "Phi-3.5 Mini (WebGPU)", provider, false, false, false, false, 4_096, false, true)
        },

        // Free / specialty
        SgLlmProvider.CloudflareWorkersAi => new()
        {
            Model("@cf/meta/llama-3.1-8b-instruct", "Llama 3.1 8B", provider, false, false, true, false, 8_192, true, true),
            Model("@cf/qwen/qwen1.5-14b-chat-awq", "Qwen 1.5 14B", provider, false, false, false, false, 32_768, false, true),
            Model("@cf/openai/whisper", "Whisper (STT)", provider, false, false, false, false, 0, false, true),
            Model("@cf/baai/bge-large-en-v1.5", "BGE Large EN (embed)", provider, false, false, false, false, 0, false, true)
        },
        SgLlmProvider.GitHubModels => new()
        {
            Model("gpt-4o", "GPT-4o", provider, true, true, true, false, 128_000, true, true),
            Model("gpt-4o-mini", "GPT-4o mini", provider, true, true, true, false, 128_000, true, true),
            Model("Phi-3.5-MoE-instruct", "Phi 3.5 MoE", provider, false, false, true, false, 131_072, false, true),
            Model("Meta-Llama-3.1-70B-Instruct", "Llama 3.1 70B", provider, false, true, true, false, 131_072, false, true)
        },
        SgLlmProvider.Pollinations => new()
        {
            Model("openai", "OpenAI (proxy)", provider, false, false, false, false, 0, true, true),
            Model("mistral", "Mistral", provider, false, false, false, false, 0, false, true),
            Model("llama", "Llama 3.x", provider, false, false, false, false, 0, false, true)
        },
        SgLlmProvider.GlhfChat => new()
        {
            Model("hf:meta-llama/Llama-3.3-70B-Instruct", "Llama 3.3 70B", provider, false, true, false, false, 131_072, true, true),
            Model("hf:Qwen/Qwen2.5-72B-Instruct", "Qwen 2.5 72B", provider, false, true, false, false, 32_768, false, true)
        },
        SgLlmProvider.Targon => new()
        {
            Model("meta-llama/Llama-3.1-8B-Instruct", "Llama 3.1 8B", provider, false, false, false, false, 8_192, true, true)
        },
        SgLlmProvider.Chutes => new()
        {
            Model("deepseek-ai/DeepSeek-V3", "DeepSeek V3 (free)", provider, false, true, false, true, 65_536, true, true),
            Model("meta-llama/Llama-3.3-70B-Instruct", "Llama 3.3 70B (free)", provider, false, true, false, false, 131_072, false, true)
        },

        // Russian
        SgLlmProvider.YandexGpt => new()
        {
            Model("yandexgpt", "YandexGPT", provider, false, false, false, false, 8_000, true),
            Model("yandexgpt-lite", "YandexGPT Lite", provider, false, false, false, false, 8_000, true),
            Model("yandexgpt-32k", "YandexGPT 32k", provider, false, false, false, false, 32_000, false),
            Model("text-search-doc", "Yandex Embed Doc", provider, false, false, false, false, 0, false)
        },

        // Specialty
        SgLlmProvider.Cohere => new()
        {
            Model("command-r-plus", "Command R+", provider, false, true, true, false, 128_000, true),
            Model("command-r", "Command R", provider, false, true, true, false, 128_000, true, true),
            Model("command-r7b", "Command R7B", provider, false, true, true, false, 128_000, false, true),
            Model("embed-english-v3.0", "Embed English v3 (embed)", provider, false, false, false, false, 0, false),
            Model("rerank-english-v3.0", "Rerank English v3", provider, false, false, false, false, 0, false)
        },
        SgLlmProvider.Mistral => new()
        {
            Model("mistral-large-latest", "Mistral Large", provider, false, true, true, false, 131_072, true),
            Model("mistral-medium-latest", "Mistral Medium", provider, true, true, true, false, 131_072, true),
            Model("mistral-small-latest", "Mistral Small", provider, false, true, true, false, 131_072, true),
            Model("pixtral-large-latest", "Pixtral Large", provider, true, true, true, false, 131_072, false),
            Model("codestral-latest", "Codestral", provider, false, true, true, false, 32_768, false),
            Model("mistral-embed", "Mistral Embed", provider, false, false, false, false, 0, false)
        },
        SgLlmProvider.Perplexity => new()
        {
            Model("sonar-pro", "Sonar Pro", provider, false, false, false, false, 200_000, true),
            Model("sonar", "Sonar", provider, false, false, false, false, 127_072, true),
            Model("sonar-reasoning", "Sonar Reasoning", provider, false, false, false, true, 127_072, true),
            Model("sonar-reasoning-pro", "Sonar Reasoning Pro", provider, false, false, false, true, 200_000, false)
        },
        SgLlmProvider.VoyageAi => new()
        {
            Model("voyage-3", "Voyage 3 (embed)", provider, false, false, false, false, 32_000, true, true),
            Model("voyage-3-lite", "Voyage 3 Lite", provider, false, false, false, false, 32_000, true, true),
            Model("voyage-code-3", "Voyage Code 3", provider, false, false, false, false, 32_000, false, true),
            Model("rerank-2", "Voyage Rerank 2", provider, false, false, false, false, 16_000, false, true)
        },
        SgLlmProvider.JinaAi => new()
        {
            Model("jina-embeddings-v3", "Jina Embeddings v3", provider, false, false, false, false, 8_192, true, true),
            Model("jina-clip-v2", "Jina CLIP v2 (multimodal)", provider, true, false, false, false, 8_192, false, true),
            Model("jina-reranker-v2-base-multilingual", "Jina Reranker v2", provider, false, false, false, false, 1_024, false, true)
        },
        SgLlmProvider.Nomic => new()
        {
            Model("nomic-embed-text-v1.5", "nomic-embed-text v1.5", provider, false, false, false, false, 8_192, true, true),
            Model("nomic-embed-text-v1", "nomic-embed-text v1", provider, false, false, false, false, 2_048, false, true),
            Model("nomic-embed-vision-v1.5", "nomic-embed-vision v1.5", provider, true, false, false, false, 0, false, true)
        },
        SgLlmProvider.AssemblyAi => new()
        {
            Model("best", "AssemblyAI Best", provider, false, false, false, false, 0, true, true),
            Model("nano", "AssemblyAI Nano", provider, false, false, false, false, 0, false, true)
        },
        SgLlmProvider.Deepgram => new()
        {
            Model("nova-3", "Nova-3 (STT)", provider, false, false, false, false, 0, true, true),
            Model("nova-2", "Nova-2 (STT)", provider, false, false, false, false, 0, false, true),
            Model("aura-2-thalia-en", "Aura-2 (TTS)", provider, false, false, false, false, 0, false, true)
        },
        SgLlmProvider.ElevenLabs => new()
        {
            Model("eleven_multilingual_v2", "Multilingual v2 (TTS)", provider, false, false, false, false, 0, true, true),
            Model("eleven_turbo_v2_5", "Turbo v2.5 (TTS)", provider, false, false, false, false, 0, true, true),
            Model("scribe_v1", "Scribe v1 (STT)", provider, false, false, false, false, 0, false, true)
        },

        // Azure — generic placeholder; actual deployments are listed by the customer.
        SgLlmProvider.AzureOpenAi => new()
        {
            Model("gpt-4o", "GPT-4o (Azure deployment)", provider, true, true, true, false, 128_000, true),
            Model("gpt-4o-mini", "GPT-4o mini (Azure deployment)", provider, true, true, true, false, 128_000, true)
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
