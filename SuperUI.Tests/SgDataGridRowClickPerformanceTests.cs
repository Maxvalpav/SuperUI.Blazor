using Bunit;
using Microsoft.AspNetCore.Components;
using SuperUI.Components;
using System.Diagnostics;

namespace SuperUI.Tests;

/// <summary>
/// Bug Condition Exploration Tests for SgDataGrid Row Click Performance
/// 
/// These tests measure click response time on UNFIXED code to surface the performance bug.
/// Expected to FAIL on unfixed code (proving the bug exists).
/// Expected to PASS after the fix is applied.
/// </summary>
public sealed class SgDataGridRowClickPerformanceTests : BunitContext
{
    private record TestItem(int Id, string Name, string Category, decimal Value);

    /// <summary>
    /// Property 1: Bug Condition - Row Click Response Time
    /// 
    /// For any row click event on a grid with 1000+ rows and detail templates enabled,
    /// the OnRowClickAsync function SHALL update the active row state immediately (under 50ms)
    /// with visual feedback, deferring expensive detail template state changes and callback
    /// invocations to a separate render cycle.
    /// 
    /// This test MUST FAIL on unfixed code (proving the bug exists).
    /// This test MUST PASS after the fix is applied.
    /// </summary>
    [Fact]
    public void BugCondition_RowClickWithLargeDatasetAndDetailTemplate_RespondsUnder50ms()
    {
        const int rowCount = 1000;
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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        firstRow.Click();
        
        stopwatch.Stop();
        var responseTime = stopwatch.ElapsedMilliseconds;

        // Bug Condition: Response time should be under 50ms for immediate visual feedback
        // On unfixed code, this will be 200-500ms, causing test to FAIL
        // On fixed code, this will be under 50ms, causing test to PASS
        Assert.True(responseTime < 50, 
            $"Row click response time was {responseTime}ms, expected under 50ms. " +
            $"This indicates the bug exists (synchronous state updates blocking UI). " +
            $"Counterexample: Click on row in grid with {rowCount} rows and inline detail template.");
    }

    /// <summary>
    /// Property 1: Bug Condition - Row Click with Drawer Detail
    /// 
    /// For any row click event on a grid with 1000+ rows and drawer detail template,
    /// the OnRowClickAsync function SHALL respond immediately (under 50ms).
    /// </summary>
    [Fact]
    public void BugCondition_RowClickWithDrawerDetail_RespondsUnder50ms()
    {
        const int rowCount = 1000;
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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        firstRow.Click();
        
        stopwatch.Stop();
        var responseTime = stopwatch.ElapsedMilliseconds;

        // Bug Condition: Response time should be under 50ms
        Assert.True(responseTime < 50, 
            $"Row click response time was {responseTime}ms, expected under 50ms. " +
            $"Counterexample: Click on row in grid with {rowCount} rows and drawer detail template.");
    }

    /// <summary>
    /// Property 1: Bug Condition - Row Click with Window Detail
    /// 
    /// For any row click event on a grid with 1000+ rows and window detail template,
    /// the OnRowClickAsync function SHALL respond immediately (under 50ms).
    /// </summary>
    [Fact]
    public void BugCondition_RowClickWithWindowDetail_RespondsUnder50ms()
    {
        const int rowCount = 1000;
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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        firstRow.Click();
        
        stopwatch.Stop();
        var responseTime = stopwatch.ElapsedMilliseconds;

        // Bug Condition: Response time should be under 50ms
        Assert.True(responseTime < 50, 
            $"Row click response time was {responseTime}ms, expected under 50ms. " +
            $"Counterexample: Click on row in grid with {rowCount} rows and window detail template.");
    }

    /// <summary>
    /// Property 1: Bug Condition - Rapid Row Clicks
    /// 
    /// For rapid row click events on a grid with 1000+ rows,
    /// each click SHALL respond immediately (under 50ms).
    /// </summary>
    [Fact]
    public void BugCondition_RapidRowClicks_EachRespondsUnder50ms()
    {
        const int rowCount = 1000;
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
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            tableRows[i].Click();
            stopwatch.Stop();
            responseTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // Each click should respond under 50ms
        foreach (var responseTime in responseTimes)
        {
            Assert.True(responseTime < 50, 
                $"Rapid row click response time was {responseTime}ms, expected under 50ms. " +
                $"Counterexample: Rapid clicks on rows in grid with {rowCount} rows.");
        }
    }

    /// <summary>
    /// Property 1: Bug Condition - Very Large Dataset
    /// 
    /// For row click events on a grid with 2000+ rows,
    /// the OnRowClickAsync function SHALL respond immediately (under 50ms).
    /// </summary>
    [Fact]
    public void BugCondition_VeryLargeDataset_RespondsUnder50ms()
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
            .Add(p => p.DetailPlacement, DetailPlacement.Inline));

        var tableRows = cut.FindAll("tbody tr");
        Assert.NotEmpty(tableRows);

        var firstRow = tableRows[0];
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        firstRow.Click();
        
        stopwatch.Stop();
        var responseTime = stopwatch.ElapsedMilliseconds;

        // Bug Condition: Response time should be under 50ms even with 2000 rows
        Assert.True(responseTime < 50, 
            $"Row click response time was {responseTime}ms, expected under 50ms. " +
            $"Counterexample: Click on row in grid with {rowCount} rows.");
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
