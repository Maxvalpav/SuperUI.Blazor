using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SuperUI.Services.AI;

public class SgLangGraphService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private IJSObjectReference? _module;
    private DotNetObjectReference<SgLangGraphService>? _selfRef;
    private string? _instanceId;

    public event Action<SgLangGraphStep>? OnStep;
    public event Action<string>? OnError;
    public event Action<SgLangGraphSchema>? OnInitialized;
    public event Action<SgLangGraphInterrupt>? OnInterrupt;
    public event Func<string, string, Task<string>>? OnToolCall;
    public event Func<string, object, Task<SgLangGraphNodeResult>>? OnNodeExecute;

    public SgLangGraphService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task InitializeAsync(SgLangGraphConfig config)
    {
        if (_module == null)
        {
            _module = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/sg-langgraph.js");
        }

        _selfRef = DotNetObjectReference.Create(this);
        _instanceId = Guid.NewGuid().ToString();
        
        await _module.InvokeVoidAsync("initGraph", _instanceId, _selfRef, config);
    }

    public async Task SendMessageAsync(string message)
    {
        if (_module == null || _instanceId == null) return;
        await _module.InvokeVoidAsync("sendMessage", _instanceId, message);
    }

    public async Task RespondToInterruptAsync(object data)
    {
        if (_module == null || _instanceId == null) return;
        await _module.InvokeVoidAsync("respondToInterrupt", _instanceId, data);
    }

    [JSInvokable]
    public void OnStepInternal(SgLangGraphStep step)
    {
        OnStep?.Invoke(step);
    }

    [JSInvokable]
    public void OnErrorInternal(string error)
    {
        OnError?.Invoke(error);
    }

    [JSInvokable]
    public void OnInitializedInternal(SgLangGraphSchema schema)
    {
        OnInitialized?.Invoke(schema);
    }

    [JSInvokable]
    public void OnInterruptInternal(SgLangGraphInterrupt interrupt)
    {
        OnInterrupt?.Invoke(interrupt);
    }

    [JSInvokable]
    public async Task<SgLangGraphNodeResult> OnNodeExecuteInternal(string nodeId, object state)
    {
        if (OnNodeExecute != null)
        {
            return await OnNodeExecute.Invoke(nodeId, state);
        }
        return new SgLangGraphNodeResult { State = state };
    }

    [JSInvokable]
    public async Task<string> OnToolCallInternal(string toolName, string argsJson)
    {
        if (OnToolCall != null)
        {
            return await OnToolCall.Invoke(toolName, argsJson);
        }
        return "{}";
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null && _instanceId != null)
        {
            try
            {
                await _module.InvokeVoidAsync("dispose", _instanceId);
                await _module.DisposeAsync();
            }
            catch { }
        }
        _selfRef?.Dispose();
    }
}

public class SgLangGraphConfig
{
    public string? ThreadId { get; set; }
    public object? GraphDefinition { get; set; }
    public List<SgLangGraphNode>? Nodes { get; set; }
    public List<SgLangGraphEdge>? Edges { get; set; }
}

public class SgLangGraphStep
{
    public string Node { get; set; } = string.Empty;
    public object? State { get; set; }
    public string? Content { get; set; }
}

public class SgLangGraphSchema
{
    public List<SgLangGraphNode> Nodes { get; set; } = new();
    public List<SgLangGraphEdge> Edges { get; set; } = new();
}

public class SgLangGraphNode
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Type { get; set; }
}

public class SgLangGraphEdge
{
    public string SourceId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string? Label { get; set; }
}

public class SgLangGraphInterrupt
{
    public string Node { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
}

public class SgLangGraphNodeResult
{
    public object? State { get; set; }
    public string? Content { get; set; }
    public SgLangGraphInterrupt? Interrupt { get; set; }
}
