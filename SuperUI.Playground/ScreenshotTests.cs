using Microsoft.Playwright;

namespace SuperUI.Playground;

[Collection("Demo")]
public class ScreenshotTests
{
    private readonly DemoFixture _demo;

    private static readonly string ScreenshotDir = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "screenshots"));

    private static readonly string DocScreenshotsDir = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "docs", "screenshots"));

    public ScreenshotTests(DemoFixture demo)
    {
        _demo = demo;
    }

    [Fact]
    public async Task TakeAllScreenshots()
    {
        var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        try
        {
            Directory.CreateDirectory(ScreenshotDir);
            Directory.CreateDirectory(DocScreenshotsDir);

            var screenshots = new (string Route, string Name, string ThemeId, string Mode)[]
            {
                // 15 Home page screenshots with different themes
                ("/", "home-natura-ui-light", "natura-ui", "light"),
                ("/", "home-solaris-light", "solaris", "light"),
                ("/", "home-royal-dark", "royal", "dark"),
                ("/", "home-graphite-light", "graphite", "light"),
                ("/", "home-forest-dark", "forest", "dark"),
                ("/", "home-neon-light", "neon", "light"),
                ("/", "home-glass-dark", "glass", "dark"),
                ("/", "home-chrono-light", "chrono", "light"),
                ("/", "home-calyx-dark", "calyx", "dark"),
                ("/", "home-apex-light", "apex", "light"),
                ("/", "home-zen-dark", "zen", "dark"),
                ("/", "home-neo-light", "neo", "light"),
                ("/", "home-oasis-dark", "oasis", "dark"),
                ("/", "home-flux-light", "flux", "light"),
                ("/", "home-prism-dark", "prism", "dark"),

                // 35 Other pages with different themes
                ("/datagrid-demo", "datagrid-natura-dark", "natura-ui", "dark"),
                ("/charts-demo", "charts-solaris-dark", "solaris", "dark"),
                ("/kanban", "kanban-royal-light", "royal", "light"),
                ("/gantt", "gantt-forest-light", "forest", "light"),
                ("/scheduler-demo", "scheduler-neon-dark", "neon", "dark"),
                ("/orgchart-demo", "orgchart-glass-light", "glass", "light"),
                ("/dashboard-demo", "dashboard-calyx-light", "calyx", "light"),
                ("/button-demo", "buttons-apex-dark", "apex", "dark"),
                ("/accordion-demo", "accordion-chrono-dark", "chrono", "dark"),
                ("/alert-demo", "alert-neo-dark", "neo", "dark"),
                ("/tabs-demo", "tabs-oasis-light", "oasis", "light"),
                ("/splitter-demo", "splitter-flux-dark", "flux", "dark"),
                ("/timeline-demo", "timeline-prism-light", "prism", "light"),
                ("/stepper-demo", "stepper-cosmos-dark", "cosmos", "dark"),
                ("/modal-demo", "modal-fractalis-light", "fractalis", "light"),
                ("/drawer-demo", "drawer-wave-dark", "wave", "dark"),
                ("/menu-demo", "menu-aurea-light", "aurea", "light"),
                ("/select-demo", "select-sylvan-dark", "sylvan", "dark"),
                ("/data-form-demo", "dataform-medici-light", "medici", "light"),
                ("/calendar", "calendar-aether-dark", "aether", "dark"),
                ("/chat-demo", "chat-clarity-light", "clarity", "light"),
                ("/qrcode-demo", "qrcode-element-dark", "element", "dark"),
                ("/spreadsheet-demo", "spreadsheet-radius-light", "radius", "light"),
                ("/dock-manager-demo", "dockmanager-muse-dark", "muse", "dark"),
                ("/property-grid", "propertygrid-forge-light", "forge", "light"),
                ("/tree-view", "treeview-gordian-dark", "gordian", "dark"),
                ("/transfer-demo", "transfer-inclus-light", "inclus", "light"),
                ("/pagination-demo", "pagination-reader-dark", "reader", "dark"),
                ("/command-palette-demo", "cmdpalette-signature-light", "signature", "light"),
                ("/weather-demo", "weather-cantus-dark", "cantus", "dark"),
                ("/terminal-demo", "terminal-natura-dark", "natura-ui", "dark"),
                ("/bpmn-demo", "bpmn-cosmos-light", "cosmos", "light"),
                ("/breadcrumb-demo", "breadcrumb-oasis-light", "oasis", "light"),
                ("/dock-window-demo", "dockwindow-prism-dark", "prism", "dark"),
                ("/data-display-demo", "datadisplay-solaris-light", "solaris", "light"),
            };

            foreach (var (route, name, themeId, mode) in screenshots)
            {
                var context = await browser.NewContextAsync();

                await context.AddInitScriptAsync(script: $@"
                    localStorage.setItem('superui-theme-id', '{themeId}');
                    localStorage.setItem('superui-dark-mode', '{mode}');
                ");

                var page = await context.NewPageAsync();
                await page.SetViewportSizeAsync(1920, 1080);
                await page.GotoAsync(_demo.BaseUrl + route);
                await page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);

                // Wait for Blazor to initialize and apply the theme
                await Task.Delay(3000);

                var screenshotPath = Path.Combine(ScreenshotDir, $"{name}.png");
                await page.ScreenshotAsync(new()
                {
                    Path = screenshotPath,
                    FullPage = true,
                });

                // Copy to docs/screenshots for README use
                var docPath = Path.Combine(DocScreenshotsDir, $"{name}.png");
                File.Copy(screenshotPath, docPath, overwrite: true);

                Console.WriteLine($"  ✓ {name}.png — {route} ({themeId} {mode})");

                await context.CloseAsync();
            }
        }
        finally
        {
            await browser.CloseAsync();
            playwright.Dispose();
        }
    }
}
