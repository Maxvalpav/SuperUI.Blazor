using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SuperUI.Base.Utilities;
using SuperUI.Components;
using SuperUI.Localization;
using SuperUI.Services;

namespace SuperUI.Tests;

/// <summary>
/// Base class for bUnit tests that provides common service registrations.
/// </summary>
public abstract class BunitContext : TestContext
{
    protected BunitContext()
    {
        Services.AddSingleton<ISuperUILocalizer, SuperUILocalizer>();
        Services.AddSingleton<SgZIndexService>();
        Services.AddSingleton<SgJsModuleCache>();
        Services.AddSingleton<SgToastService>();
        Services.AddSingleton<SgConfirmService>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
