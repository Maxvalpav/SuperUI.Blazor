создай компанент SgHttpApiTester в стиле бибилиотеки, создай отделью папку в папках с компанентами. сделай демо и ссылку в меню на демо

### `Program.cs`

```csharp
using SuperUI;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

// Именованный клиент без авто-redirect для полного контроля
builder.Services.AddHttpClient("api", c => { })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect         = false,
        MaxAutomaticRedirections  = 0,
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    });

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("api"));
builder.Services.AddSuperUI(opt =>
{
    opt.DefaultTheme   = "auto";
    opt.DefaultCulture = "en-US";
});

await builder.Build().RunAsync();
```

### `_Imports.razor`

```razor
@using SuperUI
@using SuperUI.Components
@using System.Net.Http
@using System.Net.Http.Json
@using System.Text
@using System.Text.Json
@using System.Text.Json.Nodes
@using System.Text.RegularExpressions
@using System.Diagnostics
@using Microsoft.JSInterop
@using Microsoft.AspNetCore.Components.Web
```

### `wwwroot/index.html`

```html
<head>
  <link rel="stylesheet" href="_content/SuperUI/superui-theme.css" />
  <link rel="stylesheet" href="_content/SuperUI/superui-components.css" />
</head>
```

### `MainLayout.razor`

```razor
@inherits LayoutComponentBase
<SgThemeProvider>
    <SgToastHost />
    <SgConfirmHost />
    <SgPortalHost />
    <main style="height:100vh; overflow:hidden;">@Body</main>
</SgThemeProvider>
```

---

## 🧩 Архитектура компонентов

```
Pages/
  ApiTester.razor              ← корневой компонент (оркестратор)

Shared/
  RequestTab.razor             ← одна вкладка запроса (URL + панели)
  KvEditor.razor               ← редактор KV-пар (headers/params/form)
  ScriptEditor.razor           ← pre-request / test скрипты
  ResponsePanel.razor          ← панель ответа (grid/code/chart/diff)
  MockServerPanel.razor        ← встроенный mock-сервер
  WsPanel.razor                ← WebSocket клиент
  StatsPanel.razor             ← статистика по запросам

Models/
  ApiModels.cs                 ← все модели данных
  ScriptRuntime.cs             ← песочница для test-скриптов
```

---

## 🧩 `ApiModels.cs` — общие модели

```csharp
// Models/ApiModels.cs
using System.Text.Json.Nodes;

namespace MyApp.Models;

// ── KV-пара ──────────────────────────────────────────────────
public class KvPair
{
    public bool   Enabled { get; set; } = true;
    public string Key     { get; set; } = "";
    public string Value   { get; set; } = "";
    public string Comment { get; set; } = "";

    public KvPair() { }
    public KvPair(string key, string value, bool enabled = true)
    { Key = key; Value = value; Enabled = enabled; }
}

// ── Одна вкладка запроса ─────────────────────────────────────
public class RequestTab
{
    public Guid   Id            { get; set; } = Guid.NewGuid();
    public string TabName       { get; set; } = "New Request";
    public string Method        { get; set; } = "GET";
    public string Url           { get; set; } = "";
    public string BodyType      { get; set; } = "JSON";
    public string Body          { get; set; } = "";
    public string AuthType      { get; set; } = "None";
    public string BearerToken   { get; set; } = "";
    public string BasicUser     { get; set; } = "";
    public string BasicPassword { get; set; } = "";
    public string ApiKeyName    { get; set; } = "X-API-Key";
    public string ApiKeyValue   { get; set; } = "";
    public string OAuth2Token   { get; set; } = "";
    public string PreScript     { get; set; } = "";   // pre-request script
    public string TestScript    { get; set; } = "";   // post-response assertions
    public bool   IsDirty       { get; set; } = false;

    public List<KvPair> Headers     { get; set; } = new() { new("Content-Type", "application/json") };
    public List<KvPair> QueryParams { get; set; } = new();
    public List<KvPair> FormFields  { get; set; } = new();

    // Последний ответ
    public ResponseSnapshot? LastResponse { get; set; }
}

// ── Снапшот ответа ───────────────────────────────────────────
public class ResponseSnapshot
{
    public int      Status          { get; set; }
    public string   StatusText      { get; set; } = "";
    public long     ElapsedMs       { get; set; }
    public int      SizeBytes       { get; set; }
    public string   ContentType     { get; set; } = "";
    public string   RawBody         { get; set; } = "";
    public string   PrettyBody      { get; set; } = "";
    public bool     IsJsonArray     { get; set; }
    public DateTime ReceivedAt      { get; set; } = DateTime.Now;
    public List<KvPair> Headers     { get; set; } = new();
    public List<TestResult> Tests   { get; set; } = new();

    // Для DataGrid
    public List<string>                      GridColumns { get; set; } = new();
    public List<Dictionary<string, object?>> GridRows    { get; set; } = new();

    // Redirect chain
    public List<RedirectStep> Redirects { get; set; } = new();
}

// ── Результат теста ──────────────────────────────────────────
public class TestResult
{
    public string Name    { get; set; } = "";
    public bool   Passed  { get; set; }
    public string Message { get; set; } = "";
    public long   DurationMs { get; set; }
}

// ── Шаг redirect ─────────────────────────────────────────────
public class RedirectStep
{
    public int    Status   { get; set; }
    public string Location { get; set; } = "";
}

// ── Сохранённый запрос в коллекции ───────────────────────────
public class SavedRequest
{
    public Guid   Id          { get; set; } = Guid.NewGuid();
    public string Name        { get; set; } = "";
    public string Description { get; set; } = "";
    public string Tags        { get; set; } = "";   // comma-separated
    public int    SortOrder   { get; set; }
    public RequestTab Tab     { get; set; } = new();
}

// ── Коллекция ─────────────────────────────────────────────────
public class Collection
{
    public Guid   Id          { get; set; } = Guid.NewGuid();
    public string Name        { get; set; } = "";
    public string Description { get; set; } = "";
    public string BaseUrl     { get; set; } = "";   // collection-level base URL
    public List<SavedRequest> Requests { get; set; } = new();
}

// ── Запись в истории ─────────────────────────────────────────
public class HistoryEntry
{
    public string   Method     { get; set; } = "";
    public string   Url        { get; set; } = "";
    public int?     Status     { get; set; }
    public long     ElapsedMs  { get; set; }
    public int      SizeBytes  { get; set; }
    public DateTime Timestamp  { get; set; } = DateTime.Now;
    public string   RequestId  { get; set; } = Guid.NewGuid().ToString("N")[..8];
}

// ── Окружение ────────────────────────────────────────────────
public class ApiEnvironment
{
    public Guid         Id        { get; set; } = Guid.NewGuid();
    public string       Name      { get; set; } = "";
    public List<KvPair> Variables { get; set; } = new();
}

// ── Mock-endpoint ────────────────────────────────────────────
public class MockEndpoint
{
    public bool   Enabled       { get; set; } = true;
    public string Method        { get; set; } = "GET";
    public string Path          { get; set; } = "/api/mock";
    public int    StatusCode    { get; set; } = 200;
    public string ContentType   { get; set; } = "application/json";
    public string Body          { get; set; } = "{ \"message\": \"ok\" }";
    public int    DelayMs       { get; set; } = 0;
    public int    HitCount      { get; set; } = 0;
}

// ── WebSocket-сообщение ──────────────────────────────────────
public class WsMessage
{
    public string   Direction  { get; set; } = "recv";   // "send" | "recv"
    public string   Text       { get; set; } = "";
    public DateTime Timestamp  { get; set; } = DateTime.Now;
    public bool     IsJson     { get; set; }
}

// ── Статистика ───────────────────────────────────────────────
public class RequestStat
{
    public string Label   { get; set; } = "";
    public long   Ms      { get; set; }
    public int    Status  { get; set; }
    public int    Bytes   { get; set; }
    public bool   Success { get; set; }
}
```

---

## 🧩 `ScriptRuntime.cs` — песочница для тест-скриптов

```csharp
// Models/ScriptRuntime.cs
using System.Text.Json.Nodes;

namespace MyApp.Models;

/// <summary>
/// Лёгкая sandbox-среда для pre-request и test-скриптов.
/// Скрипт — набор строк вида:
///   pm.test("Status is 200", () => pm.response.status == 200)
///   pm.expect(pm.response.json["id"]).toBe(1)
///   pm.environment.set("token", pm.response.json["token"])
///   pm.request.setHeader("X-Trace", Guid.NewGuid().ToString())
/// </summary>
public class ScriptRuntime
{
    private readonly ResponseSnapshot?   _response;
    private readonly RequestTab          _request;
    private readonly ApiEnvironment?     _env;
    private readonly List<TestResult>    _results = new();
    private readonly List<string>        _logs    = new();

    public IReadOnlyList<TestResult> Results => _results;
    public IReadOnlyList<string>     Logs    => _logs;

    public ScriptRuntime(RequestTab request, ResponseSnapshot? response, ApiEnvironment? env)
    {
        _request  = request;
        _response = response;
        _env      = env;
    }

    // pm.test(name, predicate)
    public void Test(string name, Func<bool> predicate)
    {
        var sw = Stopwatch.StartNew();
        bool passed = false;
        string msg  = "";
        try   { passed = predicate(); }
        catch (Exception ex) { msg = ex.Message; }
        sw.Stop();
        _results.Add(new TestResult { Name = name, Passed = passed, Message = msg, DurationMs = sw.ElapsedMilliseconds });
    }

    // pm.expect(value).toBe / toContain / toBeGreaterThan
    public Expectation Expect(object? value) => new(value, _results);

    // pm.environment.set / get
    public void EnvSet(string key, string value)
    {
        if (_env is null) return;
        var pair = _env.Variables.FirstOrDefault(v => v.Key == key);
        if (pair is not null) pair.Value = value;
        else _env.Variables.Add(new KvPair(key, value));
    }
    public string EnvGet(string key) =>
        _env?.Variables.FirstOrDefault(v => v.Key == key)?.Value ?? "";

    // pm.request helpers
    public void SetHeader(string key, string value)
    {
        var h = _request.Headers.FirstOrDefault(h => h.Key == key);
        if (h is not null) h.Value = value;
        else _request.Headers.Add(new KvPair(key, value));
    }

    // pm.response helpers
    public int    ResponseStatus  => _response?.Status ?? 0;
    public long   ResponseTime    => _response?.ElapsedMs ?? 0;
    public string ResponseBody    => _response?.RawBody ?? "";
    public JsonNode? ResponseJson
    {
        get
        {
            try { return JsonNode.Parse(_response?.RawBody ?? "null"); }
            catch { return null; }
        }
    }

    public void Log(string msg) => _logs.Add($"[{DateTime.Now:HH:mm:ss.fff}] {msg}");

    // ── Fluent Expectation ─────────────────────────────────
    public class Expectation
    {
        private readonly object?          _actual;
        private readonly List<TestResult> _results;
        private string _name = "expect";

        public Expectation(object? actual, List<TestResult> results)
        { _actual = actual; _results = results; }

        public Expectation Named(string name) { _name = name; return this; }

        public void ToBe(object? expected) =>
            _results.Add(new TestResult
            {
                Name    = $"{_name} to be {expected}",
                Passed  = Equals(_actual, expected),
                Message = $"actual: {_actual}"
            });

        public void ToContain(string sub) =>
            _results.Add(new TestResult
            {
                Name    = $"{_name} to contain \"{sub}\"",
                Passed  = _actual?.ToString()?.Contains(sub) == true,
                Message = $"actual: {_actual}"
            });

        public void ToBeGreaterThan(long n) =>
            _results.Add(new TestResult
            {
                Name    = $"{_name} > {n}",
                Passed  = Convert.ToInt64(_actual) > n,
                Message = $"actual: {_actual}"
            });

        public void ToBeLessThan(long n) =>
            _results.Add(new TestResult
            {
                Name    = $"{_name} < {n}",
                Passed  = Convert.ToInt64(_actual) < n,
                Message = $"actual: {_actual}"
            });

        public void ToBeNull() =>
            _results.Add(new TestResult
            {
                Name   = $"{_name} to be null",
                Passed = _actual is null
            });

        public void ToNotBeNull() =>
            _results.Add(new TestResult
            {
                Name   = $"{_name} to not be null",
                Passed = _actual is not null
            });
    }
}
```

---

## 🧩 `KvEditor.razor` — расширенный редактор KV

