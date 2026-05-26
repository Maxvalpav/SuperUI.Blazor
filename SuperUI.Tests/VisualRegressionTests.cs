using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SuperUI.Components;
using Xunit;
using System.Threading.Tasks;

namespace SuperUI.Tests.Visual;

public class VisualRegressionTests : TestContext
{
    [Fact]
    public async Task Button_Snapshot_Test()
    {
        // Setup services
        Services.AddSuperUI();

        // Render component
        var cut = RenderComponent<SgButton>(p => p.Add(x => x.Text, "Submit"));

        // Verify button renders the correct text
        Assert.Contains("Submit", cut.Markup);
        Assert.Contains("sgc-btn", cut.Markup);
    }
}
