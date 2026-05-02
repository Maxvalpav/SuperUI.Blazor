using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SuperUI.Localization;
using SuperUI.Services;
using SuperUI.Components;

namespace SuperUI.Tests;

/// <summary>
/// Base class for bUnit tests that provides common service registrations.
/// </summary>
public abstract class BunitContext : Bunit.BunitContext
{
    protected BunitContext()
    {
        Services.AddSingleton<ISuperUILocalizer, SuperUILocalizer>();
        Services.AddSingleton<SgZIndexService>();
        Services.AddSingleton<SgToastService>();
        Services.AddSingleton<SgConfirmService>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