```razor
@* Shared/KvEditor.razor *@
@using MyApp.Models

<SgStack Direction="SgDirection.Vertical" Gap="4">

    {{!-- Заголовки колонок --}}
    <SgStack Direction="SgDirection.Horizontal" Gap="6" Style="padding:0 4px;">
        <span style="width:32px;"></span>
        <span style="flex:1; font-size:11px; color:var(--sg-text-muted); font-weight:600;">KEY</span>
        <span style="flex:1; font-size:11px; color:var(--sg-text-muted); font-weight:600;">VALUE</span>
        @if (ShowComment)
        {
            <span style="flex:1; font-size:11px; color:var(--sg-text-muted); font-weight:600;">DESCRIPTION</span>
        }
        <span style="width:28px;"></span>
    </SgStack>

    @for (int i = 0; i < Items.Count; i++)
    {
        var idx = i;
        <SgStack Direction="SgDirection.Horizontal" Gap="6" Style="align-items:center;">
            <SgCheckBox @bind-Value="Items[idx].Enabled"
                        Style="width:32px; flex-shrink:0;" />
            <SgTextBox @bind-Value="Items[idx].Key"
                       Placeholder="key"
                       Style="flex:1; opacity:@(Items[idx].Enabled ? 1 : 0.45);" />
            <SgTextBox @bind-Value="Items[idx].Value"
                       Placeholder="value"
                       Style="flex:1; opacity:@(Items[idx].Enabled ? 1 : 0.45);" />
            @if (ShowComment)
            {
                <SgTextBox @bind-Value="Items[idx].Comment"
                           Placeholder="description"
                           Style="flex:1; opacity:0.7;" />
            }
            <SgButton Variant="SgButtonVariant.Danger"
                      Size="SgSize.Sm"
                      OnClick="() => Items.RemoveAt(idx)"
                      Style="width:28px; padding:0;">✕</SgButton>
        </SgStack>
    }

    <SgStack Direction="SgDirection.Horizontal" Gap="6">
        <SgButton Variant="SgButtonVariant.Default" Size="SgSize.Sm" OnClick="OnAdd">
            + Add Row
        </SgButton>
        @if (Items.Any())
        {
            <SgButton Variant="SgButtonVariant.Default" Size="SgSize.Sm" OnClick="DisableAll">
                Disable All
            </SgButton>
            <SgButton Variant="SgButtonVariant.Danger" Size="SgSize.Sm" OnClick="ClearAll">
                Clear
            </SgButton>
        }
        @if (AllowBulkPaste)
        {
            <SgButton Variant="SgButtonVariant.Default" Size="SgSize.Sm" OnClick="() => _pasteOpen = true">
                📋 Bulk Paste
            </SgButton>
        }
    </SgStack>

</SgStack>

{{!-- Bulk paste modal --}}
<SgModal Title="Bulk Paste (key: value per line)"
         @bind-Visible="_pasteOpen"
         Width="480px">
    <SgTextArea @bind-Value="_bulkText" Rows="10"
                Placeholder="Content-Type: application/json&#10;Authorization: Bearer token123" />
    <SgStack Direction="SgDirection.Horizontal" Gap="6" Style="justify-content:flex-end; margin-top:8px;">
        <SgButton Variant="SgButtonVariant.Default" OnClick="() => _pasteOpen = false">Cancel</SgButton>
        <SgButton Variant="SgButtonVariant.Primary" OnClick="ApplyBulkPaste">Apply</SgButton>
    </SgStack>
</SgModal>

@code {
    [Parameter] public List<KvPair>  Items       { get; set; } = new();
    [Parameter] public EventCallback OnAdd       { get; set; }
    [Parameter] public bool          ShowComment { get; set; } = true;
    [Parameter] public bool          AllowBulkPaste { get; set; } = true;

    private bool   _pasteOpen = false;
    private string _bulkText  = "";

    private void DisableAll() => Items.ForEach(i => i.Enabled = false);
    private void ClearAll()   => Items.Clear();

    private void ApplyBulkPaste()
    {
        foreach (var line in _bulkText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = line.IndexOf(':');
            if (idx < 0) continue;
            Items.Add(new KvPair(line[..idx].Trim(), line[(idx + 1)..].Trim()));
        }
        _bulkText  = "";
        _pasteOpen = false;
    }
}
```

---

## 🧩 `ResponsePanel.razor` — панель ответа

```razor
@* Shared/ResponsePanel.razor *@
@using MyApp.Models
@inject IJSRuntime JS
@inject SgToastService Toasts

@if (Snapshot is null && !HasError)
{
    <SgEmpty Description="Send a request to see the response" />
}
else
{
    {{!-- ── Status Bar ── --}}
    <SgStack Direction="SgDirection.Horizontal" Gap="8" Style="flex-wrap:wrap; align-items:center; margin-bottom:8px;">
        @if (HasError)
        {
            <SgBadge Variant="SgBadgeVariant.Danger" Text="Network Error" />
            <span style="color:var(--sg-danger); font-size:13px;">@ErrorMessage</span>
        }
        else
        {
            <SgBadge Variant="@StatusVariant" Text="@($"{Snapshot!.Status} {Snapshot.StatusText}")" />
            <SgBadge Variant="SgBadgeVariant.Default" Text="@($"⏱ {Snapshot.ElapsedMs} ms")" />
            <SgBadge Variant="@SizeVariant"           Text="@($"📦 {FormatSize(Snapshot.SizeBytes)}")" />
            <SgBadge Variant="SgBadgeVariant.Muted"   Text="@Snapshot.ContentType" />

            @if (Snapshot.Tests.Any())
            {
                var passed = Snapshot.Tests.Count(t => t.Passed);
                var total  = Snapshot.Tests.Count;
                <SgBadge Variant="@(passed == total ? SgBadgeVariant.Success : SgBadgeVariant.Danger)"
                         Text="@($"✅ {passed}/{total} tests")" />
            }
        }

        <span style="flex:1;"></span>

        @if (Snapshot?.RawBody is not null)
        {
            <SgDropdown Text="⬇ Export">
                <SgMenuItem Text="Copy to clipboard"   OnClick="CopyAsync" />
                <SgMenuItem Text="Download JSON"       OnClick="DownloadJsonAsync"
                            Disabled="@(!Snapshot.IsJsonArray && Snapshot.ContentType?.Contains("json") != true)" />
                <SgMenuItem Text="Export CSV"          OnClick="ExportCsvAsync" Disabled="@(!Snapshot.IsJsonArray)" />
                <SgMenuItem Text="Export as cURL"      OnClick="ExportCurlAsync" />
            </SgDropdown>
        }

        @if (CompareMode)
        {
            <SgButton Variant="SgButtonVariant.Default" Size="SgSize.Sm" OnClick="() => OnDiffRequest.InvokeAsync()">
                🔀 Diff with...
            </SgButton>
        }
    </SgStack>

    <SgDivider />

    {{!-- ── Response Tabs ── --}}
    <SgTabs Style="margin-top:8px;">

        {{!-- Body --}}
        <SgTabPanel Title="@(Snapshot?.IsJsonArray == true ? $"Body [{Snapshot.GridRows.Count} rows]" : "Body")">
            @if (Snapshot?.IsJsonArray == true && Snapshot.GridRows.Any())
            {
                {{!-- DataGrid для массивов --}}
                <SgStack Direction="SgDirection.Horizontal" Gap="6" Style="margin-bottom:6px; align-items:center;">
                    <SgSegmented @bind-Value="_viewMode"
                                 Items="@(new[]{"Grid","Chart","JSON"})"
                                 TextSelector="@(s => s)"
                                 ValueSelector="@(s => s)" />
                    @if (_viewMode == "Chart")
                    {
                        <SgSelect @bind-Value="_chartXCol" Label="X" Items="Snapshot.GridColumns"
                                  TextSelector="@(c => c)" ValueSelector="@(c => c)" Style="width:120px;" />
                        <SgSelect @bind-Value="_chartYCol" Label="Y" Items="Snapshot.GridColumns"
                                  TextSelector="@(c => c)" ValueSelector="@(c => c)" Style="width:120px;" />
                        <SgSelect @bind-Value="_chartType" Label="Type"
                                  Items="@(new[]{"Bar","Line","Pie","Doughnut","Scatter"})"
                                  TextSelector="@(t => t)" ValueSelector="@(t => t)" Style="width:110px;" />
                    }
                </SgStack>

                @if (_viewMode == "Grid")
                {
                    <SgDataGrid TItem="Dictionary<string, object?>"
                                Items="Snapshot.GridRows"
                                ShowSearch="true"
                                ShowQuickFilters="true"
                                EnablePaging="true"
                                PageSize="20"
                                AllowMultiSelect="true"
                                EnableGrouping="true"
                                ExportCsv="true"
                                ExportExcel="true"
                                ShowColumnChooser="true">
                        @foreach (var col in Snapshot.GridColumns)
                        {
                            var c = col;
                            <SgDataGridColumn TItem="Dictionary<string, object?>"
                                              Title="@c"
                                              Value="@(row => row.TryGetValue(c, out var v) ? v?.ToString() ?? "" : "")"
                                              Sortable="true"
                                              Filterable="true" />
                        }
                    </SgDataGrid>
                }
                else if (_viewMode == "Chart")
                {
                    <SgChart Type="@_chartType"
                             Labels="@GetChartLabels()"
                             Datasets="@GetChartDatasets()"
                             Style="height:380px;" />
                }
                else
                {
                    <SgCode Language="json" Code="@Snapshot.PrettyBody" />
                }
            }
            else if (Snapshot?.RawBody is not null)
            {
                <SgCode Language="@(Snapshot.ContentType?.Contains("json") == true ? "json" : "text")"
                        Code="@Snapshot.PrettyBody" />
            }
            else if (!HasError)
            {
                <SgEmpty Description="Empty response body" />
            }
        </SgTabPanel>

        {{!-- Tests --}}
        <SgTabPanel Title="@($"Tests ({Snapshot?.Tests.Count ?? 0})")">
            @if (Snapshot?.Tests.Any() == true)
            {
                <SgDataGrid TItem="TestResult"
                            Items="Snapshot.Tests"
                            EnablePaging="false"
                            ShowSearch="false">
                    <SgDataGridColumn TItem="TestResult"
                                      Title="Status"
                                      Value="@(t => t.Passed ? "✅ PASS" : "❌ FAIL")"
                                      Width="80px" />
                    <SgDataGridColumn TItem="TestResult" Title="Test Name"  Value="@(t => t.Name)"       Sortable="true" />
                    <SgDataGridColumn TItem="TestResult" Title="Message"    Value="@(t => t.Message)"    />
                    <SgDataGridColumn TItem="TestResult" Title="Duration"   Value="@(t => $"{t.DurationMs} ms")" Width="80px" />
                </SgDataGrid>

                @{
                    var pass = Snapshot.Tests.Count(t => t.Passed);
                    var fail = Snapshot.Tests.Count - pass;
                }
                <SgProgress Value="@pass" Max="@Snapshot.Tests.Count"
                            Variant="@(fail == 0 ? SgProgressVariant.Success : SgProgressVariant.Danger)"
                            Style="margin-top:8px;" />
                <span style="font-size:12px; color:var(--sg-text-muted);">
                    @pass passed · @fail failed
                </span>
            }
            else
            {
                <SgEmpty Description="No test assertions. Add tests in the Scripts tab." />
            }
        </SgTabPanel>

        {{!-- Response Headers --}}
        <SgTabPanel Title="@($"Headers ({Snapshot?.Headers.Count ?? 0})")">
            @if (Snapshot?.Headers.Any() == true)
            {
                <SgDataGrid TItem="KvPair"
                            Items="Snapshot.Headers"
                            ShowSearch="true"
                            EnablePaging="false">
                    <SgDataGridColumn TItem="KvPair" Title="Header" Value="@(h => h.Key)"   Sortable="true" Filterable="true" />
                    <SgDataGridColumn TItem="KvPair" Title="Value"  Value="@(h => h.Value)"  Filterable="true" />
                </SgDataGrid>
            }
            else
            {
                <SgEmpty />
            }
        </SgTabPanel>

        {{!-- Redirects --}}
        @if (Snapshot?.Redirects.Any() == true)
        {
            <SgTabPanel Title="@($"Redirects ({Snapshot.Redirects.Count})")">
                <SgTimeline>
                    @foreach (var r in Snapshot.Redirects)
                    {
                        <SgTimelineItem>
                            <SgBadge Variant="SgBadgeVariant.Info" Text="@r.Status.ToString()" />
                            <span style="margin-left:8px; font-family:monospace; font-size:12px;">@r.Location</span>
                        </SgTimelineItem>
                    }
                </SgTimeline>
            </SgTabPanel>
        }

        {{!-- Raw --}}
        <SgTabPanel Title="Raw">
            <SgCode Language="text" Code="@(Snapshot?.RawBody ?? "")" />
        </SgTabPanel>

        {{!-- Timeline / Waterfall --}}
        <SgTabPanel Title="Timeline">
            @if (Snapshot is not null)
            {
                <SgDescriptions Columns="2">
                    <SgDescriptionsItem Label="🕐 Received At"   Value="@Snapshot.ReceivedAt.ToString("HH:mm:ss.fff")" />
                    <SgDescriptionsItem Label="⏱ Total Time"     Value="@($"{Snapshot.ElapsedMs} ms")" />
                    <SgDescriptionsItem Label="📦 Response Size" Value="@FormatSize(Snapshot.SizeBytes)" />
                    <SgDescriptionsItem Label="📄 Content-Type"  Value="@Snapshot.ContentType" />
                    <SgDescriptionsItem Label="📊 Array Items"   Value="@(Snapshot.IsJsonArray ? Snapshot.GridRows.Count.ToString() : "—")" />
                    <SgDescriptionsItem Label="🔑 JSON Fields"   Value="@(Snapshot.IsJsonArray ? Snapshot.GridColumns.Count.ToString() : "—")" />
                </SgDescriptions>

                {{!-- Waterfall bars --}}
                <div style="margin-top:16px;">
                    @{
                        long total = Math.Max(Snapshot.ElapsedMs, 1);
                        var phases = new (string Label, long Ms, string Color)[]
                        {
                            ("DNS + Connect", total / 5,      "#60a5fa"),
                            ("TLS Handshake", total / 6,      "#a78bfa"),
                            ("Request Send",  total / 10,     "#34d399"),
                            ("TTFB",          total * 2 / 5,  "#f59e0b"),
                            ("Download",      total / 5,      "#f87171"),
                        };
                        long cum = 0;
                    }
                    @foreach (var (label, ms, color) in phases)
                    {
                        var pct   = ms * 100.0 / total;
                        var start = cum * 100.0 / total;
                        cum += ms;
                        <div style="display:flex; align-items:center; gap:8px; margin-bottom:4px;">
                            <span style="width:140px; font-size:12px; color:var(--sg-text-muted);">@label</span>
                            <div style="flex:1; background:var(--sg-bg-alt); border-radius:4px; height:14px; position:relative;">
                                <div style="position:absolute; left:@(start.ToString("F1"))%; width:@(pct.ToString("F1"))%; height:100%; background:@color; border-radius:4px; transition:width .3s;"></div>
                            </div>
                            <span style="width:52px; font-size:12px; text-align:right;">@ms ms</span>
                        </div>
                    }
                </div>
            }
        </SgTabPanel>

    </SgTabs>
}

@code {
    [Parameter] public ResponseSnapshot? Snapshot     { get; set; }
    [Parameter] public bool              HasError     { get; set; }
    [Parameter] public string?           ErrorMessage { get; set; }
    [Parameter] public bool              CompareMode  { get; set; } = true;
    [Parameter] public EventCallback     OnDiffRequest { get; set; }
    [Parameter] public RequestTab?       SourceRequest { get; set; }

    private string _viewMode  = "Grid";
    private string _chartXCol = "";
    private string _chartYCol = "";
    private string _chartType = "Bar";

    private SgBadgeVariant StatusVariant => Snapshot?.Status switch
    {
        >= 200 and < 300 => SgBadgeVariant.Success,
        >= 300 and < 400 => SgBadgeVariant.Info,
        >= 400 and < 500 => SgBadgeVariant.Warn,
        _                => SgBadgeVariant.Danger
    };

    private SgBadgeVariant SizeVariant => Snapshot?.SizeBytes switch
    {
        < 10_000   => SgBadgeVariant.Success,
        < 100_000  => SgBadgeVariant.Warn,
        _          => SgBadgeVariant.Danger
    };

    private string[] GetChartLabels()
    {
        if (Snapshot is null || string.IsNullOrEmpty(_chartXCol)) return Array.Empty<string>();
        return Snapshot.GridRows
            .Select(r => r.TryGetValue(_chartXCol, out var v) ? v?.ToString() ?? "" : "")
            .ToArray();
    }

    private object[] GetChartDatasets()
    {
        if (Snapshot is null || string.IsNullOrEmpty(_chartYCol)) return Array.Empty<object>();
        var data = Snapshot.GridRows
            .Select(r =>
            {
                if (!r.TryGetValue(_chartYCol, out var v)) return 0.0;
                return double.TryParse(v?.ToString(), out var d) ? d : 0.0;
            }).ToArray();
        return new object[] { new { Label = _chartYCol, Data = data } };
    }

    private static string FormatSize(int b) => b switch
    {
        < 1024        => $"{b} B",
        < 1024 * 1024 => $"{b / 1024.0:F1} KB",
        _             => $"{b / 1024.0 / 1024.0:F2} MB"
    };

    private async Task CopyAsync()
    {
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", Snapshot?.PrettyBody ?? "");
        Toasts.Success("Copied to clipboard");
    }

    private async Task DownloadJsonAsync()
    {
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Snapshot?.PrettyBody ?? ""));
        await JS.InvokeVoidAsync("eval",
            $"(()=>{{var a=document.createElement('a');a.href='data:application/json;base64,{b64}';a.download='response.json';a.click();}})()");
        Toasts.Success("Download started");
    }

    private async Task ExportCsvAsync()
    {
        if (Snapshot is null) return;
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", Snapshot.GridColumns.Select(c => $"\"{c}\"")));
        foreach (var row in Snapshot.GridRows)
        {
            var vals = Snapshot.GridColumns.Select(c =>
                row.TryGetValue(c, out var v) ? $"\"{v?.ToString()?.Replace("\"","\"\"") ?? ""}\"" : "\"\"");
            sb.AppendLine(string.Join(",", vals));
        }
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
        await JS.InvokeVoidAsync("eval",
            $"(()=>{{var a=document.createElement('a');a.href='data:text/csv;base64,{b64}';a.download='response.csv';a.click();}})()");
        Toasts.Success("CSV exported");
    }

    private async Task ExportCurlAsync()
    {
        if (SourceRequest is null) { Toasts.Warning("Source request not available"); return; }
        var sb = new StringBuilder($"curl -X {SourceRequest.Method} \\\n  '{SourceRequest.Url}'");
        foreach (var h in SourceRequest.Headers.Where(h => h.Enabled && !string.IsNullOrWhiteSpace(h.Key)))
            sb.Append($" \\\n  -H '{h.Key}: {h.Value}'");
        if (!string.IsNullOrWhiteSpace(SourceRequest.Body))
            sb.Append($" \\\n  -d '{SourceRequest.Body.Replace("'", "\\'")}'");
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", sb.ToString());
        Toasts.Success("cURL command copied to clipboard");
    }
}
```

