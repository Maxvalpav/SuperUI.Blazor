using Microsoft.Extensions.Time.Testing;
using SuperUI.Base.Utilities;
using Xunit;

namespace SuperUI.Tests.Base;

public class SgThrottlerTests
{
    [Fact]
    public async Task LeadingOnly_first_call_runs_immediately()
    {
        var time = new FakeTimeProvider();
        var throttler = new SgThrottler(SgThrottler.Mode.LeadingOnly, time);
        var count = 0;
        await throttler.InvokeAsync(_ => { count++; return Task.CompletedTask; }, TimeSpan.FromMilliseconds(100));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task LeadingOnly_second_call_within_window_ignored()
    {
        var time = new FakeTimeProvider();
        var throttler = new SgThrottler(SgThrottler.Mode.LeadingOnly, time);
        var count = 0;
        await throttler.InvokeAsync(_ => { count++; return Task.CompletedTask; }, TimeSpan.FromMilliseconds(100));
        await throttler.InvokeAsync(_ => { count++; return Task.CompletedTask; }, TimeSpan.FromMilliseconds(100));
        await throttler.InvokeAsync(_ => { count++; return Task.CompletedTask; }, TimeSpan.FromMilliseconds(100));
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task LeadingOnly_call_after_window_runs_again()
    {
        var time = new FakeTimeProvider();
        var throttler = new SgThrottler(SgThrottler.Mode.LeadingOnly, time);
        var count = 0;
        await throttler.InvokeAsync(_ => { count++; return Task.CompletedTask; }, TimeSpan.FromMilliseconds(100));
        time.Advance(TimeSpan.FromMilliseconds(150));
        await Task.Delay(10);
        await throttler.InvokeAsync(_ => { count++; return Task.CompletedTask; }, TimeSpan.FromMilliseconds(100));
        Assert.Equal(2, count);
    }
}
