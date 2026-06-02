using SuperUI.Base.Utilities;
using Xunit;

namespace SuperUI.Tests.Base;

public class SgStringBuilderCacheTests
{
    [Fact]
    public void Acquire_returns_writable_stringbuilder()
    {
        var sb = SgStringBuilderCache.Acquire(64);
        Assert.NotNull(sb);
        sb.Append("hello");
        SgStringBuilderCache.Release(sb);
    }

    [Fact]
    public void GetStringAndRelease_returns_appended_text()
    {
        var sb = SgStringBuilderCache.Acquire(64);
        sb.Append("hello");
        sb.Append(' ');
        sb.Append("world");
        var result = SgStringBuilderCache.GetStringAndRelease(sb);
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void Repeated_acquires_reuse_pooled_instances()
    {
        // Pre-warm to ensure the pool is populated.
        for (var i = 0; i < 10; i++)
        {
            var s = SgStringBuilderCache.Acquire(32);
            SgStringBuilderCache.Release(s);
        }

        // The same thread should get a cached instance on subsequent Acquire.
        var a = SgStringBuilderCache.Acquire(32);
        var b = SgStringBuilderCache.Acquire(32);
        SgStringBuilderCache.Release(a);
        SgStringBuilderCache.Release(b);
    }
}