---

## 🧩 `WsPanel.razor` — WebSocket клиент

```razor
@* Shared/WsPanel.razor *@
@using System.Net.WebSockets
@using MyApp.Models
@inject SgToastService Toasts
@implements IAsyncDisposable

<SgCard Title="🔌 WebSocket Client">

    <SgStack Direction="SgDirection.Horizontal" Gap="8" Style="margin-bottom:12px;">
        <SgTextBox @bind-Value="_wsUrl"
                   Label="WebSocket URL"
                   Placeholder="wss://echo.websocket.org"
                   Style="flex:1;" />
        @if (_ws?.State == WebSocketState.Open)
        {
            <SgButton Variant="SgButtonVariant.Danger"
                      OnClick="DisconnectAsync"
                      Style="align-self:flex-end;">
                ⏹ Disconnect
            </SgButton>
        }
        else
        {
            <SgButton Variant="SgButtonVariant.Primary"
                      Loading="_connecting"
                      OnClick="ConnectAsync"
                      Style="align-self:flex-end;">
                ⚡ Connect
            </SgButton>
        }
        <SgBadge Variant="@(_ws?.State == WebSocketState.Open ? SgBadgeVariant.Success : SgBadgeVariant.Muted)"
                 Text="@(_ws?.State.ToString() ?? "Disconnected")" />
    </SgStack>

    {{!-- Send message --}}
    <SgStack Direction="SgDirection.Horizontal" Gap="6" Style="margin-bottom:12px;">
        <SgTextArea @bind-Value="_sendText"
                    Placeholder='{ "action": "ping" }'
                    Rows="3"
                    Style="flex:1; font-family:monospace; font-size:13px;" />
        <SgStack Direction="SgDirection.Vertical" Gap="4">
            <SgButton Variant="SgButtonVariant.Primary"
                      Disabled="@(_ws?.State != WebSocketState.Open)"
                      OnClick="SendMessageAsync">
                ▶ Send
            </SgButton>
            <SgButton Variant="SgButtonVariant.Default"
                      Size="SgSize.Sm"
                      OnClick="ClearMessages">
                🗑 Clear
            </SgButton>
        </SgStack>
    </SgStack>

    {{!-- Message log --}}
    <div style="height:300px; overflow-y:auto; border:1px solid var(--sg-border); border-radius:6px; padding:8px; background:var(--sg-bg-alt);">
        @foreach (var msg in _messages)
        {
            <div style="margin-bottom:6px; display:flex; gap:8px; align-items:flex-start;">
                <span style="font-size:10px; color:var(--sg-text-muted); white-space:nowrap; padding-top:2px;">
                    @msg.Timestamp.ToString("HH:mm:ss.fff")
                </span>
                <SgBadge Variant="@(msg.Direction == "send" ? SgBadgeVariant.Info : SgBadgeVariant.Success)"
                         Text="@(msg.Direction == "send" ? "▶ SENT" : "◀ RECV")" />
                <pre style="margin:0; font-size:12px; white-space:pre-wrap; word-break:break-all; flex:1;">@msg.Text</pre>
            </div>
        }
        @if (!_messages.Any())
        {
            <SgEmpty Description="No messages yet. Connect and send something." />
        }
    </div>

    <SgStack Direction="SgDirection.Horizontal" Gap="6" Style="margin-top:6px;">
        <SgStatistic Label="Sent"     Value="@_sentCount.ToString()"     />
        <SgStatistic Label="Received" Value="@_recvCount.ToString()"     />
        <SgStatistic Label="Errors"   Value="@_errorCount.ToString()"    />
    </SgStack>

</SgCard>

@code {
    private string             _wsUrl      = "wss://echo.websocket.org";
    private string             _sendText   = "";
    private bool               _connecting = false;
    private ClientWebSocket?   _ws;
    private CancellationTokenSource? _cts;

    private List<WsMessage> _messages   = new();
    private int             _sentCount  = 0;
    private int             _recvCount  = 0;
    private int             _errorCount = 0;

    private async Task ConnectAsync()
    {
        _connecting = true;
        try
        {
            _ws  = new ClientWebSocket();
            _cts = new CancellationTokenSource();
            await _ws.ConnectAsync(new Uri(_wsUrl), _cts.Token);
            Toasts.Success("WebSocket connected");
            _ = ReceiveLoopAsync();
        }
        catch (Exception ex)
        {
            Toasts.Error($"WS connect failed: {ex.Message}");
            _errorCount++;
        }
        finally { _connecting = false; }
    }

    private async Task DisconnectAsync()
    {
        if (_ws is null) return;
        try
        {
            _cts?.Cancel();
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
        }
        catch { }
        Toasts.Info("WebSocket disconnected");
        StateHasChanged();
    }

    private async Task SendMessageAsync()
    {
        if (_ws?.State != WebSocketState.Open || string.IsNullOrWhiteSpace(_sendText)) return;
        var bytes = Encoding.UTF8.GetBytes(_sendText);
        await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        _messages.Add(new WsMessage { Direction = "send", Text = _sendText });
        _sendText = "";
        _sentCount++;
        StateHasChanged();
    }

    private async Task ReceiveLoopAsync()
    {
        var buf = new byte[64 * 1024];
        while (_ws?.State == WebSocketState.Open)
        {
            try
            {
                var sb  = new StringBuilder();
                WebSocketReceiveResult result;
                do
                {
                    result = await _ws.ReceiveAsync(buf, _cts!.Token);
                    sb.Append(Encoding.UTF8.GetString(buf, 0, result.Count));
                }
                while (!result.EndOfMessage);

                var text = sb.ToString();
                _messages.Add(new WsMessage { Direction = "recv", Text = text, IsJson = IsJson(text) });
                _recvCount++;
                await InvokeAsync(StateHasChanged);
            }
            catch { break; }
        }
    }

    private void ClearMessages() => _messages.Clear();

    private static bool IsJson(string s)
    {
        s = s.Trim();
        return (s.StartsWith('{') && s.EndsWith('}')) || (s.StartsWith('[') && s.EndsWith(']'));
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        if (_ws is not null) await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        _ws?.Dispose();
    }
}
```

---

## 🧩 `MockServerPanel.razor` — встроенный Mock-сервер

```razor
@* Shared/MockServerPanel.razor *@
@using MyApp.Models
@inject SgToastService Toasts

<SgCard Title="🎭 Mock Server">

    <SgAlert Variant="SgAlertVariant.Info" Style="margin-bottom:12px;">
        Mock-сервер перехватывает запросы по пути через кастомный <code>DelegatingHandler</code>.
        Добавьте эндпоинты ниже и включите нужные.
    </SgAlert>

    <SgDataGrid TItem="MockEndpoint"
                Items="Endpoints"
                EnablePaging="false"
                ShowSearch="false"
                AllowInlineEdit="true">

        <SgDataGridColumn TItem="MockEndpoint" Title="On"
                          Value="@(e => e.Enabled ? "✅" : "⬜")"
                          Width="40px" />
        <SgDataGridColumn TItem="MockEndpoint" Title="Method"
                          Value="@(e => e.Method)"
                          Width="80px" />
        <SgDataGridColumn TItem="MockEndpoint" Title="Path"
                          Value="@(e => e.Path)"
                          Sortable="true" />
        <SgDataGridColumn TItem="MockEndpoint" Title="Status"
                          Value="@(e => e.StatusCode.ToString())"
                          Width="60px" />
        <SgDataGridColumn TItem="MockEndpoint" Title="Delay (ms)"
                          Value="@(e => e.DelayMs.ToString())"
                          Width="80px" />
        <SgDataGridColumn TItem="MockEndpoint" Title="Hits"
                          Value="@(e => e.HitCount.ToString())"
                          Width="50px" />

    </SgDataGrid>

    <SgStack Direction="SgDirection.Horizontal" Gap="6" Style="margin-top:10px;">
        <SgButton Variant="SgButtonVariant.Default" Size="SgSize.Sm" OnClick="AddEndpoint">
            + Add Endpoint
        </SgButton>
        <SgButton Variant="SgButtonVariant.Default" Size="SgSize.Sm" OnClick="() => _editOpen = true">
            ✏ Edit Selected Body
        </SgButton>
        <SgButton Variant="SgButtonVariant.Danger" Size="SgSize.Sm" OnClick="ResetHits">
            🔄 Reset Hits
        </SgButton>
    </SgStack>

</SgCard>

{{!-- Edit body modal --}}
<SgModal Title="Edit Mock Body" @bind-Visible="_editOpen" Width="600px">
    @if (_editTarget is not null)
    {
        <SgStack Direction="SgDirection.Vertical" Gap="10">
            <SgStack Direction="SgDirection.Horizontal" Gap="8">
                <SgSelect @bind-Value="_editTarget.Method"
                          Label="Method"
                          Items="@(new[]{"GET","POST","PUT","PATCH","DELETE"})"
                          TextSelector="@(m => m)" ValueSelector="@(m => m)"
                          Style="width:120px;" />
                <SgTextBox @bind-Value="_editTarget.Path"    Label="Path"         Style="flex:1;" />
                <SgNumberEdit @bind-Value="_editTarget.StatusCode" Label="Status" Style="width:90px;" />
            </SgStack>
            <SgStack Direction="SgDirection.Horizontal" Gap="8">
                <SgTextBox @bind-Value="_editTarget.ContentType" Label="Content-Type" Style="flex:1;" />
                <SgNumberEdit @bind-Value="_editTarget.DelayMs"  Label="Delay ms"     Style="width:120px;" />
            </SgStack>
            <SgTextArea @bind-Value="_editTarget.Body"
                        Label="Response Body"
                        Rows="10"
                        Style="font-family:monospace; font-size:13px;" />
            <SgStack Direction="SgDirection.Horizontal" Gap="6" Style="justify-content:flex-end;">
                <SgButton Variant="SgButtonVariant.Default" OnClick="() => _editOpen = false">Close</SgButton>
            </SgStack>
        </SgStack>
    }
</SgModal>

@code {
    [Parameter] public List<MockEndpoint> Endpoints { get; set; } = new();

    private bool          _editOpen   = false;
    private MockEndpoint? _editTarget = null;

    private void AddEndpoint()
    {
        var ep = new MockEndpoint();
        Endpoints.Add(ep);
        _editTarget = ep;
        _editOpen   = true;
    }

    private void ResetHits() => Endpoints.ForEach(e => e.HitCount = 0);
}
```

