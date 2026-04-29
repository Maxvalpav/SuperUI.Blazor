using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SuperUI.Localization;
using SuperUI.Services;
using SuperUI.Components;

namespace SuperUI.Tests;

/// <summary>
/// Base class for bUnit tests that provides common service registrations.
/// </summary>
public abstract class BunitContext : TestContext
{
    protected BunitContext()
    {
        // Register required services for SuperUI components
        Services.AddSingleton<ISuperUILocalizer, SuperUILocalizer>();
        Services.AddSingleton<SgZIndexService>();
        Services.AddSingleton<SgToastService>();
        Services.AddSingleton<SgConfirmService>();
        
        // Mock JSInterop for common modules if needed
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
