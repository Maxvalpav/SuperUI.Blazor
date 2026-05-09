using System.Text.Json.Nodes;

namespace SuperUI.Components;

// ── KV-пара ──────────────────────────────────────────────────
public class KvPair
{
    public bool Enabled { get; set; } = true;
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public string Comment { get; set; } = "";

    public KvPair() { }
    public KvPair(string key, string value, bool enabled = true)
    {
        Key = key;
        Value = value;
        Enabled = enabled;
    }
}

// ── Одна вкладка запроса ─────────────────────────────────────
public class RequestTab
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TabName { get; set; } = "New Request";
    public string Method { get; set; } = "GET";
    public string Url { get; set; } = "";
    public string BodyType { get; set; } = "JSON";
    public string Body { get; set; } = "";
    public string AuthType { get; set; } = "None";
    public string BearerToken { get; set; } = "";
    public string BasicUser { get; set; } = "";
    public string BasicPassword { get; set; } = "";
    public string ApiKeyName { get; set; } = "X-API-Key";
    public string ApiKeyValue { get; set; } = "";
    public string OAuth2Token { get; set; } = "";
    public string PreScript { get; set; } = "";
    public string TestScript { get; set; } = "";
    public bool IsDirty { get; set; } = false;

    public List<KvPair> Headers { get; set; } = new() { new("Content-Type", "application/json") };
    public List<KvPair> QueryParams { get; set; } = new();
    public List<KvPair> FormFields { get; set; } = new();

    public ResponseSnapshot? LastResponse { get; set; }
}

// ── Снапшот ответа ───────────────────────────────────────────
public class ResponseSnapshot
{
    public int Status { get; set; }
    public string StatusText { get; set; } = "";
    public long ElapsedMs { get; set; }
    public int SizeBytes { get; set; }
    public string ContentType { get; set; } = "";
    public string RawBody { get; set; } = "";
    public string PrettyBody { get; set; } = "";
    public bool IsJsonArray { get; set; }
    public DateTime ReceivedAt { get; set; } = DateTime.Now;
    public List<KvPair> Headers { get; set; } = new();
    public List<TestResult> Tests { get; set; } = new();

    public List<string> GridColumns { get; set; } = new();
    public List<Dictionary<string, object?>> GridRows { get; set; } = new();

    public List<RedirectStep> Redirects { get; set; } = new();
}

// ── Результат теста ──────────────────────────────────────────
public class TestResult
{
    public string Name { get; set; } = "";
    public bool Passed { get; set; }
    public string Message { get; set; } = "";
    public long DurationMs { get; set; }
}

// ── Шаг redirect ─────────────────────────────────────────────
public class RedirectStep
{
    public int Status { get; set; }
    public string Location { get; set; } = "";
}

// ── Сохранённый запрос в коллекции ───────────────────────────
public class SavedRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Tags { get; set; } = "";
    public int SortOrder { get; set; }
    public RequestTab Tab { get; set; } = new();
}

// ── Коллекция ─────────────────────────────────────────────────
public class Collection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public List<SavedRequest> Requests { get; set; } = new();
}

// ── Запись в истории ─────────────────────────────────────────
public class HistoryEntry
{
    public string Method { get; set; } = "";
    public string Url { get; set; } = "";
    public int? Status { get; set; }
    public long ElapsedMs { get; set; }
    public int SizeBytes { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N")[..8];
}

// ── Окружение ────────────────────────────────────────────────
public class ApiEnvironment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public List<KvPair> Variables { get; set; } = new();
}

// ── Mock-endpoint ────────────────────────────────────────────
public class MockEndpoint
{
    public bool Enabled { get; set; } = true;
    public string Method { get; set; } = "GET";
    public string Path { get; set; } = "/api/mock";
    public int StatusCode { get; set; } = 200;
    public string ContentType { get; set; } = "application/json";
    public string Body { get; set; } = "{ \"message\": \"ok\" }";
    public int DelayMs { get; set; } = 0;
    public int HitCount { get; set; } = 0;
}

// ── WebSocket-сообщение ──────────────────────────────────────
public class WsMessage
{
    public string Direction { get; set; } = "recv";
    public string Text { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public bool IsJson { get; set; }
}

// ── Статистика ───────────────────────────────────────────────
public class RequestStat
{
    public string Label { get; set; } = "";
    public long Ms { get; set; }
    public int Status { get; set; }
    public int Bytes { get; set; }
    public bool Success { get; set; }
}

// ── Результат Runner ─────────────────────────────────────────
public class RunnerResult
{
    public int Iteration { get; set; }
    public int Status { get; set; }
    public long ElapsedMs { get; set; }
    public int PassedTests { get; set; }
    public int TotalTests { get; set; }
    public string? Error { get; set; }
    public string Label { get; set; } = "";
}