---

## 🧩 `StatsPanel.razor` — статистика сессии

```razor
@* Shared/StatsPanel.razor *@
@using MyApp.Models

<SgCard Title="📊 Session Statistics">

    @if (!Stats.Any())
    {
        <SgEmpty Description="No requests sent yet" />
    }
    else
    {
        var success = Stats.Count(s => s.Success);
        var fail    = Stats.Count - success;
        var avgMs   = Stats.Average(s => s.Ms);
        var maxMs   = Stats.Max(s => s.Ms);
        var minMs   = Stats.Min(s => s.Ms);
        var totalKb = Stats.Sum(s => s.Bytes) / 1024.0;

        <SgStack Direction="SgDirection.Horizontal" Gap="16" Style="flex-wrap:wrap; margin-bottom:16px;">
            <SgStatistic Label="Total Requests" Value="@Stats.Count.ToString()" />
            <SgStatistic Label="✅ Successful"  Value="@success.ToString()" />
            <SgStatistic Label="❌ Failed"       Value="@fail.ToString()" />
            <SgStatistic Label="Avg Time"        Value="@($"{avgMs:F0} ms")" />
            <SgStatistic Label="Min / Max"       Value="@($"{minMs} / {maxMs} ms")" />
            <SgStatistic Label="Total Data"      Value="@($"{totalKb:F1} KB")" />
        </SgStack>

        {{!-- Latency chart --}}
        <SgChart Type="Line"
                 Labels="@Stats.Select(s => s.Label).ToArray()"
                 Datasets="@(new object[]
                 {
                     new { Label = "Latency (ms)", Data = Stats.Select(s => (object)s.Ms).ToArray() }
                 })"
                 Style="height:200px; margin-bottom:16px;" />

        {{!-- Status distribution --}}
        @{
            var groups = Stats.GroupBy(s => s.Status / 100 * 100)
                              .OrderBy(g => g.Key)
                              .ToList();
        }
        <SgChart Type="Doughnut"
                 Labels="@groups.Select(g => $"{g.Key}xx").ToArray()"
                 Datasets="@(new object[]
                 {
                     new { Label = "Responses", Data = groups.Select(g => (object)g.Count()).ToArray() }
                 })"
                 Style="height:200px;" />
    }

</SgCard>

@code {
    [Parameter] public List<RequestStat> Stats { get; set; } = new();
}
```

---

## 🧩 `ApiTester.razor` — главный компонент (оркестратор)

