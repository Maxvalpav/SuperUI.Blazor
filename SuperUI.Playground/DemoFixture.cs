using System.Diagnostics;

namespace SuperUI.Playground;

public class DemoFixture : IAsyncLifetime
{
    private Process? _process;

    public string BaseUrl => "http://localhost:5080";

    public async Task InitializeAsync()
    {
        var demoDir = FindDemoProjectDir();

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{demoDir}\" --launch-profile http",
            WorkingDirectory = demoDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        _process = new Process { StartInfo = psi };
        _process.Start();

        _ = Task.Run(async () =>
        {
            await Task.Yield();
            try { await _process.StandardOutput.ReadToEndAsync(); } catch { }
        });
        _ = Task.Run(async () =>
        {
            await Task.Yield();
            try { await _process.StandardError.ReadToEndAsync(); } catch { }
        });

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        for (var i = 0; i < 60; i++)
        {
            try
            {
                var response = await client.GetAsync(BaseUrl);
                if (response.IsSuccessStatusCode) return;
            }
            catch { }
            await Task.Delay(1000);
        }
        throw new TimeoutException("Demo server did not start within 60 seconds.");
    }

    public async Task DisposeAsync()
    {
        if (_process is not null && !_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync();
            _process.Dispose();
        }
    }

    private static string FindDemoProjectDir()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(dir, "SuperUI.slnx")))
                return Path.Combine(dir, "SuperUI.Demo");
            dir = Path.GetDirectoryName(dir)!;
        }
        throw new FileNotFoundException(
            "Could not locate solution root. Searched up from: " + AppContext.BaseDirectory);
    }
}
