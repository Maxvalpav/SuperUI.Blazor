using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SuperUI.Core;

/// <summary>
/// Base for components backed by a co-located JS module. Handles import on first render,
/// caches the <see cref="IJSObjectReference"/> module and a <see cref="DotNetObjectReference{T}"/>
/// to <c>this</c>, and disposes both safely. Derived components override <see cref="ModuleName"/>
/// to choose the JS file (defaults to the C# type name) and implement
/// <see cref="OnJsModuleReadyAsync"/> to run their first-time initialization call.
/// </summary>
/// <typeparam name="TSelf">CRTP-style self type so <see cref="DotNetObjectReference{T}"/> is strongly typed.</typeparam>
public abstract class SgJsInteropComponent<TSelf> : SgComponentBase where TSelf : SgJsInteropComponent<TSelf>
{
    private bool _imported;

    /// <summary>Cached JS module. <c>null</c> until <see cref="OnAfterRenderAsync"/> first runs.</summary>
    protected IJSObjectReference? Module { get; private set; }

    /// <summary>Cached .NET object reference to this component. Pass to JS for callbacks.</summary>
    protected DotNetObjectReference<TSelf>? DotNetRef { get; private set; }

    /// <summary>
    /// Name of the JS file (without extension), assumed to live at
    /// <c>./_content/SuperUI/{ModuleFolder}/{ModuleName}.razor.js</c>. Defaults to the type name.
    /// </summary>
    protected virtual string ModuleName => GetType().Name;

    /// <summary>Subfolder under <c>_content/SuperUI</c>. Defaults to <c>"Components"</c>.</summary>
    protected virtual string ModuleFolder => "Components";

    /// <summary>
    /// True once the JS module is loaded and <see cref="OnJsModuleReadyAsync"/> has finished.
    /// Derived components may guard interop calls on this flag.
    /// </summary>
    protected bool IsModuleReady => _imported && Module is not null;

    /// <summary>
    /// Called once after the module has been imported and <see cref="DotNetRef"/> is created.
    /// Use this to invoke a JS <c>init(dotNetRef, ...)</c> function. Default: no-op.
    /// </summary>
    protected virtual ValueTask OnJsModuleReadyAsync() => ValueTask.CompletedTask;

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender && !_imported && !IsDisposed)
        {
            DotNetRef = CreateDotNetRef((TSelf)this);
            Module = await ImportModuleAsync(ModuleName, ModuleFolder);
            _imported = true;
            if (Module is not null)
            {
                try { await OnJsModuleReadyAsync(); }
                catch (JSDisconnectedException) { }
                catch (OperationCanceledException) { }
            }
        }
    }

    /// <inheritdoc/>
    protected override async ValueTask DisposeAsyncCore()
    {
        await base.DisposeAsyncCore();
        try
        {
            if (Module is not null)
            {
                await SafeInvokeVoidAsync(Module, "dispose");
                await Module.SafeDisposeAsync();
                Module = null;
            }
        }
        finally
        {
            DotNetRef?.Dispose();
            DotNetRef = null;
        }
    }
}