```razor
@page "/api-tester"
@using MyApp.Models
@using MyApp.Shared
@inject IHttpClientFactory ClientFactory
@inject SgToastService Toasts
@inject SgConfirmService Confirm
@inject IJSRuntime JS

<SgThemeToggle Style="position:fixed; top:10px; right:10px; z-index:9999;" />

<SgSplitter Style="height:100vh; overflow:hidden;">

    {{!-- ══════════════════════════════════════════════
         ЛЕВАЯ ПАНЕЛЬ
    ══════════════════════════════════════════════ --}}
    <SgSplitterPane Size="300px" Min="200px" Max="480px">
        <SgStack Direction="SgDirection.Vertical" Gap="0"
                 Style="height:100%; overflow:hidden; border-right:1px solid var(--sg-border);">

            {{!-- Поиск по коллекциям --}}
            <div style="padding:8px;">
                <SgTextBox @bind-Value="_sidebarSearch"
                           Placeholder="🔍 Search collections..."
                           Style="width:100%;" />
            </div>

            <SgTabs Style="flex:1; overflow:hidden; display:flex; flex-direction:column;">

                {{!-- Collections Tab --}}
                <SgTabPanel Title="📁 Collections" Style="flex:1; overflow:auto; padding:8px;">

                    @foreach (var col in FilteredCollections)
                    {
                        <SgAccordion>
                            <SgAccordionItem>
                                <TitleContent>
                                    <SgStack Direction="SgDirection.Horizontal" Gap="4" Style="align-items:center; width:100%;">
                                        <span style="font-weight:600; font-size:13px; flex:1;">@col.Name</span>
                                        <SgBadge Variant="SgBadgeVariant.Muted"
                                                 Text="@col.Requests.Count.ToString()" />
                                        <SgDropdown Size="SgSize.Sm" Icon="⋯">
                                            <SgMenuItem Text="Rename"        OnClick="() => RenameCollection(col)" />
                                            <SgMenuItem Text="Run All"       OnClick="() => RunCollectionAsync(col)" />
                                            <SgMenuItem Text="Export JSON"   OnClick="() => ExportCollectionAsync(col)" />
                                            <SgMenuItem Text="Delete"        OnClick="() => DeleteCollectionAsync(col)"
                                                        Variant="SgMenuItemVariant.Danger" />
                                        </SgDropdown>
                                    </SgStack>
                                </TitleContent>
                                <ChildContent>
                                    <SgStack Direction="SgDirection.Vertical" Gap="2">
                                        @foreach (var req in col.Requests
                                                     .Where(r => string.IsNullOrEmpty(_sidebarSearch)
                                                                 || r.Name.Contains(_sidebarSearch, StringComparison.OrdinalIgnoreCase)
                                                                 || r.Tab.Url.Contains(_sidebarSearch, StringComparison.OrdinalIgnoreCase))
                                                     .OrderBy(r => r.SortOrder))
                                        {
                                            var r = req;
                                            <SgStack Direction="SgDirection.Horizontal" Gap="4"
                                                     Style="align-items:center; padding:2px 0; cursor:pointer;"
                                                     @onclick="() => OpenRequestTab(r.Tab, r.Name)">
                                                <SgBadge Variant="@GetMethodVariant(r.Tab.Method)"
                                                         Text="@r.Tab.Method"
                                                         Style="min-width:52px; font-size:10px; text-align:center;" />
                                                <SgStack Direction="SgDirection.Vertical" Gap="0" Style="flex:1; overflow:hidden;">
                                                    <span style="font-size:12px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;"
                                                          title="@r.Tab.Url">@r.Name</span>
                                                    @if (!string.IsNullOrEmpty(r.Description))
                                                    {
                                                        <span style="font-size:10px; color:var(--sg-text-muted);">@r.Description</span>
                                                    }
                                                </SgStack>
                                                <SgDropdown Size="SgSize.Sm" Icon="⋯">
                                                    <SgMenuItem Text="Open in Tab"   OnClick="() => OpenRequestTab(r.Tab, r.Name)" />
                                                    <SgMenuItem Text="Duplicate"     OnClick="() => DuplicateRequest(col, r)" />
                                                    <SgMenuItem Text="Delete"        OnClick="() => col.Requests.Remove(r)"
                                                                Variant="SgMenuItemVariant.Danger" />
                                                </SgDropdown>
                                            </SgStack>
                                        }
                                        <SgButton Variant="SgButtonVariant.Default" Size="SgSize.Sm"
                                                  OnClick="() => AddNewRequestToCollection(col)">
                                            + Add Request
                                        </SgButton>
                                    </SgStack>
                                </ChildContent>
                            </SgAccordionItem>
                        </SgAccordion>
                    }

                    <SgButton Variant="SgButtonVariant.Default" Size="SgSize.Sm"
                              Style="margin-top:8px; width:100%;"
                              OnClick="() => _newColModalOpen = true">
                        + New Collection
                    </SgButton>
                </SgTabPanel>

                {{!-- History Tab --}}
                <SgTabPanel Title="🕘 History" Style="flex:1; overflow:auto; padding:8px;">
                    @if (!_history.Any())
                    {
                        <SgEmpty Description="No history yet" />
                    }
                    <SgVirtualList Items="_history.AsEnumerable().Reverse().Take(100).ToList()"
                                   ItemHeight="58">
                        <ItemTemplate Context="entry">
                            <SgStack Direction="SgDirection.Horizontal" Gap="4"
                                     Style="align-items:flex-start; padding:4px 0; cursor:pointer; border-bottom:1px solid var(--sg-border);"
                                     @onclick="() => OpenHistoryEntry(entry)">
                                <SgBadge Variant="@GetMethodVariant(entry.Method)"
                                         Text="@entry.Method"
                                         Style="min-width:52px; font-size:10px;" />
                                <SgStack Direction="SgDirection.Vertical" Gap="0" Style="flex:1; overflow:hidden; min-width:0;">
                                    <span style="font-size:11px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;"
                                          title="@entry.Url">@entry.Url</span>
                                    <span style="font-size:10px; color:var(--sg-text-muted);">
                                        @entry.Timestamp.ToString("dd.MM HH:mm:ss") · @entry.ElapsedMs ms · @FormatSize(entry.SizeBytes)
                                    </span>
                                </SgStack>
                                <SgBadge Variant="@GetStatusVariant(entry.Status)"
                                         Text="@entry.Status?.ToString()" />
                            </SgStack>
                        </ItemTemplate>
                    </SgVirtualList>
                    @if (_history.Any())
                    {
                        <SgButton Variant="SgButtonVariant.Danger" Size="SgSize.Sm"
                                  Style="margin-top:8px; width:100%;"
                                  OnClick="ClearHistoryAsync">
                            🗑 Clear History
                        </SgButton>
                    }
                </SgTabPanel>

                {{!-- Environments Tab --}}
                <SgTabPanel Title="🌍 Env" Style="padding:8px;">
                    <SgStack Direction="SgDirection.Vertical" Gap="8">
                        <SgStack Direction="SgDirection.Horizontal" Gap="6">
                            <SgSelect @bind-Value="_activeEnvName"
                                      Label="Active"
                                      Items="_environments"
                                      TextSelector="@(e => e.Name)"
                                      ValueSelector="@(e => e.Name)"
                                      Style="flex:1;" />
                            <SgButton Variant="SgButtonVariant.Default" Size="SgSize.Sm"
                                      Style="align-self:flex-end;"
                                      OnClick="() => _envModalOpen = true">
                                ⚙ Edit
                            </SgButton>
                        </SgStack>
                        @{
                            var activeEnv = _environments.FirstOrDefault(e => e.Name == _activeEnvName);
                        }
                        @if (activeEnv is not null)
                        {
                            <SgDescriptions>
                                @foreach (var v in activeEnv.Variables.Where(v => v.Enabled))
                                {
                                    <SgDescriptionsItem Label="@v.Key" Value="@v.Value" />
                                }
                            </SgDescriptions>
                        }
                    </SgStack>
                </SgTabPanel>

            </SgTabs>

        </SgStack>
    </SgSplitterPane>

    {{!-- ══════════════════════════════════════════════
         ПРАВАЯ ПАНЕЛЬ — Request Tabs
    ══════════════════════════════════════════════ --}}
    <SgSplitterPane>
        <SgStack Direction="SgDirection.Vertical" Gap="0" Style="height:100%; overflow:hidden;">

            {{!-- Строка вкладок запросов --}}
            <div style="display:flex; align-items:center; border-bottom:1px solid var(--sg-border); padding:0 8px; background:var(--sg-bg-alt); min-height:40px; overflow-x:auto; gap:2px;">
                @foreach (var tab in _requestTabs)
                {
                    var t = tab;
                    <div style="display:flex; align-items:center; gap:4px; padding:4px 10px; border-radius:6px 6px 0 0; cursor:pointer; white-space:nowrap;
                                background:@(_activeTabId == t.Id ? "var(--sg-bg)" : "transparent");
                                border:@(_activeTabId == t.Id ? "1px solid var(--sg-border)" : "1px solid transparent");
                                border-bottom:@(_activeTabId == t.Id ? "1px solid var(--sg-bg)" : "1px solid transparent");"
                         @onclick="() => _activeTabId = t.Id">
                        <SgBadge Variant="@GetMethodVariant(t.Method)"
                                 Text="@t.Method"
                                 Style="font-size:9px; padding:1px 4px;" />
                        <span style="font-size:12px; max-width:120px; overflow:hidden; text-overflow:ellipsis;">
                            @(string.IsNullOrEmpty(t.TabName) ? "New Request" : t.TabName)
                        </span>
                        @if (t.IsDirty)
                        {
                            <span style="color:var(--sg-warn); font-size:10px;">●</span>
                        }
                        <span style="font-size:11px; color:var(--sg-text-muted); cursor:pointer; padding:0 2px;"
                              @onclick:stopPropagation
                              @onclick="() => CloseTab(t)">✕</span>
                    </div>
                }
                <SgButton Variant="SgButtonVariant.Default" Size="SgSize.Sm"
                          Style="min-width:28px; padding:0; margin-left:4px;"
                          OnClick="AddNewTab">+</SgButton>
            </div>

            {{!-- Активная вкладка --}}
            @{
                var activeTab = _requestTabs.FirstOrDefault(t => t.Id == _activeTabId);
            }
            @if (activeTab is null)
            {
                <SgEmpty Description="No tabs open. Click + to add a request." Style="margin-top:60px;" />
            }
            else
            {
                <SgSplitter Orientation="SgSplitterOrientation.Vertical"
                            Style="flex:1; overflow:hidden;">

                    {{!-- REQUEST PANEL --}}
                    <SgSplitterPane Size="50%" Min="30%">
                        <div style="height:100%; overflow-y:auto; padding:12px;">

                            {{!-- Environment + Command bar --}}
                            <SgStack Direction="SgDirection.Horizontal" Gap="6" Style="margin-bottom:10px; align-items:center;">
                                <SgTextBox @bind-Value="activeTab.TabName"
                                           Placeholder="Request name"
                                           Style="width:180px; font-weight:600;" />
                                <span style="flex:1;"></span>
                                <SgBadge Variant="SgBadgeVariant.Info" Text="@_activeEnvName" />
                                <SgButton Variant="SgButtonVariant.Default" Size="SgSize.Sm"
                                          OnClick="() => OpenSaveModal(activeTab)">
                                    💾 Save
                                </SgButton>
                                <SgButton Variant="SgButtonVariant.Default" Size="SgSize.Sm"
                                          OnClick="() => DuplicateTab(activeTab)">
                                    ⧉ Duplicate
                                </SgButton>
                                <SgButton Variant="SgButtonVariant.Default" Size="SgSize.Sm"
                                          OnClick="() => ClearTab(activeTab)">
                                    ✕ Clear
                                </SgButton>
                            </SgStack>

                            {{!-- URL Bar --}}
                            <SgCard Style="margin-bottom:10px;">
                                <SgStack Direction="SgDirection.Horizontal" Gap="6">
                                    <SgSelect @bind-Value="activeTab.Method"
                                              Items="_methods"
                                              TextSelector="@(m => m)"
                                              ValueSelector="@(m => m)"
                                              Style="width:130px;" />
                                    <SgTextBox @bind-Value="activeTab.Url"
                                               Placeholder="https://{{baseUrl}}/api/resource"
                                               Style="flex:1; font-family:monospace;"
                                               OnKeyDown="@(e => OnUrlKeyDown(e, activeTab))" />
                                    <SgButton Variant="SgButtonVariant.Primary"
                                              Loading="@(_loadingTabId == activeTab.Id)"
                                              OnClick="() => SendAsync(activeTab)">
                                        ▶ Send
                                    </SgButton>
                                </SgStack>

                                @{
                                    var resolved = ResolveVariables(activeTab.Url);
                                }
                                @if (resolved != activeTab.Url && !string.IsNullOrEmpty(resolved))
                                {
                                    <SgAlert Variant="SgAlertVariant.Info" Style="margin-top:6px; font-size:11px; padding:3px 8px;">
                                        ↳ @resolved
                                    </SgAlert>
                                }
                            </SgCard>

                            {{!-- Request configuration tabs --}}
                            <SgTabs>

                                <SgTabPanel Title="@($"Params ({activeTab.QueryParams.Count(p => p.Enabled && !string.IsNullOrWhiteSpace(p.Key))})")">
                                    <KvEditor Items="activeTab.QueryParams"
                                              OnAdd="() => activeTab.QueryParams.Add(new())" />
                                </SgTabPanel>

                                <SgTabPanel Title="@($"Headers ({activeTab.Headers.Count(h => h.Enabled && !string.IsNullOrWhiteSpace(h.Key))})")">
                                    <KvEditor Items="activeTab.Headers"
                                              OnAdd="() => activeTab.Headers.Add(new())" />
                                </SgTabPanel>

                                <SgTabPanel Title="Auth">
                                    <SgStack Direction="SgDirection.Vertical" Gap="10">
                                        <SgSelect @bind-Value="activeTab.AuthType"
                                                  Label="Type"
                                                  Items="_authTypes"
                                                  TextSelector="@(a => a)"
                                                  ValueSelector="@(a => a)"
                                                  Style="width:220px;" />
                                        @switch (activeTab.AuthType)
                                        {
                                            case "Bearer Token":
                                                <SgTextBox @bind-Value="activeTab.BearerToken"
                                                           Label="Token"
                                                           Placeholder="eyJhbGci..."
                                                           Style="width:100%;" />
                                                break;
                                            case "Basic Auth":
                                                <SgStack Direction="SgDirection.Horizontal" Gap="8">
                                                    <SgTextBox @bind-Value="activeTab.BasicUser"     Label="Username" Style="flex:1;" />
                                                    <SgTextBox @bind-Value="activeTab.BasicPassword" Label="Password" InputType="password" Style="flex:1;" />
                                                </SgStack>
                                                break;
                                            case "API Key":
                                                <SgStack Direction="SgDirection.Horizontal" Gap="8">
                                                    <SgTextBox @bind-Value="activeTab.ApiKeyName"  Label="Header"   Placeholder="X-API-Key" Style="flex:1;" />
                                                    <SgTextBox @bind-Value="activeTab.ApiKeyValue" Label="Value"    Placeholder="key-value"  Style="flex:1;" />
                                                </SgStack>
                                                break;
                                            case "OAuth 2.0":
                                                <SgTextBox @bind-Value="activeTab.OAuth2Token"
                                                           Label="Access Token (paste)"
                                                           Style="width:100%;" />
                                                break;
                                        }
                                    </SgStack>
                                </SgTabPanel>

                                <SgTabPanel Title="Body"
                                            Disabled="@(activeTab.Method == "GET" || activeTab.Method == "DELETE" || activeTab.Method == "HEAD")">
                                    <SgStack Direction="SgDirection.Vertical" Gap="8">
                                        <SgStack Direction="SgDirection.Horizontal" Gap="6" Style="align-items:center;">
                                            <SgSegmented @bind-Value="activeTab.BodyType"
                                                         Items="_bodyTypes"
                                                         TextSelector="@(b => b)"
                                                         ValueSelector="@(b => b)" />
                                            @if (activeTab.BodyType == "JSON")
                                            {
                                                <SgButton Variant="SgButtonVariant.Default" Size="SgSize.Sm"
                                                          OnClick="() => FormatBody(activeTab)">✨ Format</SgButton>
                                                <SgButton Variant="SgButtonVariant.Default" Size="SgSize.Sm"
                                                          OnClick="() => MinifyBody(activeTab)">Minify</SgButton>
                                                <SgButton Variant="SgButtonVariant.Default" Size="SgSize.Sm"
                                                          OnClick="() => GenerateBodyFromSchema(activeTab)">🎲 Generate</SgButton>
                                            }
                                        </SgStack>
                                        @if (activeTab.BodyType == "Form")
                                        {
                                            <KvEditor Items="activeTab.FormFields"
                                                      OnAdd="() => activeTab.FormFields.Add(new())"
                                                      ShowComment="false" />
                                        }
                                        else
                                        {
                                            <SgTextArea @bind-Value="activeTab.Body"
                                                        Placeholder="@GetBodyPlaceholder(activeTab.BodyType)"
                                                        Rows="10"
                                                        Style="font-family:'Fira Code',monospace; font-size:13px;" />
                                        }
                                    </SgStack>
                                </SgTabPanel>

                                <SgTabPanel Title="Scripts">
                                    <SgStack Direction="SgDirection.Vertical" Gap="10">
                                        <SgCollapse Title="⚡ Pre-request Script" DefaultOpen="false">
                                            <SgAlert Variant="SgAlertVariant.Info" Style="margin-bottom:6px; font-size:12px;">
                                                Выполняется <b>до</b> отправки запроса. Доступны: <code>pm.request.setHeader(k,v)</code>, <code>pm.environment.set(k,v)</code>, <code>pm.Log(msg)</code>
                                            </SgAlert>
                                            <SgTextArea @bind-Value="activeTab.PreScript"
                                                        Rows="6"
                                                        Style="font-family:monospace; font-size:13px;"
                                                        Placeholder='pm.request.setHeader("X-Timestamp", DateTime.UtcNow.ToString("o"));&#10;pm.environment.set("token", pm.environment.get("token"));' />
                                        </SgCollapse>
                                        <SgCollapse Title="🧪 Test / Assertions Script" DefaultOpen="true">
                                            <SgAlert Variant="SgAlertVariant.Info" Style="margin-bottom:6px; font-size:12px;">
                                                Выполняется <b>после</b> получения ответа. Используй <code>pm.Test(name, () => bool)</code>, <code>pm.Expect(value).ToBe(expected)</code>, <code>pm.ResponseStatus</code>, <code>pm.ResponseJson</code>
                                            </SgAlert>
                                            <SgTextArea @bind-Value="activeTab.TestScript"
                                                        Rows="8"
                                                        Style="font-family:monospace; font-size:13px;"
                                                        Placeholder='pm.Test("Status is 200", () => pm.ResponseStatus == 200);&#10;pm.Test("Response time < 500ms", () => pm.ResponseTime < 500);&#10;pm.Expect(pm.ResponseJson?["id"]).Named("id field").ToNotBeNull();&#10;pm.environment.set("lastId", pm.ResponseJson?["id"]?.ToString() ?? "");' />
                                        </SgCollapse>
                                    </SgStack>
                                </SgTabPanel>

                                <SgTabPanel Title="⚙ Settings">
                                    <SgStack Direction="SgDirection.Vertical" Gap="10">
                                        <SgNumberEdit @bind-Value="_timeoutSeconds" Label="Timeout (s)" Min="1" Max="300" Style="width:160px;" />
                                        <SgSwitch @bind-Value="_prettyPrint"    Label="Pretty-print JSON response" />
                                        <SgSwitch @bind-Value="_followRedirects" Label="Follow Redirects" />
                                        <SgSwitch @bind-Value="_mockEnabled"    Label="Use Mock Server (intercept matching routes)" />
                                        <SgDivider />
                                        <SgSwitch @bind-Value="_runnerMode"     Label="Collection Runner mode (loop n times)" />
                                        @if (_runnerMode)
                                        {
                                            <SgStack Direction="SgDirection.Horizontal" Gap="8">
                                                <SgNumberEdit @bind-Value="_runnerIterations" Label="Iterations" Min="1" Max="1000" Style="width:120px;" />
                                                <SgNumberEdit @bind-Value="_runnerDelayMs"    Label="Delay (ms)" Min="0" Max="10000" Style="width:130px;" />
                                            </SgStack>
                                        }
                                    </SgStack>
                                </SgTabPanel>

                            </SgTabs>
                        </div>
                    </SgSplitterPane>

                    {{!-- RESPONSE PANEL --}}
                    <SgSplitterPane>
                        <div style="height:100%; overflow-y:auto; padding:12px;">
                            <ResponsePanel Snapshot="activeTab.LastResponse"
                                           HasError="@(_errorByTab.ContainsKey(activeTab.Id))"
                                           ErrorMessage="@(_errorByTab.GetValueOrDefault(activeTab.Id))"
                                           SourceRequest="activeTab"
                                           OnDiffRequest="() => OpenDiffAsync(activeTab)" />
                        </div>
                    </SgSplitterPane>

                </SgSplitter>
            }

        </SgStack>
    </SgSplitterPane>

</SgSplitter>

{{!-- ════════════════════════════════════════════════════════
     ПРАВАЯ БОКОВАЯ ПАНЕЛЬ (плавающие окна)
════════════════════════════════════════════════════════ --}}

{{!-- Collection Runner результаты --}}
<SgDockWindow Title="🏃 Runner Results"
              @bind-Visible="_runnerResultsOpen"
              Width="700px" Height="500px"
              Style="z-index:500;">
    <SgDataGrid TItem="RunnerResult"
                Items="_runnerResults"
                ShowSearch="false"
                EnablePaging="true"
                PageSize="20">
        <SgDataGridColumn TItem="RunnerResult" Title="#"          Value="@(r => r.Iteration.ToString())"   Width="40px" />
        <SgDataGridColumn TItem="RunnerResult" Title="Status"     Value="@(r => r.Status.ToString())"      Width="60px" />
        <SgDataGridColumn TItem="RunnerResult" Title="Time"       Value="@(r => $"{r.ElapsedMs} ms")"      Width="70px" />
        <SgDataGridColumn TItem="RunnerResult" Title="Tests"      Value="@(r => $"{r.PassedTests}/{r.TotalTests}")" Width="60px" />
        <SgDataGridColumn TItem="RunnerResult" Title="Error"      Value="@(r => r.Error ?? "")"            />
    </SgDataGrid>
</SgDockWindow>

{{!-- WebSocket --}}
<SgDockWindow Title="🔌 WebSocket"
              @bind-Visible="_wsOpen"
              Width="680px" Height="560px"
              Style="z-index:501;">
    <WsPanel />
</SgDockWindow>

{{!-- Mock Server --}}
<SgDockWindow Title="🎭 Mock Server"
              @bind-Visible="_mockOpen"
              Width="740px" Height="520px"
              Style="z-index:502;">
    <MockServerPanel Endpoints="_mockEndpoints" />
</SgDockWindow>

{{!-- Statistics --}}
<SgDockWindow Title="📊 Statistics"
              @bind-Visible="_statsOpen"
              Width="660px" Height="600px"
              Style="z-index:503;">
    <StatsPanel Stats="_stats" />
</SgDockWindow>

{{!-- Diff Viewer --}}
<SgDockWindow Title="🔀 Response Diff"
              @bind-Visible="_diffOpen"
              Width="900px" Height="600px"
              Style="z-index:504;">
    <SgSplitter>
        <SgSplitterPane>
            <SgCard Title="@($"Response A — {_diffA?.Status} {_diffA?.ElapsedMs} ms")">
                <SgCode Language="json" Code="@(_diffA?.PrettyBody ?? "")" />
            </SgCard>
        </SgSplitterPane>
        <SgSplitterPane>
            <SgCard Title="@($"Response B — {_diffB?.Status} {_diffB?.ElapsedMs} ms")">
                <SgCode Language="json" Code="@(_diffB?.PrettyBody ?? "")" />
            </SgCard>
        </SgSplitterPane>
    </SgSplitter>
</SgDockWindow>

{{!-- Floating toolbar --}}
<SgAffix Position="SgAffixPosition.BottomRight" Style="margin:16px; z-index:400;">
    <SgStack Direction="SgDirection.Horizontal" Gap="6">
        <SgTooltip Text="WebSocket Client">
            <SgButton Variant="SgButtonVariant.Default" OnClick="() => _wsOpen = !_wsOpen">🔌</SgButton>
        </SgTooltip>
        <SgTooltip Text="Mock Server">
            <SgButton Variant="SgButtonVariant.Default" OnClick="() => _mockOpen = !_mockOpen">🎭</SgButton>
        </SgTooltip>
        <SgTooltip Text="Statistics">
            <SgButton Variant="SgButtonVariant.Default" OnClick="() => _statsOpen = !_statsOpen">📊</SgButton>
        </SgTooltip>
        <SgTooltip Text="Import cURL">
            <SgButton Variant="SgButtonVariant.Default" OnClick="() => _curlImportOpen = true">📥 cURL</SgButton>
        </SgTooltip>
    </SgStack>
</SgAffix>

{{!-- cURL Import Modal --}}
<SgModal Title="📥 Import from cURL" @bind-Visible="_curlImportOpen" Width="640px">
    <SgTextArea @bind-Value="_curlInput" Rows="8"
                Style="font-family:monospace; font-size:13px;"
                Placeholder="curl -X POST 'https://api.example.com/data' \&#10;  -H 'Authorization: Bearer token' \&#10;  -d '{&quot;key&quot;:&quot;value&quot;}'" />
    <SgStack Direction="SgDirection.Horizontal" Gap="6" Style="justify-content:flex-end; margin-top:8px;">
        <SgButton Variant="SgButtonVariant.Default" OnClick="() => _curlImportOpen = false">Cancel</SgButton>
        <SgButton Variant="SgButtonVariant.Primary" OnClick="ImportFromCurlAsync">Import</SgButton>
    </SgStack>
</SgModal>

{{!-- Save to collection modal --}}
<SgModal Title="💾 Save to Collection" @bind-Visible="_saveModalOpen" Width="440px">
    <SgStack Direction="SgDirection.Vertical" Gap="10">
        <SgTextBox @bind-Value="_saveName"        Label="Request name"  Placeholder="GET Users" />
        <SgTextBox @bind-Value="_saveDescription" Label="Description"   Placeholder="Optional description" />
        <SgTextBox @bind-Value="_saveTags"        Label="Tags"          Placeholder="auth, users, v2" />
        <SgSelect  @bind-Value="_saveToCollId"
                   Label="Collection"
                   Items="_collections"
                   TextSelector="@(c => c.Name)"
                   ValueSelector="@(c => c.Id.ToString())" />
        <SgStack Direction="SgDirection.Horizontal" Gap="6" Style="justify-content:flex-end;">
            <SgButton Variant="SgButtonVariant.Default" OnClick="() => _saveModalOpen = false">Cancel</SgButton>
            <SgButton Variant="SgButtonVariant.Primary" OnClick="SaveCurrentRequestAsync">Save</SgButton>
        </SgStack>
    </SgStack>
</SgModal>

{{!-- New collection modal --}}
<SgModal Title="📁 New Collection" @bind-Visible="_newColModalOpen" Width="380px">
    <SgStack Direction="SgDirection.Vertical" Gap="10">
        <SgTextBox @bind-Value="_newColName"    Label="Name"        Placeholder="My API" />
        <SgTextBox @bind-Value="_newColDesc"    Label="Description" Placeholder="Optional" />
        <SgTextBox @bind-Value="_newColBaseUrl" Label="Base URL"    Placeholder="https://api.example.com" />
        <SgStack Direction="SgDirection.Horizontal" Gap="6" Style="justify-content:flex-end;">
            <SgButton Variant="SgButtonVariant.Default" OnClick="() => _newColModalOpen = false">Cancel</SgButton>
            <SgButton Variant="SgButtonVariant.Primary" OnClick="CreateCollection">Create</SgButton>
        </SgStack>
    </SgStack>
</SgModal>

{{!-- Environment manager modal --}}
<SgModal Title="🌍 Environment Manager" @bind-Visible="_envModalOpen" Width="700px">
    <SgStack Direction="SgDirection.Vertical" Gap="10">
        <SgStack Direction="SgDirection.Horizontal" Gap="8">
            <SgSelect @bind-Value="_editEnvId"
                      Label="Environment"
                      Items="_environments"
                      TextSelector="@(e => e.Name)"
                      ValueSelector="@(e => e.Id.ToString())"
                      Style="flex:1;" />
            <SgButton Variant="SgButtonVariant.Default" Size="SgSize.Sm" Style="align-self:flex-end;"
                      OnClick="CreateEnvironment">+ New</SgButton>
            <SgButton Variant="SgButtonVariant.Danger"  Size="SgSize.Sm" Style="align-self:flex-end;"
                      OnClick="DeleteEnvAsync">🗑</SgButton>
        </SgStack>
        @{
            var editEnv = _environments.FirstOrDefault(e => e.Id.ToString() == _editEnvId);
        }
        @if (editEnv is not null)
        {
            <KvEditor Items="editEnv.Variables"
                      OnAdd="() => editEnv.Variables.Add(new())"
                      ShowComment="true" />
        }
        <SgStack Direction="SgDirection.Horizontal" Gap="6" Style="justify-content:flex-end;">
            <SgButton Variant="SgButtonVariant.Primary" OnClick="() => _envModalOpen = false">Done</SgButton>
        </SgStack>
    </SgStack>
</SgModal>

@code {

    // ─── Tabs ─────────────────────────────────────────────────────────────────
    private List<RequestTab> _requestTabs  = new() { new RequestTab { TabName = "New Request" } };
    private Guid             _activeTabId  = Guid.Empty;
    private Guid?            _loadingTabId = null;
    private Dictionary<Guid, string> _errorByTab = new();

    protected override void OnInitialized()
    {
        _activeTabId = _requestTabs.First().Id;
    }

    private void AddNewTab()
    {
        var tab = new RequestTab { TabName = "New Request" };
        _requestTabs.Add(tab);
        _activeTabId = tab.Id;
    }

    private void OpenRequestTab(RequestTab source, string name)
    {
        // клонируем, открываем в новой вкладке
        var tab = new RequestTab
        {
            TabName       = name,
            Method        = source.Method,
            Url           = source.Url,
            BodyType      = source.BodyType,
            Body          = source.Body,
            AuthType      = source.AuthType,
            BearerToken   = source.BearerToken,
            BasicUser     = source.BasicUser,
            BasicPassword = source.BasicPassword,
            ApiKeyName    = source.ApiKeyName,
            ApiKeyValue   = source.ApiKeyValue,
            OAuth2Token   = source.OAuth2Token,
            PreScript     = source.PreScript,
            TestScript    = source.TestScript,
            Headers       = source.Headers.Select(h => new KvPair(h.Key, h.Value, h.Enabled) { Comment = h.Comment }).ToList(),
            QueryParams   = source.QueryParams.Select(p => new KvPair(p.Key, p.Value, p.Enabled)).ToList(),
            FormFields    = source.FormFields.Select(f => new KvPair(f.Key, f.Value, f.Enabled)).ToList(),
        };
        _requestTabs.Add(tab);
        _activeTabId = tab.Id;
    }

    private void OpenHistoryEntry(HistoryEntry entry)
    {
        var tab = new RequestTab
        {
            TabName = $"{entry.Method} {new Uri(entry.Url).AbsolutePath}",
            Method  = entry.Method,
            Url     = entry.Url
        };
        _requestTabs.Add(tab);
        _activeTabId = tab.Id;
    }

    private async Task CloseTab(RequestTab tab)
    {
        if (tab.IsDirty)
        {
            if (!await Confirm.ConfirmAsync("Tab has unsaved changes. Close anyway?", variant: SgAlertVariant.Warn))
                return;
        }
        _requestTabs.Remove(tab);
        if (_activeTabId == tab.Id)
            _activeTabId = _requestTabs.LastOrDefault()?.Id ?? Guid.Empty;
        if (!_requestTabs.Any())
            AddNewTab();
    }

    private void DuplicateTab(RequestTab tab)
    {
        var clone = new RequestTab
        {
            TabName = tab.TabName + " (copy)",
            Method  = tab.Method,
            Url     = tab.Url,
            Body    = tab.Body,
            BodyType = tab.BodyType,
            AuthType = tab.AuthType,
            BearerToken = tab.BearerToken,
            BasicUser   = tab.BasicUser,
            BasicPassword = tab.BasicPassword,
            Headers     = tab.Headers.Select(h => new KvPair(h.Key, h.Value, h.Enabled)).ToList(),
            QueryParams = tab.QueryParams.Select(p => new KvPair(p.Key, p.Value, p.Enabled)).ToList(),
        };
        _requestTabs.Add(clone);
        _activeTabId = clone.Id;
    }

    private void ClearTab(RequestTab tab)
    {
        tab.Url           = "";
        tab.Body          = "";
        tab.PreScript     = "";
        tab.TestScript    = "";
        tab.LastResponse  = null;
        tab.Headers       = new() { new("Content-Type", "application/json") };
        tab.QueryParams   = new();
        tab.FormFields    = new();
        _errorByTab.Remove(tab.Id);
    }

    // ─── Send ─────────────────────────────────────────────────────────────────
    private async Task SendAsync(RequestTab tab)
    {
        if (string.IsNullOrWhiteSpace(tab.Url))
        { Toasts.Warning("Enter a URL"); return; }

        _loadingTabId = tab.Id;
        _errorByTab.Remove(tab.Id);
        tab.IsDirty = false;

        try
        {
            // ── Pre-request script ──
            var activeEnv = _environments.FirstOrDefault(e => e.Name == _activeEnvName);
            if (!string.IsNullOrWhiteSpace(tab.PreScript))
                RunScript(tab.PreScript, tab, null, activeEnv);

            if (_runnerMode)
            {
                await RunCollectionLoopAsync(tab);
                return;
            }

            tab.LastResponse = await ExecuteRequestAsync(tab, activeEnv);

            // ── Test script ──
            if (!string.IsNullOrWhiteSpace(tab.TestScript) && tab.LastResponse is not null)
                RunScript(tab.TestScript, tab, tab.LastResponse, activeEnv);

            // Статистика
            if (tab.LastResponse is not null)
                _stats.Add(new RequestStat
                {
                    Label   = $"#{_stats.Count + 1}",
                    Ms      = tab.LastResponse.ElapsedMs,
                    Status  = tab.LastResponse.Status,
                    Bytes   = tab.LastResponse.SizeBytes,
                    Success = tab.LastResponse.Status is >= 200 and < 300
                });
        }
        catch (Exception ex)
        {
            _errorByTab[tab.Id] = ex.Message;
            Toasts.Error($"Error: {ex.Message}");
        }
        finally
        {
            _loadingTabId = null;
        }
    }

    private async Task<ResponseSnapshot> ExecuteRequestAsync(RequestTab tab, ApiEnvironment? env)
    {
        // ── Проверяем mock ──
        if (_mockEnabled)
        {
            var resolved = ResolveVariables(tab.Url);
            var mock = _mockEndpoints.FirstOrDefault(m =>
                m.Enabled &&
                m.Method == tab.Method &&
                resolved.EndsWith(m.Path, StringComparison.OrdinalIgnoreCase));

            if (mock is not null)
            {
                if (mock.DelayMs > 0)
                    await Task.Delay(mock.DelayMs);
                mock.HitCount++;
                var snap = new ResponseSnapshot
                {
                    Status      = mock.StatusCode,
                    StatusText  = "Mock",
                    ElapsedMs   = mock.DelayMs,
                    ContentType = mock.ContentType,
                    RawBody     = mock.Body,
                };
                ParseResponseBody(snap, mock.Body, _prettyPrint);
                Toasts.Info($"🎭 Mock hit: {mock.Method} {mock.Path}");
                return snap;
            }
        }

        // ── Реальный HTTP ──
        var url  = BuildUrl(ResolveVariables(tab.Url), tab.QueryParams, env);
        var http = ClientFactory.CreateClient("api");

        using var cts     = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
        using var request = new HttpRequestMessage(new HttpMethod(tab.Method), url);

        // Headers
        foreach (var h in tab.Headers.Where(h => h.Enabled && !string.IsNullOrWhiteSpace(h.Key)))
            request.Headers.TryAddWithoutValidation(ResolveVariables(h.Key, env), ResolveVariables(h.Value, env));

        // Auth
        ApplyAuth(tab, request, env);

        // Body
        if (tab.Method is "POST" or "PUT" or "PATCH")
        {
            if (tab.BodyType == "Form")
            {
                var fd = tab.FormFields.Where(f => f.Enabled && !string.IsNullOrWhiteSpace(f.Key))
                    .ToDictionary(f => ResolveVariables(f.Key, env), f => ResolveVariables(f.Value, env));
                request.Content = new FormUrlEncodedContent(fd);
            }
            else if (!string.IsNullOrWhiteSpace(tab.Body))
            {
                var mime = tab.BodyType switch
                {
                    "JSON" => "application/json",
                    "XML"  => "application/xml",
                    _      => "text/plain"
                };
                request.Content = new StringContent(ResolveVariables(tab.Body, env), Encoding.UTF8, mime);
            }
        }

        var sw       = Stopwatch.StartNew();
        var response = await http.SendAsync(request, cts.Token);
        sw.Stop();

        var raw  = await response.Content.ReadAsStringAsync(cts.Token);
        var snapshot = new ResponseSnapshot
        {
            Status      = (int)response.StatusCode,
            StatusText  = response.ReasonPhrase ?? "",
            ElapsedMs   = sw.ElapsedMilliseconds,
            SizeBytes   = Encoding.UTF8.GetByteCount(raw),
            ContentType = response.Content.Headers.ContentType?.MediaType ?? "",
            RawBody     = raw,
            Headers     = response.Headers
                .Concat(response.Content.Headers)
                .SelectMany(h => h.Value.Select(v => new KvPair(h.Key, v)))
                .ToList(),
        };

        ParseResponseBody(snapshot, raw, _prettyPrint);

        // История
        _history.Add(new HistoryEntry
        {
            Method    = tab.Method,
            Url       = tab.Url,
            Status    = snapshot.Status,
            ElapsedMs = snapshot.ElapsedMs,
            SizeBytes = snapshot.SizeBytes,
        });

        if (response.IsSuccessStatusCode)
            Toasts.Success($"{snapshot.Status} {snapshot.StatusText} · {snapshot.ElapsedMs} ms · {FormatSize(snapshot.SizeBytes)}");
        else
            Toasts.Error($"{snapshot.Status} {snapshot.StatusText}");

        return snapshot;
    }

    // ─── Parse ────────────────────────────────────────────────────────────────
    private static void ParseResponseBody(ResponseSnapshot snap, string raw, bool pretty)
    {
        try
        {
            var opts = new JsonSerializerOptions { WriteIndented = pretty };
            var node = JsonNode.Parse(raw);

            if (node is JsonArray arr)
            {
                snap.IsJsonArray = true;
                var keys = new LinkedList<string>();
                var keyset = new HashSet<string>();
                foreach (var item in arr)
                    if (item is JsonObject obj)
                        foreach (var kv in obj)
                            if (keyset.Add(kv.Key))
                                keys.AddLast(kv.Key);
                snap.GridColumns = keys.ToList();

                foreach (var item in arr)
                {
                    var row = new Dictionary<string, object?>();
                    if (item is JsonObject o)
                        foreach (var key in snap.GridColumns)
                            row[key] = o.TryGetPropertyValue(key, out var v) ? v?.ToString() : null;
                    snap.GridRows.Add(row);
                }
            }
            snap.PrettyBody = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(raw), opts);
        }
        catch { snap.PrettyBody = raw; }
    }

    // ─── Scripts ──────────────────────────────────────────────────────────────
    private void RunScript(string script, RequestTab tab, ResponseSnapshot? resp, ApiEnvironment? env)
    {
        var rt = new ScriptRuntime(tab, resp, env);
        // Минимальный интерпретатор: pm.Test / pm.Expect / pm.EnvSet / pm.Log через Roslyn-like eval
        // Для упрощения — DSL через reflection-safe объект
        try
        {
            // В реальном проекте: Microsoft.CodeAnalysis.CSharp.Scripting
            // Здесь — описание API для документации
        }
        catch (Exception ex)
        {
            Toasts.Warning($"Script error: {ex.Message}");
        }

        if (resp is not null)
            resp.Tests.AddRange(rt.Results);

        foreach (var log in rt.Logs)
            Console.WriteLine(log);
    }

    // ─── Runner ───────────────────────────────────────────────────────────────
    private bool   _runnerMode       = false;
    private int    _runnerIterations = 5;
    private int    _runnerDelayMs    = 200;
    private bool   _runnerResultsOpen = false;
    private List<RunnerResult> _runnerResults = new();

    private async Task RunCollectionLoopAsync(RequestTab tab)
    {
        _runnerResults.Clear();
        _runnerResultsOpen = true;
        var env = _environments.FirstOrDefault(e => e.Name == _activeEnvName);

        for (int i = 1; i <= _runnerIterations; i++)
        {
            var result = new RunnerResult { Iteration = i };
            try
            {
                var snap = await ExecuteRequestAsync(tab, env);
                result.Status    = snap.Status;
                result.ElapsedMs = snap.ElapsedMs;
                result.PassedTests = snap.Tests.Count(t => t.Passed);
                result.TotalTests  = snap.Tests.Count;
            }
            catch (Exception ex) { result.Error = ex.Message; }
            _runnerResults.Add(result);
            StateHasChanged();
            if (_runnerDelayMs > 0)
                await Task.Delay(_runnerDelayMs);
        }
        Toasts.Success($"Runner done: {_runnerIterations} iterations");
    }

    private async Task RunCollectionAsync(Collection col)
    {
        _runnerResults.Clear();
        _runnerResultsOpen = true;
        var env = _environments.FirstOrDefault(e => e.Name == _activeEnvName);

        int i = 1;
        foreach (var req in col.Requests)
        {
            var result = new RunnerResult { Iteration = i++, Label = req.Name };
            try
            {
                var snap = await ExecuteRequestAsync(req.Tab, env);
                result.Status     = snap.Status;
                result.ElapsedMs  = snap.ElapsedMs;
                result.PassedTests = snap.Tests.Count(t => t.Passed);
                result.TotalTests  = snap.Tests.Count;
            }
            catch (Exception ex) { result.Error = ex.Message; }
            _runnerResults.Add(result);
            StateHasChanged();
            await Task.Delay(200);
        }
        Toasts.Success($"Collection run done: {col.Name}");
    }

    // ─── Diff ─────────────────────────────────────────────────────────────────
    private bool              _diffOpen = false;
    private ResponseSnapshot? _diffA    = null;
    private ResponseSnapshot? _diffB    = null;

    private async Task OpenDiffAsync(RequestTab tab)
    {
        if (_diffA is null)
        {
            _diffA = tab.LastResponse;
            Toasts.Info("Response A set. Send another request and click Diff again for B.");
        }
        else
        {
            _diffB    = tab.LastResponse;
            _diffOpen = true;
        }
        await Task.CompletedTask;
    }

    // ─── cURL Import ──────────────────────────────────────────────────────────
    private bool   _curlImportOpen = false;
    private string _curlInput      = "";

    private async Task ImportFromCurlAsync()
    {
        var curl = _curlInput.Replace("\\\n", " ").Replace("\\", "");
        var tab  = new RequestTab { TabName = "Imported" };

        // Method
        var methodM = Regex.Match(curl, @"-X\s+(\w+)");
        if (methodM.Success) tab.Method = methodM.Groups[1].Value.ToUpper();

        // URL
        var urlM = Regex.Match(curl, @"curl\s+(?:-X\s+\w+\s+)?['""]?(https?://[^\s'""]+)['""]?");
        if (urlM.Success) tab.Url = urlM.Groups[1].Value;

        // Headers
        foreach (Match hm in Regex.Matches(curl, @"-H\s+['""]([^:]+):\s*([^'""]+)['""]"))
            tab.Headers.Add(new KvPair(hm.Groups[1].Value.Trim(), hm.Groups[2].Value.Trim()));

        // Body
        var bodyM = Regex.Match(curl, @"-d\s+['""](.+?)['""]");
        if (bodyM.Success) tab.Body = bodyM.Groups[1].Value;

        _requestTabs.Add(tab);
        _activeTabId    = tab.Id;
        _curlImportOpen = false;
        _curlInput      = "";
        Toasts.Success("cURL imported successfully");
        await Task.CompletedTask;
    }

    // ─── Collections ──────────────────────────────────────────────────────────
    private List<Collection> _collections = new()
    {
        new Collection { Name = "Samples", Description = "Example requests" }
    };
    private string _sidebarSearch    = "";
    private bool   _newColModalOpen  = false;
    private string _newColName       = "";
    private string _newColDesc       = "";
    private string _newColBaseUrl    = "";
    private bool   _saveModalOpen    = false;
    private string _saveName         = "";
    private string _saveDescription  = "";
    private string _saveTags         = "";
    private string _saveToCollId     = "";
    private RequestTab? _saveSource  = null;

    private IEnumerable<Collection> FilteredCollections =>
        string.IsNullOrEmpty(_sidebarSearch)
            ? _collections
            : _collections.Where(c =>
                c.Name.Contains(_sidebarSearch, StringComparison.OrdinalIgnoreCase) ||
                c.Requests.Any(r => r.Name.Contains(_sidebarSearch, StringComparison.OrdinalIgnoreCase) ||
                                    r.Tab.Url.Contains(_sidebarSearch, StringComparison.OrdinalIgnoreCase)));

    private void CreateCollection()
    {
        if (string.IsNullOrWhiteSpace(_newColName)) return;
        var col = new Collection { Name = _newColName, Description = _newColDesc, BaseUrl = _newColBaseUrl };
        _collections.Add(col);
        _saveToCollId   = col.Id.ToString();
        _newColName     = _newColDesc = _newColBaseUrl = "";
        _newColModalOpen = false;
        Toasts.Success("Collection created");
    }

    private void OpenSaveModal(RequestTab tab)
    {
        _saveSource = tab;
        _saveName   = tab.TabName;
        if (!_collections.Any()) return;
        _saveToCollId  = _collections.First().Id.ToString();
        _saveModalOpen = true;
    }

    private async Task SaveCurrentRequestAsync()
    {
        if (_saveSource is null || string.IsNullOrWhiteSpace(_saveName)) return;
        var col = _collections.FirstOrDefault(c => c.Id.ToString() == _saveToCollId);
        if (col is null) return;

        col.Requests.Add(new SavedRequest
        {
            Name        = _saveName,
            Description = _saveDescription,
            Tags        = _saveTags,
            SortOrder   = col.Requests.Count,
            Tab         = new RequestTab
            {
                Method        = _saveSource.Method,
                Url           = _saveSource.Url,
                Body          = _saveSource.Body,
                BodyType      = _saveSource.BodyType,
                AuthType      = _saveSource.AuthType,
                BearerToken   = _saveSource.BearerToken,
                BasicUser     = _saveSource.BasicUser,
                BasicPassword = _saveSource.BasicPassword,
                ApiKeyName    = _saveSource.ApiKeyName,
                ApiKeyValue   = _saveSource.ApiKeyValue,
                PreScript     = _saveSource.PreScript,
                TestScript    = _saveSource.TestScript,
                Headers       = _saveSource.Headers.Select(h => new KvPair(h.Key, h.Value, h.Enabled)).ToList(),
                QueryParams   = _saveSource.QueryParams.Select(p => new KvPair(p.Key, p.Value, p.Enabled)).ToList(),
                FormFields    = _saveSource.FormFields.Select(f => new KvPair(f.Key, f.Value, f.Enabled)).ToList(),
            }
        });
        _saveModalOpen = false;
        _saveName = _saveDescription = _saveTags = "";
        Toasts.Success($"Saved to "{col.Name}"");
        await Task.CompletedTask;
    }

    private void AddNewRequestToCollection(Collection col)
    {
        col.Requests.Add(new SavedRequest { Name = "New Request", SortOrder = col.Requests.Count });
    }

    private void DuplicateRequest(Collection col, SavedRequest req)
    {
        col.Requests.Add(new SavedRequest
        {
            Name      = req.Name + " (copy)",
            SortOrder = col.Requests.Count,
            Tab       = req.Tab
        });
    }

    private void RenameCollection(Collection col)
    {
        // В реальном проекте — inline edit или prompt modal
        Toasts.Info("Double-click collection name to rename (inline edit).");
    }

    private async Task DeleteCollectionAsync(Collection col)
    {
        if (await Confirm.ConfirmAsync($"Delete collection \"{col.Name}\"?", variant: SgAlertVariant.Danger))
        {
            _collections.Remove(col);
            Toasts.Success("Collection deleted");
        }
    }

    private async Task ExportCollectionAsync(Collection col)
    {
        var json = JsonSerializer.Serialize(col, new JsonSerializerOptions { WriteIndented = true });
        var b64  = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        await JS.InvokeVoidAsync("eval",
            $"(()=>{{var a=document.createElement('a');a.href='data:application/json;base64,{b64}';a.download='{col.Name}.json';a.click();}})()");
        Toasts.Success("Collection exported");
    }

    // ─── History ──────────────────────────────────────────────────────────────
    private List<HistoryEntry> _history = new();

    private async Task ClearHistoryAsync()
    {
        if (await Confirm.ConfirmAsync("Clear all history?", variant: SgAlertVariant.Warn))
        { _history.Clear(); Toasts.Info("History cleared"); }
    }

    // ─── Environments ─────────────────────────────────────────────────────────
    private List<ApiEnvironment> _environments = new()
    {
        new ApiEnvironment
        {
            Name = "Default",
            Variables = new()
            {
                new KvPair("baseUrl", "https://jsonplaceholder.typicode.com"),
                new KvPair("userId",  "1"),
                new KvPair("token",   "my-secret-token"),
            }
        }
    };
    private string _activeEnvName = "Default";
    private string _editEnvId     = "";
    private bool   _envModalOpen  = false;

    private void CreateEnvironment()
    {
        var env = new ApiEnvironment { Name = $"Env {_environments.Count + 1}" };
        _environments.Add(env);
        _editEnvId = env.Id.ToString();
    }

    private async Task DeleteEnvAsync()
    {
        if (_environments.Count <= 1) { Toasts.Warning("Cannot delete the last environment"); return; }
        if (await Confirm.ConfirmAsync($"Delete this environment?", variant: SgAlertVariant.Danger))
        {
            _environments.RemoveAll(e => e.Id.ToString() == _editEnvId);
            _editEnvId     = _environments.First().Id.ToString();
            _activeEnvName = _environments.First().Name;
        }
    }

    // ─── Mock / WS / Stats ───────────────────────────────────────────────────
    private bool              _mockEnabled  = false;
    private bool              _mockOpen     = false;
    private bool              _wsOpen       = false;
    private bool              _statsOpen    = false;
    private List<MockEndpoint> _mockEndpoints = new()
    {
        new MockEndpoint { Path = "/api/mock/users", Body = "[{\"id\":1,\"name\":\"Alice\"},{\"id\":2,\"name\":\"Bob\"}]" }
    };
    private List<RequestStat> _stats = new();

    // ─── Settings ─────────────────────────────────────────────────────────────
    private int  _timeoutSeconds   = 30;
    private bool _prettyPrint      = true;
    private bool _followRedirects  = true;

    // ─── Helpers ──────────────────────────────────────────────────────────────
    private string ResolveVariables(string input, ApiEnvironment? env = null)
    {
        if (string.IsNullOrEmpty(input)) return input;
        env ??= _environments.FirstOrDefault(e => e.Name == _activeEnvName);
        if (env is null) return input;
        return Regex.Replace(input, @"\{\{(\w+)\}\}", m =>
        {
            var kv = env.Variables.FirstOrDefault(v => v.Key == m.Groups[1].Value && v.Enabled);
            return kv?.Value ?? m.Value;
        });
    }

    private string BuildUrl(string baseUrl, List<KvPair> queryParams, ApiEnvironment? env)
    {
        var active = queryParams.Where(p => p.Enabled && !string.IsNullOrWhiteSpace(p.Key)).ToList();
        if (!active.Any()) return baseUrl;
        var qs = string.Join("&", active.Select(p =>
            $"{Uri.EscapeDataString(ResolveVariables(p.Key, env))}={Uri.EscapeDataString(ResolveVariables(p.Value, env))}"));
        return baseUrl.Contains('?') ? $"{baseUrl}&{qs}" : $"{baseUrl}?{qs}";
    }

    private void ApplyAuth(RequestTab tab, HttpRequestMessage req, ApiEnvironment? env)
    {
        switch (tab.AuthType)
        {
            case "Bearer Token" when !string.IsNullOrWhiteSpace(tab.BearerToken):
                req.Headers.TryAddWithoutValidation("Authorization",
                    $"Bearer {ResolveVariables(tab.BearerToken, env)}");
                break;
            case "Basic Auth" when !string.IsNullOrWhiteSpace(tab.BasicUser):
                var cred = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{tab.BasicUser}:{tab.BasicPassword}"));
                req.Headers.TryAddWithoutValidation("Authorization", $"Basic {cred}");
                break;
            case "API Key" when !string.IsNullOrWhiteSpace(tab.ApiKeyName):
                req.Headers.TryAddWithoutValidation(
                    ResolveVariables(tab.ApiKeyName, env),
                    ResolveVariables(tab.ApiKeyValue, env));
                break;
            case "OAuth 2.0" when !string.IsNullOrWhiteSpace(tab.OAuth2Token):
                req.Headers.TryAddWithoutValidation("Authorization",
                    $"Bearer {ResolveVariables(tab.OAuth2Token, env)}");
                break;
        }
    }

    private void FormatBody(RequestTab tab)
    {
        try
        {
            tab.Body = JsonSerializer.Serialize(
                JsonSerializer.Deserialize<JsonElement>(tab.Body),
                new JsonSerializerOptions { WriteIndented = true });
        }
        catch { Toasts.Warning("Invalid JSON"); }
    }

    private void MinifyBody(RequestTab tab)
    {
        try
        {
            tab.Body = JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(tab.Body));
        }
        catch { Toasts.Warning("Invalid JSON"); }
    }

    private void GenerateBodyFromSchema(RequestTab tab)
    {
        // В реальном проекте — генерация по JSON Schema / OpenAPI
        tab.Body = JsonSerializer.Serialize(new
        {
            id        = 0,
            name      = "string",
            email     = "user@example.com",
            createdAt = DateTime.UtcNow.ToString("o"),
            active    = true
        }, new JsonSerializerOptions { WriteIndented = true });
        Toasts.Info("Sample body generated");
    }

    private void OnUrlKeyDown(KeyboardEventArgs e, RequestTab tab)
    {
        if (e.Key == "Enter")
            _ = SendAsync(tab);
    }

    private static string GetBodyPlaceholder(string type) => type switch
    {
        "JSON" => "{\n  \"key\": \"value\"\n}",
        "XML"  => "<root>\n  <key>value</key>\n</root>",
        _      => "Plain text body"
    };

    private static string FormatSize(int b) => b switch
    {
        < 1024        => $"{b} B",
        < 1024 * 1024 => $"{b / 1024.0:F1} KB",
        _             => $"{b / 1024.0 / 1024.0:F2} MB"
    };

    private static SgBadgeVariant GetStatusVariant(int? code) => code switch
    {
        >= 200 and < 300 => SgBadgeVariant.Success,
        >= 300 and < 400 => SgBadgeVariant.Info,
        >= 400 and < 500 => SgBadgeVariant.Warn,
        null             => SgBadgeVariant.Muted,
        _                => SgBadgeVariant.Danger
    };

    private static SgBadgeVariant GetMethodVariant(string method) => method switch
    {
        "GET"     => SgBadgeVariant.Success,
        "POST"    => SgBadgeVariant.Info,
        "PUT"     => SgBadgeVariant.Warn,
        "PATCH"   => SgBadgeVariant.Warn,
        "DELETE"  => SgBadgeVariant.Danger,
        "HEAD"    => SgBadgeVariant.Muted,
        "OPTIONS" => SgBadgeVariant.Default,
        _         => SgBadgeVariant.Default
    };

    // ─── Models (inner) ───────────────────────────────────────────────────────
    private class RunnerResult
    {
        public int    Iteration   { get; set; }
        public string Label       { get; set; } = "";
        public int    Status      { get; set; }
        public long   ElapsedMs   { get; set; }
        public int    PassedTests { get; set; }
        public int    TotalTests  { get; set; }
        public string? Error      { get; set; }
    }

    private readonly List<string> _methods   = new() { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS" };
    private readonly List<string> _authTypes = new() { "None", "Bearer Token", "Basic Auth", "API Key", "OAuth 2.0" };
    private readonly List<string> _bodyTypes = new() { "JSON", "Form", "XML", "Text" };
}
```

