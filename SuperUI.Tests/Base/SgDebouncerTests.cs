using Microsoft.Extensions.Time.Testing;
using SuperUI.Base.Utilities;
using Xunit;

namespace SuperUI.Tests.Base;

public class SgDebouncerTests
{
    [Fact]
    public async Task Leading_mode_runs_immediately()
    {
        var time = new FakeTimeProvider();
        var debouncer = new SgDebouncer(leading: true, time);
        var count = 0;
        await debouncer.RunAsync(() => count++, TimeSpan.FromMilliseconds(100));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Trailing_mode_delays_execution()
    {
        var time = new FakeTimeProvider();
        var debouncer = new SgDebouncer(leading: false, time);
        var count = 0;
        var task = debouncer.RunAsync(() => count++, TimeSpan.FromMilliseconds(100));
        Assert.Equal(0, count);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await task;
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Cancel_prevents_pending_call()
    {
        var time = new FakeTimeProvider();
        var debouncer = new SgDebouncer(leading: false, time);
        var count = 0;
        debouncer.RunAsync(() => count++, TimeSpan.FromMilliseconds(100));
        debouncer.Cancel();
        time.Advance(TimeSpan.FromMilliseconds(150));
        await Task.Delay(10);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Multiple_calls_within_window_coalesce()
    {
        var time = new FakeTimeProvider();
        var debouncer = new SgDebouncer(leading: false, time);
        var count = 0;
        debouncer.RunAsync(() => count++, TimeSpan.FromMilliseconds(100));
        time.Advance(TimeSpan.FromMilliseconds(50));
        debouncer.RunAsync(() => count++, TimeSpan.FromMilliseconds(100));
        time.Advance(TimeSpan.FromMilliseconds(50));
        debouncer.RunAsync(() => count++, TimeSpan.FromMilliseconds(100));
        time.Advance(TimeSpan.FromMilliseconds(150));
        await Task.Delay(20);
        // Only the last call should fire.
        Assert.Equal(1, count);
    }
}
