namespace SuperUI.Playground;

[Collection("Demo")]
public class ScreenshotTests
{
    private readonly DemoFixture _demo;

    private static readonly string ScreenshotDir = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "screenshots"));

    public ScreenshotTests(DemoFixture demo)
    {
        _demo = demo;
    }

    [Fact]
    public async Task HomePage()
    {
        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        try
        {
            var page = await browser.NewPageAsync();
            await page.SetViewportSizeAsync(1920, 1080);
            await page.GotoAsync(_demo.BaseUrl);
            await page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
            Directory.CreateDirectory(ScreenshotDir);
            await page.ScreenshotAsync(new()
            {
                Path = Path.Combine(ScreenshotDir, "homepage.png"),
                FullPage = true,
            });
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }
}