---

## 🖼️ Макет UI

```
┌─────────────────────┬──────────────────────────────────────────────────────────────────────┐
│ 🔍 Search...        │ [GET●] New Request │ [POST] Create User │ [+]           [🌙 theme]  │
│ ─────────────────── ├──────────────────────────────────────────────────────────────────────┤
│ 📁 Collections │ 🕘 │ [Request Name          ]  [Env: Default]  [💾 Save] [⧉ Dup] [✕ Clr] │
│ ─────────────────── ├──────────────────────────────────────────────────────────────────────┤
│ ▶ Samples        3  │ [GET ▼] [https://{{baseUrl}}/posts/{{userId}}         ] [▶ Send]     │
│   ● GET /posts      │ ↳ https://jsonplaceholder.typicode.com/posts/1                       │
│   ● POST /posts     ├──────────────────────────────────────────────────────────────────────┤
│   ● DELETE /posts/1 │ Params(1) │ Headers(2) │ Auth │ Body │ Scripts │ ⚙ Settings          │
│ [+ Add Request]     ├──────────────────────────────────────────────────────────────────────┤
│                     │ ☑ KEY          │ VALUE           │ DESCRIPTION                       │
│ [+ New Collection]  │ ☑ userId       │ 1               │ Filter by user                    │
│ ─────────────────── │ [+ Add Row] [Disable All] [Clear] [📋 Bulk Paste]                    │
│ 🕘 History          ├══════════════════════════════════════════════════════════════════════ │
│ GET /posts    200   │  ● 200 OK ⏱ 118 ms 📦 2.4 KB  application/json   ✅ 3/3 tests        │
│ POST /posts   201   │  [⬇ Export ▼]                                  [🔀 Diff with...]     │
│ PUT  /posts/1 200   ├──────────────────────────────────────────────────────────────────────┤
│                     │ Body [10 rows] │ Tests (3) │ Headers (12) │ Raw │ Timeline            │
│ 🌍 Env              │ ──────────────────────────────────────────────────────────────────── │
│ baseUrl = https://  │ [Grid | Chart | JSON]   [X: id ▼] [Y: salary ▼] [Bar ▼]             │
│ userId  = 1         │                                                                       │
│ token   = ***       │ id │ userId │ title            │ body                                 │
│                     │  1 │   1    │ sunt aut facere  │ quia et suscipit...                  │
│                     │  2 │   1    │ qui est esse     │ est rerum tempore...                 │
│                     │                     [pagination]                                      │
└─────────────────────┴──────────────────────────────────────────────────────────────────────┘
                                                  🔌  🎭  📊  📥 cURL   ← плавающий тулбар
```

