using Bunit;
using SuperUI.Components;
using System.Diagnostics;

namespace SuperUI.Tests;

/// <summary>
/// Bug Condition Exploration Tests for SgDataGrid Large Dataset Performance
/// **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 1.5, 1.6**
/// 
/// CRITICAL: These tests are EXPECTED TO FAIL on unfixed code.
/// Failure confirms the bug exists. DO NOT attempt to fix the test or code when it fails.
/// </summary>
public sealed class SgDataGridPerformanceTests : BunitContext
{
    private record TestItem(int Id, string Name, string Category, decimal Value);

    /// <summary>
    /// Property 1: Bug Condition - Large Dataset Performance Degradation
    /// 
    /// CRITICAL: This test MUST FAIL on unfixed code - failure confirms the bug exists.
    /// DO NOT attempt to fix the test or the code when it fails.
    /// 
    /// This test encodes the expected behavior - it will validate the fix when it passes after implementation.
    /// 
    /// GOAL: Surface counterexamples that demonstrate the bug exists.
    /// 
    /// Test that rendering 10,000+ rows with EnablePaging = false results in poor performance:
    /// - Initial render time exceeds 100ms (expected: >1000ms on unfixed code)
    /// - DOM contains 10,000+ <tr> elements (expected: all rows rendered)
    /// - Memory usage is excessive (expected: >100MB on unfixed code)
    /// 
    /// EXPECTED OUTCOME: Test FAILS (this is correct - it proves the bug exists)
    /// </summary>
    [Fact]
    public void BugCondition_LargeDatasetWithoutPaging_CausesPoorPerformance()
    {
        // Arrange: Create a large dataset (10,000 rows)
        const int rowCount = 10000;
        var items = GenerateLargeDataset(rowCount);

        // Setup JSInterop mock for the grid's JavaScript module
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true); // Match any arguments

        // Measure initial memory before rendering
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var memoryBefore = GC.GetTotalMemory(forceFullCollection: false);

        // Act: Render the grid with pagination disabled
        var stopwatch = Stopwatch.StartNew();
        
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AutoGenerateColumns, true));

        stopwatch.Stop();
        var renderTime = stopwatch.ElapsedMilliseconds;

        // Measure memory after rendering
        var memoryAfter = GC.GetTotalMemory(forceFullCollection: false);
        var memoryUsedMB = (memoryAfter - memoryBefore) / (1024.0 * 1024.0);

        // Count DOM nodes (table rows)
        var tableRows = cut.FindAll("tbody tr");
        var domRowCount = tableRows.Count;

        // Assert: Expected behavior (these will FAIL on unfixed code, proving the bug exists)
        
        // Expected: Initial render time should be under 100ms
        // On unfixed code: Will be >1000ms (FAIL - proves bug exists)
        Assert.True(renderTime < 100, 
            $"COUNTEREXAMPLE FOUND: Initial render time was {renderTime}ms (expected <100ms). " +
            $"This demonstrates the performance bug exists.");

        // Expected: DOM should contain fewer than 100 rows (virtualized)
        // On unfixed code: Will contain 10,000 rows (FAIL - proves bug exists)
        Assert.True(domRowCount < 100, 
            $"COUNTEREXAMPLE FOUND: DOM contains {domRowCount} rows (expected <100 with virtualization). " +
            $"This demonstrates that all rows are being rendered without virtualization.");

        // Expected: Memory usage should be under 50MB
        // On unfixed code: Will be >100MB (FAIL - proves bug exists)
        Assert.True(memoryUsedMB < 50, 
            $"COUNTEREXAMPLE FOUND: Memory usage was {memoryUsedMB:F2}MB (expected <50MB). " +
            $"This demonstrates excessive memory consumption due to rendering all rows.");

        // Document the counterexamples found
        Console.WriteLine("=== BUG CONDITION EXPLORATION RESULTS ===");
        Console.WriteLine($"Dataset size: {rowCount} rows");
        Console.WriteLine($"Initial render time: {renderTime}ms (expected <100ms)");
        Console.WriteLine($"DOM row count: {domRowCount} (expected <100)");
        Console.WriteLine($"Memory usage: {memoryUsedMB:F2}MB (expected <50MB)");
        Console.WriteLine("==========================================");
    }

    /// <summary>
    /// Additional bug condition test: Verify that the bug manifests with different dataset sizes
    /// </summary>
    [Theory]
    [InlineData(1000)]
    [InlineData(5000)]
    [InlineData(10000)]
    public void BugCondition_VariousLargeDatasets_AllRenderAllRows(int rowCount)
    {
        // Arrange
        var items = GenerateLargeDataset(rowCount);

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true); // Match any arguments

        // Act
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AutoGenerateColumns, true));

        // Assert: On unfixed code, all rows should be rendered
        var tableRows = cut.FindAll("tbody tr");
        var domRowCount = tableRows.Count;

        // Expected behavior: Should render only visible rows (<100)
        // On unfixed code: Will render all rows (FAIL - proves bug exists)
        Assert.True(domRowCount < 100,
            $"COUNTEREXAMPLE FOUND for {rowCount} rows: DOM contains {domRowCount} rows (expected <100). " +
            $"Bug manifests at this dataset size.");

        Console.WriteLine($"Dataset: {rowCount} rows -> DOM rows: {domRowCount}");
    }

    /// <summary>
    /// Bug condition test: Verify that small datasets are NOT affected
    /// This should PASS even on unfixed code (no bug for small datasets)
    /// </summary>
    [Fact]
    public void SmallDataset_WithoutPaging_RendersNormally()
    {
        // Arrange: Small dataset (50 rows)
        const int rowCount = 50;
        var items = GenerateLargeDataset(rowCount);

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true); // Match any arguments

        // Act
        var stopwatch = Stopwatch.StartNew();
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AutoGenerateColumns, true));
        stopwatch.Stop();

        // Assert: Small datasets should render quickly even on unfixed code
        var tableRows = cut.FindAll("tbody tr");
        Assert.Equal(rowCount, tableRows.Count); // All rows should be rendered for small datasets
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Small dataset render took {stopwatch.ElapsedMilliseconds}ms");

        Console.WriteLine($"Small dataset ({rowCount} rows) rendered in {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Bug condition test: Verify that pagination is NOT affected by the bug
    /// This should PASS even on unfixed code (pagination already chunks data)
    /// </summary>
    [Fact]
    public void LargeDataset_WithPagination_RendersOnlyPageSize()
    {
        // Arrange: Large dataset with pagination enabled
        const int rowCount = 10000;
        const int pageSize = 25;
        var items = GenerateLargeDataset(rowCount);

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true); // Match any arguments

        // Act
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, true)
            .Add(p => p.PageSize, pageSize)
            .Add(p => p.AutoGenerateColumns, true));

        // Assert: With pagination, only page size rows should be rendered
        var tableRows = cut.FindAll("tbody tr");
        Assert.Equal(pageSize, tableRows.Count);

        Console.WriteLine($"Paginated grid ({rowCount} rows, page size {pageSize}) rendered {tableRows.Count} rows");
    }

    /// <summary>
    /// Helper method to generate a large dataset for testing
    /// </summary>
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
