using SuperUI.Base.Utilities;
using Xunit;

namespace SuperUI.Tests.Base;

public class SgKeyboardNavigationTests
{
    [Fact]
    public void ArrowDown_returns_Move_to_next()
    {
        var action = SgKeyboardNavigation.Resolve("ArrowDown", SgKeyboardNavigation.Orientation.Vertical, 10, 0);
        Assert.Equal(NavKind.Move, action.Kind);
        Assert.Equal(1, action.Index);
    }

    [Fact]
    public void ArrowUp_returns_Move_to_previous()
    {
        var action = SgKeyboardNavigation.Resolve("ArrowUp", SgKeyboardNavigation.Orientation.Vertical, 10, 5);
        Assert.Equal(NavKind.Move, action.Kind);
        Assert.Equal(4, action.Index);
    }

    [Fact]
    public void ArrowRight_returns_Move_for_horizontal()
    {
        var action = SgKeyboardNavigation.Resolve("ArrowRight", SgKeyboardNavigation.Orientation.Horizontal, 10, 3);
        Assert.Equal(NavKind.Move, action.Kind);
        Assert.Equal(4, action.Index);
    }

    [Fact]
    public void ArrowLeft_returns_Move_for_horizontal()
    {
        var action = SgKeyboardNavigation.Resolve("ArrowLeft", SgKeyboardNavigation.Orientation.Horizontal, 10, 3);
        Assert.Equal(NavKind.Move, action.Kind);
        Assert.Equal(2, action.Index);
    }

    [Fact]
    public void Home_returns_Move_to_zero()
    {
        var action = SgKeyboardNavigation.Resolve("Home", SgKeyboardNavigation.Orientation.Vertical, 10, 5);
        Assert.Equal(NavKind.Move, action.Kind);
        Assert.Equal(0, action.Index);
    }

    [Fact]
    public void End_returns_Move_to_last()
    {
        var action = SgKeyboardNavigation.Resolve("End", SgKeyboardNavigation.Orientation.Vertical, 10, 0);
        Assert.Equal(NavKind.Move, action.Kind);
        Assert.Equal(9, action.Index);
    }

    [Fact]
    public void Enter_returns_Activate()
    {
        var action = SgKeyboardNavigation.Resolve("Enter", SgKeyboardNavigation.Orientation.Vertical, 10, 5);
        Assert.Equal(NavKind.Activate, action.Kind);
        Assert.Equal(5, action.Index);
    }

    [Fact]
    public void Escape_returns_Cancel()
    {
        var action = SgKeyboardNavigation.Resolve("Escape", SgKeyboardNavigation.Orientation.Vertical, 10, 5);
        Assert.Equal(NavKind.Cancel, action.Kind);
        Assert.Equal(5, action.Index);
    }

    [Fact]
    public void Unknown_key_returns_None()
    {
        var action = SgKeyboardNavigation.Resolve("a", SgKeyboardNavigation.Orientation.Vertical, 10, 0);
        Assert.Equal(NavKind.None, action.Kind);
    }

    [Fact]
    public void Clamp_clamps_to_valid_range()
    {
        Assert.Equal(0, SgKeyboardNavigation.Clamp(-5, 10));
        Assert.Equal(9, SgKeyboardNavigation.Clamp(15, 10));
        Assert.Equal(5, SgKeyboardNavigation.Clamp(5, 10));
    }

    [Fact]
    public void Clamp_zero_count_returns_negative()
    {
        Assert.Equal(-1, SgKeyboardNavigation.Clamp(0, 0));
    }

    [Fact]
    public void ArrowDown_in_horizontal_is_None()
    {
        var action = SgKeyboardNavigation.Resolve("ArrowDown", SgKeyboardNavigation.Orientation.Horizontal, 10, 0);
        Assert.Equal(NavKind.None, action.Kind);
    }

    [Fact]
    public void Both_orientation_accepts_all_arrows()
    {
        Assert.Equal(NavKind.Move, SgKeyboardNavigation.Resolve("ArrowDown", SgKeyboardNavigation.Orientation.Both, 10, 0).Kind);
        Assert.Equal(NavKind.Move, SgKeyboardNavigation.Resolve("ArrowRight", SgKeyboardNavigation.Orientation.Both, 10, 0).Kind);
    }
}
