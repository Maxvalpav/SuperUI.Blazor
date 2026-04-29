using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SuperUI.Components;
using SuperUI.Localization;
using SuperUI.Services;

namespace SuperUI.Tests;

public sealed class SgConfirmHostTests : BunitContext
{
    [Fact]
    public async Task ReturnsTrueWhenConfirmed()
    {
        var module = JSInterop.SetupModule("/_content/SuperUI/superui-modal.js");
        module.SetupVoid("attach", _ => true);
        module.SetupVoid("detach", _ => true);

        var service = new SgConfirmService(new SuperUILocalizer());
        Services.AddSingleton(service);
        Services.AddSingleton<ISuperUILocalizer>(new SuperUILocalizer());
        Services.AddSingleton(new SgZIndexService());

        var cut = Render<SgConfirmHost>();
        var pending = service.ConfirmAsync("Delete item?", "Confirm");

        cut.WaitForAssertion(() => Assert.Contains("Delete item?", cut.Markup));
        cut.FindAll("button").Last().Click();

        Assert.True(await pending);
    }
}
