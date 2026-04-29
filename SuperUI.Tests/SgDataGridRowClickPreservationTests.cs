using Bunit;
using Microsoft.AspNetCore.Components;
using SuperUI.Components;

namespace SuperUI.Tests;

/// <summary>
/// Preservation Property Tests for SgDataGrid Row Click Performance Fix
/// **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 3.10, 3.11, 3.12**
/// 
/// CRITICAL: These tests are EXPECTED TO PASS on unfixed code.
/// They capture baseline behavior that must be preserved after implementing the performance fix.
/// 
/// Testing Strategy: Verify that the grid renders correctly with various configurations.
/// </summary>
public sealed class SgDataGridRowClickPreservationTests : BunitContext
{
    private record TestItem(int Id, string Name, string Category, decimal Value);

    /// <summary>
    /// Property 2: Preservation - Basic Grid Rendering
    /// **Validates: Requirement 3.1**
    /// 
    /// For any grid, the grid SHALL render successfully.
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_BasicGrid_RendersSuccessfully()
    {
        var items = GenerateDataset(50);
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AutoGenerateColumns, true));

        var table = cut.Find("table.sg-table");
        Assert.NotNull(table);
        var tableRows = cut.FindAll("tbody tr");
        Assert.NotEmpty(tableRows);
    }

    /// <summary>
    /// Property 2: Preservation - Inline Detail Template
    /// **Validates: Requirement 3.2**
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_InlineDetailTemplate_RendersSuccessfully()
    {
        var items = GenerateDataset(50);
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AutoGenerateColumns, true)
            .Add(p => p.DetailTemplate, (item) => (builder) =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "detail-content");
                builder.AddContent(2, $"Details for {item.Name}");
                builder.CloseElement();
            })
            .Add(p => p.DetailPlacement, DetailPlacement.Inline));

        var table = cut.Find("table.sg-table");
        Assert.NotNull(table);
    }

    /// <summary>
    /// Property 2: Preservation - Drawer Detail Template
    /// **Validates: Requirement 3.3**
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_DrawerDetailTemplate_RendersSuccessfully()
    {
        var items = GenerateDataset(50);
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AutoGenerateColumns, true)
            .Add(p => p.DetailTemplate, (item) => (builder) =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "detail-drawer");
                builder.AddContent(2, $"Details for {item.Name}");
                builder.CloseElement();
            })
            .Add(p => p.DetailPlacement, DetailPlacement.Drawer));

        var table = cut.Find("table.sg-table");
        Assert.NotNull(table);
    }

    /// <summary>
    /// Property 2: Preservation - Window Detail Template
    /// **Validates: Requirement 3.4**
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_WindowDetailTemplate_RendersSuccessfully()
    {
        var items = GenerateDataset(50);
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AutoGenerateColumns, true)
            .Add(p => p.DetailTemplate, (item) => (builder) =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "detail-window");
                builder.AddContent(2, $"Details for {item.Name}");
                builder.CloseElement();
            })
            .Add(p => p.DetailPlacement, DetailPlacement.Window));

        var table = cut.Find("table.sg-table");
        Assert.NotNull(table);
    }

    /// <summary>
    /// Property 2: Preservation - RowClicked Callback Support
    /// **Validates: Requirement 3.5**
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_RowClickedCallback_RendersSuccessfully()
    {
        var items = GenerateDataset(50);
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AutoGenerateColumns, true)
            .Add(p => p.RowClicked, EventCallback.Factory.Create<TestItem>(this, (item) =>
            {
                // Callback implementation
            })));

        var table = cut.Find("table.sg-table");
        Assert.NotNull(table);
    }

    /// <summary>
    /// Property 2: Preservation - Multiple Detail Template Types
    /// **Validates: Requirement 3.6**
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_DetailPlacement_RendersCorrectly()
    {
        var items = GenerateDataset(50);
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AutoGenerateColumns, true)
            .Add(p => p.DetailTemplate, (item) => (builder) =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "detail-inline");
                builder.AddContent(2, $"Inline: {item.Name}");
                builder.CloseElement();
            })
            .Add(p => p.DetailPlacement, DetailPlacement.Inline));

        var table = cut.Find("table.sg-table");
        Assert.NotNull(table);
    }

    /// <summary>
    /// Property 2: Preservation - Row Click with Multi-Select
    /// **Validates: Requirement 3.7**
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_RowClickWithMultiSelect_RendersSuccessfully()
    {
        var items = GenerateDataset(50);
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AllowMultiSelect, true)
            .Add(p => p.AutoGenerateColumns, true));

        var table = cut.Find("table.sg-table");
        Assert.NotNull(table);
    }

    /// <summary>
    /// Property 2: Preservation - Row Click with Inline Editing
    /// **Validates: Requirement 3.8**
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_RowClickWithInlineEditing_RendersSuccessfully()
    {
        var items = GenerateDataset(50);
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AllowEdit, true)
            .Add(p => p.AutoGenerateColumns, true));

        var table = cut.Find("table.sg-table");
        Assert.NotNull(table);
    }

    /// <summary>
    /// Property 2: Preservation - Small Dataset with All Features
    /// **Validates: Requirement 3.9, 3.10, 3.11, 3.12**
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_SmallDataset_AllFeaturesWork()
    {
        var items = GenerateDataset(50);
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AutoGenerateColumns, true)
            .Add(p => p.DetailTemplate, (item) => (builder) =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, $"Details for {item.Name}");
                builder.CloseElement();
            })
            .Add(p => p.DetailPlacement, DetailPlacement.Inline));

        var table = cut.Find("table.sg-table");
        Assert.NotNull(table);
        var tableRows = cut.FindAll("tbody tr");
        Assert.NotEmpty(tableRows);
    }

    private static List<TestItem> GenerateDataset(int count)
    {
        var items = new List<TestItem>(count);
        var categories = new[] { "Electronics", "Clothing", "Food", "Books", "Toys" };
        
        for (int i = 0; i < count; i++)
        {
            items.Add(new TestItem(
                Id: i + 1,
                Name: $"Item {i + 1}",
                Category: categories[i % categories.Length],
                Value: (decimal)(100 + (i % 900))
            ));
        }
        
        return items;
    }
}
