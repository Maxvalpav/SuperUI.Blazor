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

        // Match markup snapshot (bUnit Snapshot Testing)
        cut.MarkupMatches(@"<button class=""sgc-btn sgc-btn-default sgc-md"" type=""button"" ...><span>Submit</span></button>");
        
        // Note: For actual visual regression, integration with Playwright is required
        // to take actual screenshots and compare pixel-by-pixel.
    }
}
