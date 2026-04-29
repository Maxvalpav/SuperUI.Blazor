using Bunit;
using Microsoft.AspNetCore.Components;
using SuperUI.Components;
using System.Diagnostics;

namespace SuperUI.Tests;

/// <summary>
/// Bug Condition Exploration Tests for SgDataGrid Row Click No Re-render Bugfix
/// **Property 1: Bug Condition** - Row Click Response Time Without Full Re-render
/// **Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5, 2.6**
/// 
/// These tests measure click response time on UNFIXED code to surface the performance bug.
/// Expected to FAIL on unfixed code (proving the bug exists).
/// Expected to PASS after the fix is applied.
/// 
/// The bug condition is: Row clicks on grids with 2000+ rows trigger full grid re-renders
/// causing 200-500ms lag due to Blazor's StateHasChanged() being called.
/// 
/// The expected behavior is: Row clicks should respond immediately (under 50ms) with visual
/// feedback without triggering a full grid re-render.
/// </summary>
public sealed class SgDataGridRowClickNoRerenderPerformanceTests : BunitContext
{
    private record TestItem(int Id, string Name, string Category, decimal Value);

    /// <summary>
    /// Property 1: Bug Condition - Row Click Response Time with Large Dataset
    /// 
    /// For any row click event on a grid with 2000+ rows and detail templates enabled,
    /// the OnRowClickAsync function SHALL update the active row state immediately (under 50ms)
    /// with visual feedback, without triggering a full grid re-render.
    /// 
    /// Bug Condition: isBugCondition(input) where:
    ///   - input.eventType == 'click'
    ///   - input.targetElement == 'row'
    ///   - gridRowCount >= 2000
    ///   - _activeRow is updated in Blazor state
    ///   - StateHasChanged() is triggered
    ///   - template re-evaluates IsActiveRow() for all rows
    /// 
    /// Expected Behavior: expectedBehavior(result) where:
    ///   - JavaScript adds sg-active class to clicked row immediately (under 50ms)
    ///   - No full grid re-render occurs
    ///   - Visual feedback appears instantly
    /// 
    /// This test MUST FAIL on unfixed code (proving the bug exists).
    /// This test MUST PASS after the fix is applied.
    /// </summary>
    [Fact]
    public void BugCondition_RowClickWithLargeDatasetAndInlineDetail_RespondsUnder50ms()
    {
        const int rowCount = 2000;
        var items = GenerateLargeDataset(rowCount);

        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AllowMultiSelect, true)
            .Add(p => p.AutoGenerateColumns, true)
            .Add(p => p.DetailTemplate, (item) => (builder) =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, $"Details for {item.Name}");
                builder.CloseElement();
            })
            .Add(p => p.DetailPlacement, DetailPlacement.Inline));

        var tableRows = cut.FindAll("tbody tr");
        Assert.NotEmpty(tableRows);

        // Measure click response time for first row
        var firstRow = tableRows[0];
        var stopwatch = Stopwatch.StartNew();
        
        firstRow.Click();
        
        stopwatch.Stop();
        var responseTime = stopwatch.ElapsedMilliseconds;

        // Bug Condition: Response time should be under 50ms for immediate visual feedback
        // On unfixed code, this will be 200-500ms, causing test to FAIL
        // On fixed code, this will be under 50ms, causing test to PASS
        Assert.True(responseTime < 50, 
            $"Row click response time was {responseTime}ms, expected under 50ms. " +
            $"This indicates the bug exists (synchronous state updates blocking UI). " +
            $"Counterexample: Click on row in grid with {rowCount} rows and inline detail template. " +
            $"Bug Condition: isBugCondition(input) where input.eventType='click' AND gridRowCount={rowCount} AND DetailTemplate is not null");
    }

    /// <summary>
    /// Property 1: Bug Condition - Row Click with Drawer Detail
    /// 
    /// For any row click event on a grid with 2000+ rows and drawer detail template,
    /// the OnRowClickAsync function SHALL respond immediately (under 50ms).
    /// </summary>
    [Fact]
    public void BugCondition_RowClickWithDrawerDetail_RespondsUnder50ms()
    {
        const int rowCount = 2000;
        var items = GenerateLargeDataset(rowCount);

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
            .Add(p => p.DetailPlacement, DetailPlacement.Drawer));

        var tableRows = cut.FindAll("tbody tr");
        Assert.NotEmpty(tableRows);

        var firstRow = tableRows[0];
        var stopwatch = Stopwatch.StartNew();
        
        firstRow.Click();
        
        stopwatch.Stop();
        var responseTime = stopwatch.ElapsedMilliseconds;

        // Bug Condition: Response time should be under 50ms
        Assert.True(responseTime < 50, 
            $"Row click response time was {responseTime}ms, expected under 50ms. " +
            $"Counterexample: Click on row in grid with {rowCount} rows and drawer detail template. " +
            $"Bug Condition: isBugCondition(input) where input.eventType='click' AND gridRowCount={rowCount} AND DetailPlacement=Drawer");
    }

    /// <summary>
    /// Property 1: Bug Condition - Row Click with Window Detail
    /// 
    /// For any row click event on a grid with 2000+ rows and window detail template,
    /// the OnRowClickAsync function SHALL respond immediately (under 50ms).
    /// </summary>
    [Fact]
    public void BugCondition_RowClickWithWindowDetail_RespondsUnder50ms()
    {
        const int rowCount = 2000;
        var items = GenerateLargeDataset(rowCount);

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
            .Add(p => p.DetailPlacement, DetailPlacement.Window));

        var tableRows = cut.FindAll("tbody tr");
        Assert.NotEmpty(tableRows);

        var firstRow = tableRows[0];
        var stopwatch = Stopwatch.StartNew();
        
        firstRow.Click();
        
        stopwatch.Stop();
        var responseTime = stopwatch.ElapsedMilliseconds;

        // Bug Condition: Response time should be under 50ms
        Assert.True(responseTime < 50, 
            $"Row click response time was {responseTime}ms, expected under 50ms. " +
            $"Counterexample: Click on row in grid with {rowCount} rows and window detail template. " +
            $"Bug Condition: isBugCondition(input) where input.eventType='click' AND gridRowCount={rowCount} AND DetailPlacement=Window");
    }

    /// <summary>
    /// Property 1: Bug Condition - Rapid Row Clicks
    /// 
    /// For rapid row click events on a grid with 2000+ rows,
    /// each click SHALL respond immediately (under 50ms).
    /// </summary>
    [Fact]
    public void BugCondition_RapidRowClicks_EachRespondsUnder50ms()
    {
        const int rowCount = 2000;
        var items = GenerateLargeDataset(rowCount);
        var responseTimes = new List<long>();

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

        var tableRows = cut.FindAll("tbody tr");
        Assert.NotEmpty(tableRows);

        // Simulate rapid clicks on multiple rows
        for (int i = 0; i < 5 && i < tableRows.Count; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            tableRows[i].Click();
            stopwatch.Stop();
            responseTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // Each click should respond under 50ms
        foreach (var responseTime in responseTimes)
        {
            Assert.True(responseTime < 50, 
                $"Rapid row click response time was {responseTime}ms, expected under 50ms. " +
                $"Counterexample: Rapid clicks on rows in grid with {rowCount} rows. " +
                $"Bug Condition: isBugCondition(input) where input.eventType='click' AND rapid clicks cause cumulative blocking");
        }
    }

    /// <summary>
    /// Property 1: Bug Condition - Row Click with Callback
    /// 
    /// For row click events on a grid with 2000+ rows and RowClicked callback,
    /// the OnRowClickAsync function SHALL respond immediately (under 50ms).
    /// </summary>
    [Fact]
    public void BugCondition_RowClickWithCallback_RespondsUnder50ms()
    {
        const int rowCount = 2000;
        var items = GenerateLargeDataset(rowCount);
        var callbackInvoked = false;

        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AutoGenerateColumns, true)
            .Add(p => p.RowClicked, EventCallback.Factory.Create<TestItem>(this, (item) =>
            {
                callbackInvoked = true;
            })));

        var tableRows = cut.FindAll("tbody tr");
        Assert.NotEmpty(tableRows);

        var firstRow = tableRows[0];
        var stopwatch = Stopwatch.StartNew();
        
        firstRow.Click();
        
        stopwatch.Stop();
        var responseTime = stopwatch.ElapsedMilliseconds;

        // Bug Condition: Response time should be under 50ms even with callback
        Assert.True(responseTime < 50, 
            $"Row click response time was {responseTime}ms, expected under 50ms. " +
            $"Counterexample: Click on row in grid with {rowCount} rows and RowClicked callback. " +
            $"Bug Condition: isBugCondition(input) where input.eventType='click' AND RowClicked callback is defined");
    }

    /// <summary>
    /// Property 1: Bug Condition - Row Click with Multi-Select
    /// 
    /// For row click events on a grid with 2000+ rows and multi-select enabled,
    /// the OnRowClickAsync function SHALL respond immediately (under 50ms).
    /// </summary>
    [Fact]
    public void BugCondition_RowClickWithMultiSelect_RespondsUnder50ms()
    {
        const int rowCount = 2000;
        var items = GenerateLargeDataset(rowCount);

        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AllowMultiSelect, true)
            .Add(p => p.AutoGenerateColumns, true)
            .Add(p => p.DetailTemplate, (item) => (builder) =>
            {
                builder.OpenElement(0, "div");
                builder.AddContent(1, $"Details for {item.Name}");
                builder.CloseElement();
            })
            .Add(p => p.DetailPlacement, DetailPlacement.Inline));

        var tableRows = cut.FindAll("tbody tr");
        Assert.NotEmpty(tableRows);

        var firstRow = tableRows[0];
        var stopwatch = Stopwatch.StartNew();
        
        firstRow.Click();
        
        stopwatch.Stop();
        var responseTime = stopwatch.ElapsedMilliseconds;

        // Bug Condition: Response time should be under 50ms with multi-select
        Assert.True(responseTime < 50, 
            $"Row click response time was {responseTime}ms, expected under 50ms. " +
            $"Counterexample: Click on row in grid with {rowCount} rows and multi-select enabled. " +
            $"Bug Condition: isBugCondition(input) where input.eventType='click' AND AllowMultiSelect=true");
    }

    private static List<TestItem> GenerateLargeDataset(int count)
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