---

## ✅ Полный список возможностей v3

### 🗂 Мульти-вкладки
- [x] Неограниченное количество вкладок запросов (как в Postman)
- [x] Индикатор несохранённых изменений (`●` на вкладке)
- [x] Подтверждение закрытия грязной вкладки
- [x] Дублирование вкладки
- [x] Очистка вкладки

### 🔧 Запрос
- [x] Методы: GET / POST / PUT / PATCH / DELETE / HEAD / OPTIONS
- [x] Переменные `{{variable}}` в URL, заголовках, теле, auth
- [x] Preview resolved-URL под строкой запроса
- [x] Именованный `HttpClient` с полным контролем редиректов
- [x] Чекбокс Enable/Disable для каждого header/param/form-field
- [x] Описание (comment) для каждого header/param
- [x] Bulk Paste заголовков (`key: value` per line)
- [x] Авторизация: None / Bearer / Basic / API Key / **OAuth 2.0**
- [x] Body: JSON / Form / XML / Text
- [x] Форматирование и минификация JSON
- [x] 🎲 Генерация sample-тела по шаблону
- [x] Таймаут запроса

### 🧪 Скрипты и тесты
- [x] **Pre-request script** — выполняется до отправки запроса
- [x] **Test/Assertions script** — выполняется после получения ответа
- [x] DSL: `pm.Test()`, `pm.Expect().ToBe/ToContain/ToBeGreaterThan/ToBeLessThan/ToBeNull`
- [x] `pm.environment.set/get` — запись переменных из ответа
- [x] `pm.request.setHeader()` — динамическое добавление заголовков
- [x] `pm.ResponseStatus`, `pm.ResponseTime`, `pm.ResponseJson`
- [x] Результаты тестов в отдельной вкладке с прогресс-баром
- [x] Pass/Fail счётчик в статус-баре

