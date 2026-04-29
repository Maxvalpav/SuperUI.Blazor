using Bunit;
using Microsoft.AspNetCore.Components;
using SuperUI.Components;

namespace SuperUI.Tests;

public sealed class SgVirtualListTests : BunitContext
{
    [Fact]
    public void UsesVariableHeightsToComputeOffsets()
    {
        var module = JSInterop.SetupModule("/_content/SuperUI/superui-virtuallist.js");
        module.SetupVoid("init", _ => true);
        module.SetupVoid("refreshObservers", _ => true);
        module.SetupVoid("setScrollTop", _ => true);
        module.SetupVoid("dispose", _ => true);

        var items = new[] { "A", "B", "C" };
        var cut = Render<SgVirtualList<string>>(parameters => parameters
            .Add(x => x.Items, items)
            .Add(x => x.Height, "100px")
            .Add(x => x.ItemHeightSelector, item => item switch
            {
                "A" => 20,
                "B" => 40,
                _ => 30
            })
            .Add(x => x.ChildContent, item => builder => builder.AddContent(0, item)));

        Assert.Contains("height: 90px", cut.Markup);
        Assert.Contains("top: 0px", cut.Markup);
        Assert.Contains("top: 20px", cut.Markup);
        Assert.Contains("top: 60px", cut.Markup);
    }

    [Fact]
    public async Task PreservesAnchorItemWhenItemsChange()
    {
        var module = JSInterop.SetupModule("/_content/SuperUI/superui-virtuallist.js");
        module.SetupVoid("init", _ => true);
        module.SetupVoid("refreshObservers", _ => true);
        module.SetupVoid("setScrollTop", _ => true);
        module.SetupVoid("dispose", _ => true);

        var items = new[] { "A", "B", "C", "D" };
        var cut = Render<SgVirtualList<string>>(parameters => parameters
            .Add(x => x.Items, items)
            .Add(x => x.Height, "40px")
            .Add(x => x.Overscan, 0)
            .Add(x => x.ItemHeight, 20)
            .Add(x => x.ItemKeySelector, item => item)
            .Add(x => x.ChildContent, item => builder => builder.AddContent(0, item)));

        await cut.InvokeAsync(() =>
        {
            cut.Instance.OnScroll(40);
            return Task.CompletedTask;
        });

        await cut.InvokeAsync(() => cut.Instance.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(SgVirtualList<string>.Items)] = new[] { "X", "A", "B", "C", "D" },
            [nameof(SgVirtualList<string>.Height)] = "40px",
            [nameof(SgVirtualList<string>.Overscan)] = 0,
            [nameof(SgVirtualList<string>.ItemHeight)] = 20f,
            [nameof(SgVirtualList<string>.ItemKeySelector)] = (Func<string, object?>)(item => item),
            [nameof(SgVirtualList<string>.ChildContent)] = (RenderFragment<string>)(item => builder => builder.AddContent(0, item))
        })));

        Assert.Contains(">C<", cut.Markup);
        Assert.Contains(">D<", cut.Markup);
        Assert.DoesNotContain(">A<", cut.Markup);
    }

    [Fact]
    public async Task EndIntersectionInvokesReachedEndCallbackOncePerEntry()
    {
        var module = JSInterop.SetupModule("/_content/SuperUI/superui-virtuallist.js");
        module.SetupVoid("init", _ => true);
        module.SetupVoid("refreshObservers", _ => true);
        module.SetupVoid("setScrollTop", _ => true);
        module.SetupVoid("dispose", _ => true);

        var callbackCount = 0;
        var cut = Render<SgVirtualList<string>>(parameters => parameters
            .Add(x => x.Items, new[] { "A", "B" })
            .Add(x => x.ReachedEnd, EventCallback.Factory.Create(this, () => callbackCount++))
            .Add(x => x.ChildContent, item => builder => builder.AddContent(0, item)));

        await cut.InvokeAsync(() => cut.Instance.OnEdgeIntersectionChanged("end", true));
        await cut.InvokeAsync(() => cut.Instance.OnEdgeIntersectionChanged("end", true));
        await cut.InvokeAsync(() => cut.Instance.OnEdgeIntersectionChanged("end", false));
        await cut.InvokeAsync(() => cut.Instance.OnEdgeIntersectionChanged("end", true));

        Assert.Equal(2, callbackCount);
    }
}
