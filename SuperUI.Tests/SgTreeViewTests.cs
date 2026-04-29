using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Components;

namespace SuperUI.Tests;

public sealed class SgTreeViewTests : BunitContext
{
    [Fact]
    public void SearchTextRendersMatchingDescendantsEvenWhenParentCollapsed()
    {
        var child = new TreeNode { Key = "child", Text = "Needle" };
        var root = new TreeNode
        {
            Key = "root",
            Text = "Root",
            Expanded = false,
            Children = new List<TreeNode> { child }
        };

        var cut = Render<SgTreeView>(parameters => parameters
            .Add(x => x.Nodes, new[] { root })
            .Add(x => x.SearchText, "Needle"));

        Assert.Contains("Root", cut.Markup);
        Assert.Contains("Needle", cut.Markup);
        Assert.Single(cut.FindAll(".sgc-tree-children"));
    }

    [Fact]
    public void ArrowDownMovesSelectionToNextVisibleNode()
    {
        var first = new TreeNode { Key = "first", Text = "First" };
        var second = new TreeNode { Key = "second", Text = "Second" };

        var cut = Render<SgTreeView>(parameters => parameters
            .Add(x => x.Nodes, new[] { first, second }));

        var rows = cut.FindAll(".sgc-tree-row");
        rows[0].KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

        var selectedRows = cut.FindAll(".sgc-tree-row.sgc-selected");
        Assert.Single(selectedRows);
        Assert.Contains("Second", selectedRows[0].TextContent);
    }

    [Fact]
    public void CtrlClickKeepsExistingSelectionInMultiSelectMode()
    {
        var first = new TreeNode { Key = "first", Text = "First" };
        var second = new TreeNode { Key = "second", Text = "Second" };

        var cut = Render<SgTreeView>(parameters => parameters
            .Add(x => x.Nodes, new[] { first, second })
            .Add(x => x.MultiSelect, true));

        cut.FindAll(".sgc-tree-row")[0].Click();
        cut.FindAll(".sgc-tree-row")[1].Click(new MouseEventArgs { CtrlKey = true });

        Assert.Equal(2, cut.FindAll(".sgc-tree-row.sgc-selected").Count);
    }

    [Fact]
    public void ArrowRightInvokesAsyncExpandCallback()
    {
        var root = new TreeNode { Key = "root", Text = "Root" };
        var callbackCount = 0;

        var cut = Render<SgTreeView>(parameters => parameters
            .Add(x => x.Nodes, new[] { root })
            .Add(x => x.OnExpandAsync, EventCallback.Factory.Create<TreeNode>(this, async node =>
            {
                callbackCount++;
                node.Children.Add(new TreeNode { Key = "child", Text = "Loaded child" });
                await Task.CompletedTask;
            })));

        cut.Find(".sgc-tree-row").KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.Equal(1, callbackCount);
        Assert.Contains("Loaded child", cut.Markup);
    }

    [Fact]
    public void VirtualizeModeRendersVirtualHost()
    {
        var child = new TreeNode { Key = "child", Text = "Child" };
        var root = new TreeNode
        {
            Key = "root",
            Text = "Root",
            Expanded = true,
            Children = new List<TreeNode> { child }
        };

        JSInterop.SetupModule("/_content/SuperUI/superui-virtuallist.js")
            .SetupVoid("init", _ => true);

        var cut = Render<SgTreeView>(parameters => parameters
            .Add(x => x.Nodes, new[] { root })
            .Add(x => x.Virtualize, true));

        Assert.Single(cut.FindAll(".sg-virtual-list-container"));
        Assert.Contains("Root", cut.Markup);
        Assert.Contains("Child", cut.Markup);
    }

    [Fact]
    public void DragDropReordersRootNodes()
    {
        var first = new TreeNode { Key = "first", Text = "First" };
        var second = new TreeNode { Key = "second", Text = "Second" };
        var roots = new List<TreeNode> { first, second };

        var cut = Render<SgTreeView>(parameters => parameters
            .Add(x => x.Nodes, roots)
            .Add(x => x.AllowDragDrop, true));

        cut.FindAll(".sgc-tree-row")[1].DragStart();
        cut.FindAll(".sgc-tree-drop-zone-before")[0].Drop();

        Assert.Equal(second, roots[0]);
        Assert.Equal(first, roots[1]);
    }

    [Fact]
    public void DragDropInsideMovesNodeIntoTargetChildren()
    {
        var first = new TreeNode { Key = "first", Text = "First" };
        var parent = new TreeNode { Key = "parent", Text = "Parent" };
        var roots = new List<TreeNode> { first, parent };

        var cut = Render<SgTreeView>(parameters => parameters
            .Add(x => x.Nodes, roots)
            .Add(x => x.AllowDragDrop, true));

        cut.FindAll(".sgc-tree-row")[0].DragStart();
        cut.FindAll(".sgc-tree-row")[1].Drop();

        Assert.Single(parent.Children);
        Assert.Equal(first, parent.Children[0]);
        Assert.Single(roots);
        Assert.True(parent.Expanded);
    }

    [Fact]
    public void DragDropAfterPlacesNodeAfterTarget()
    {
        var first = new TreeNode { Key = "first", Text = "First" };
        var second = new TreeNode { Key = "second", Text = "Second" };
        var third = new TreeNode { Key = "third", Text = "Third" };
        var roots = new List<TreeNode> { first, second, third };

        var cut = Render<SgTreeView>(parameters => parameters
            .Add(x => x.Nodes, roots)
            .Add(x => x.AllowDragDrop, true));

        cut.FindAll(".sgc-tree-row")[0].DragStart();
        cut.FindAll(".sgc-tree-drop-zone-after")[1].Drop();

        Assert.Equal(second, roots[0]);
        Assert.Equal(first, roots[1]);
        Assert.Equal(third, roots[2]);
    }
}
