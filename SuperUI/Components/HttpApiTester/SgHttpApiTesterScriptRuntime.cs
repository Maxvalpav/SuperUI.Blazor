using System.Diagnostics;
using System.Text.Json.Nodes;

namespace SuperUI.Components;

/// <summary>
/// Лёгкая sandbox-среда для pre-request и test-скриптов.
/// </summary>
public class ScriptRuntime
{
    private readonly ResponseSnapshot? _response;
    private readonly RequestTab _request;
    private readonly ApiEnvironment? _env;
    private readonly List<TestResult> _results = new();
    private readonly List<string> _logs = new();

    public IReadOnlyList<TestResult> Results => _results;
    public IReadOnlyList<string> Logs => _logs;

    public ScriptRuntime(RequestTab request, ResponseSnapshot? response, ApiEnvironment? env)
    {
        _request = request;
        _response = response;
        _env = env;
    }

    /// <summary>Registers and executes a test assertion. Results are added to <see cref="Results"/>.</summary>
    public void Test(string name, Func<bool> predicate)
    {
        var sw = Stopwatch.StartNew();
        bool passed = false;
        string msg = "";
        try { passed = predicate(); }
        catch (Exception ex) { msg = ex.Message; }
        sw.Stop();
        _results.Add(new TestResult { Name = name, Passed = passed, Message = msg, DurationMs = sw.ElapsedMilliseconds });
    }

    public Expectation Expect(object? value) => new(value, _results);

    /// <summary>Sets an environment variable value.</summary>
    public void EnvSet(string key, string value)
    {
        if (_env is null) return;
        var pair = _env.Variables.FirstOrDefault(v => v.Key == key);
        if (pair is not null) pair.Value = value;
        else _env.Variables.Add(new KvPair(key, value));
    }

    public string EnvGet(string key) =>
        _env?.Variables.FirstOrDefault(v => v.Key == key)?.Value ?? "";

    /// <summary>Sets a request header. Adds the header if it does not already exist.</summary>
    public void SetHeader(string key, string value)
    {
        var h = _request.Headers.FirstOrDefault(h => h.Key == key);
        if (h is not null) h.Value = value;
        else _request.Headers.Add(new KvPair(key, value));
    }

    public int ResponseStatus => _response?.Status ?? 0;
    public long ResponseTime => _response?.ElapsedMs ?? 0;
    public string ResponseBody => _response?.RawBody ?? "";
    public JsonNode? ResponseJson
    {
        get
        {
            try { return JsonNode.Parse(_response?.RawBody ?? "null"); }
            catch { return null; }
        }
    }

    public void Log(string msg) => _logs.Add($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");

    /// <summary>Fluent assertion helper returned by <see cref="Expect"/> for chaining test expectations.</summary>
    public class Expectation
    {
        private readonly object? _actual;
        private readonly List<TestResult> _results;
        private string _name = "expect";

        public Expectation(object? actual, List<TestResult> results)
        {
            _actual = actual;
            _results = results;
        }

        /// <summary>Sets a custom name for the next assertion.</summary>
        public Expectation Named(string name) { _name = name; return this; }

        public void ToBe(object? expected) =>
            _results.Add(new TestResult
            {
                Name = $"{_name} to be {expected}",
                Passed = Equals(_actual, expected),
                Message = $"actual: {_actual}"
            });

        public void ToContain(string sub) =>
            _results.Add(new TestResult
            {
                Name = $"{_name} to contain \"{sub}\"",
                Passed = _actual?.ToString()?.Contains(sub) == true,
                Message = $"actual: {_actual}"
            });

        public void ToBeGreaterThan(long n) =>
            _results.Add(new TestResult
            {
                Name = $"{_name} > {n}",
                Passed = Convert.ToInt64(_actual) > n,
                Message = $"actual: {_actual}"
            });

        public void ToBeLessThan(long n) =>
            _results.Add(new TestResult
            {
                Name = $"{_name} < {n}",
                Passed = Convert.ToInt64(_actual) < n,
                Message = $"actual: {_actual}"
            });

        public void ToBeNull() =>
            _results.Add(new TestResult
            {
                Name = $"{_name} to be null",
                Passed = _actual is null
            });

        public void ToNotBeNull() =>
            _results.Add(new TestResult
            {
                Name = $"{_name} to not be null",
                Passed = _actual is not null
            });
    }
}