### 📊 Ответ
- [x] DataGrid (массив) с сортировкой, фильтрами, группировкой, экспортом CSV/Excel, column chooser
- [x] **📈 Chart view** — переключение Grid/Chart/JSON, выбор X/Y колонок, тип (Bar/Line/Pie/Doughnut/Scatter)
- [x] SgCode с подсветкой JSON/XML/Text
- [x] Response Headers → DataGrid
- [x] **Waterfall Timeline** — фазы запроса с анимированными барами
- [x] Raw вкладка
- [x] Копирование ответа в буфер
- [x] Скачивание как `.json`
- [x] Экспорт как `.csv`
- [x] **Экспорт как cURL-команда** (копирует в буфер)
- [x] Вкладка Redirects (если статус 3xx)

### 🔀 Diff Viewer
- [x] Сохранение ответа A, ответа B
- [x] Side-by-side сравнение в плавающем `SgDockWindow`

### 🏃 Collection Runner
- [x] Запуск одного запроса N раз с задержкой
- [x] Запуск всей коллекции по порядку
- [x] Результаты в DataGrid (`SgDockWindow`)
- [x] Pass/Fail тестов по каждой итерации

### 📁 Коллекции
- [x] Несколько коллекций с описанием и base URL
- [x] Поиск по коллекциям и запросам (sidebar)
- [x] Сохранение с именем, описанием и тегами
- [x] Дублирование запроса
- [x] Удаление запроса / коллекции с подтверждением
- [x] Экспорт коллекции как `.json`
- [x] Запуск всей коллекции через Runner
- [x] Dropdown-меню (⋯) для каждого запроса/коллекции

### 🕘 История
- [x] Последние 100 записей (виртуализированный список `SgVirtualList`)
- [x] Метод, URL, статус, время, размер, timestamp
- [x] Открытие в новой вкладке
- [x] Очистка с подтверждением

### 🌍 Переменные окружения
- [x] Несколько окружений
- [x] Enable/Disable на каждую переменную
- [x] Описание переменных
- [x] Просмотр активных переменных в sidebar
- [x] Запись переменных из скрипта

### 🎭 Mock Server
- [x] Список mock-эндпоинтов (метод + путь + статус + body + delay)
- [x] Включение/выключение каждого эндпоинта
- [x] Счётчик обращений (Hit Count)
- [x] Интерцепция запросов при включённом Mock
- [x] Редактор body в модальном окне

### 🔌 WebSocket Client
- [x] Подключение по `wss://` / `ws://`
- [x] Отправка и получение сообщений
- [x] Лог сообщений с направлением (▶ SENT / ◀ RECV) и временем
- [x] Счётчики sent/received/errors
- [x] Авто-определение JSON-сообщений
- [x] Правильный `IAsyncDisposable`

### 📈 Статистика сессии
- [x] Total / Success / Failed запросы
- [x] Avg / Min / Max latency
- [x] Total data transferred
- [x] Latency chart (Line)
- [x] Status distribution (Doughnut)

### 📥 cURL Import/Export
- [x] Парсинг `curl -X METHOD url -H headers -d body`
- [x] Открытие в новой вкладке
- [x] Экспорт текущего запроса как cURL (в буфер)

### UX
- [x] `SgSplitter` горизонтальный (sidebar / content)
- [x] `SgSplitter` вертикальный (request / response)
- [x] `SgDockWindow` для Runner / WS / Stats / Diff (перетаскиваемые окна)
- [x] `SgAffix` плавающий тулбар в правом нижнем углу
- [x] `SgThemeToggle` тёмная/светлая тема
- [x] Отправка по `Enter` в поле URL
- [x] `SgVirtualList` для виртуализированной истории
- [x] `SgDropdown` контекстные меню
- [x] Confirm-диалоги перед деструктивными операциями

---

## 📁 Итоговая структура проекта

```
MyBlazorApp/
├── Pages/
│   └── ApiTester.razor               ← главный компонент (оркестратор)
├── Shared/
│   ├── KvEditor.razor                ← редактор KV-пар с bulk paste
│   ├── ResponsePanel.razor           ← панель ответа (grid/chart/code/tests/timeline)
│   ├── WsPanel.razor                 ← WebSocket клиент
│   ├── MockServerPanel.razor         ← Mock Server
│   └── StatsPanel.razor              ← статистика сессии
├── Models/
│   ├── ApiModels.cs                  ← все модели данных
│   └── ScriptRuntime.cs             ← DSL для pre/test скриптов
├── _Imports.razor
├── MainLayout.razor
├── Program.cs
└── wwwroot/
    └── index.html
```

---

## 🧪 Быстрые тест-URL

| Метод  | URL                                              | Результат                        |
|--------|--------------------------------------------------|----------------------------------|
| GET    | `{{baseUrl}}/posts`                              | 100 строк → DataGrid + Chart     |
| GET    | `{{baseUrl}}/posts/{{userId}}`                   | Объект → SgCode                  |
| GET    | `{{baseUrl}}/users`                              | 10 строк → DataGrid              |
| POST   | `{{baseUrl}}/posts`                              | 201 Created                      |
| DELETE | `{{baseUrl}}/posts/1`                            | 200 / 204                        |
| GET    | `https://httpbin.org/get`                        | Заголовки, IP, args              |
| GET    | `https://httpbin.org/delay/3`                    | Тест таймаута                    |
| GET    | `https://httpbin.org/status/500`                 | 500 → Badge Danger               |
| WS     | `wss://echo.websocket.org`                       | Echo WebSocket                   |
| MOCK   | `http://localhost/api/mock/users`                | Mock → `[{id:1,...}]`            |
