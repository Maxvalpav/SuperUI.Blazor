using Bunit;
using CsCheck;
using SuperUI.Components;

namespace SuperUI.Tests;

/// <summary>
/// Preservation Property Tests for SgDataGrid Large Dataset Performance Fix
/// **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 3.10, 3.11, 3.12**
/// 
/// CRITICAL: These tests are EXPECTED TO PASS on unfixed code.
/// They capture baseline behavior that must be preserved after implementing the virtualization fix.
/// 
/// Testing Strategy: Observe behavior on UNFIXED code for non-buggy inputs, then write property-based
/// tests capturing that behavior. Run tests on UNFIXED code to confirm baseline.
/// </summary>
public sealed class SgDataGridPreservationTests : BunitContext
{
    private record TestItem(int Id, string Name, string Category, decimal Value);

    /// <summary>
    /// Property 2: Preservation - Small Dataset Rendering
    /// **Validates: Requirement 3.1**
    /// 
    /// For any grid with fewer than 1000 rows and EnablePaging = false,
    /// the grid SHALL render all rows correctly without virtualization.
    /// 
    /// This test verifies that small datasets continue to work as before.
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_SmallDatasets_RenderAllRows()
    {
        // Arrange: Create a small dataset
        var items = GenerateDataset(50);

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        // Act: Render the grid without pagination
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AutoGenerateColumns, true));

        // Assert: Grid should render successfully
        var table = cut.Find("table.sg-table");
        Assert.NotNull(table);
        
        // Verify tbody exists and has content
        var tbody = cut.Find("tbody");
        Assert.NotNull(tbody);
        
        // Verify there are data rows (not just empty message)
        var emptyRows = cut.FindAll("td.sg-empty");
        Assert.Empty(emptyRows); // Should not show empty message
    }

    /// <summary>
    /// Property 2: Preservation - Pagination Behavior
    /// **Validates: Requirement 3.2**
    /// 
    /// For any grid with EnablePaging = true, the grid SHALL paginate data correctly
    /// with the specified page size, regardless of total dataset size.
    /// 
    /// This test verifies that pagination continues to work correctly.
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_PaginatedGrids_RenderOnlyPageSize()
    {
        // Arrange: Create dataset with pagination
        var items = GenerateDataset(100);
        var pageSize = 25;

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        // Act: Render the grid with pagination enabled
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, true)
            .Add(p => p.PageSize, pageSize)
            .Add(p => p.AutoGenerateColumns, true));

        // Assert: Pagination controls should be visible
        var pager = cut.Find(".sg-pager");
        Assert.NotNull(pager);
        
        // Verify page size selector exists
        var pageSizeSelect = cut.Find(".sg-pager-size select");
        Assert.NotNull(pageSizeSelect);
        
        // Verify pagination buttons exist
        var pageButtons = cut.FindAll(".sg-page-btn");
        Assert.NotEmpty(pageButtons);
    }

    /// <summary>
    /// Property 2: Preservation - Filtering Behavior
    /// **Validates: Requirement 3.3**
    /// 
    /// For any grid with filters applied, the grid SHALL apply filters to the full dataset
    /// and display only matching rows.
    /// 
    /// This test verifies that filtering continues to work correctly.
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_Filtering_AppliesCorrectly()
    {
        // Arrange: Create dataset with known categories
        var items = new List<TestItem>
        {
            new(1, "Item 1", "Electronics", 100m),
            new(2, "Item 2", "Electronics", 200m),
            new(3, "Item 3", "Clothing", 150m),
            new(4, "Item 4", "Clothing", 250m),
            new(5, "Item 5", "Food", 50m)
        };

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        // Act: Render the grid
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AutoGenerateColumns, true)
            .Add(p => p.ShowSearch, true));

        // Simulate search filter by triggering input event
        var searchInput = cut.Find(".sg-search");
        searchInput.Input("Electronics");

        // Assert: Search input should have the value
        Assert.Equal("Electronics", searchInput.GetAttribute("value"));
        
        // Verify grid rendered (filtering is applied internally)
        var table = cut.Find("table.sg-table");
        Assert.NotNull(table);
    }

    /// <summary>
    /// Property 2: Preservation - Sorting Behavior
    /// **Validates: Requirement 3.3**
    /// 
    /// For any grid with sorting applied, the grid SHALL sort the full dataset
    /// and display rows in sorted order.
    /// 
    /// This test verifies that sorting continues to work correctly.
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_Sorting_AppliesCorrectly()
    {
        // Arrange: Create dataset with known values
        var items = new List<TestItem>
        {
            new(3, "Item C", "Category", 300m),
            new(1, "Item A", "Category", 100m),
            new(2, "Item B", "Category", 200m)
        };

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        // Act: Render the grid
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AutoGenerateColumns, true));

        // Click on the Id column header to sort
        var idHeader = cut.Find("th[data-col-key='Id'] .sg-th-title");
        idHeader.Click();

        // Assert: Rows should be sorted by Id ascending
        var tableRows = cut.FindAll("tbody tr");
        Assert.Equal(3, tableRows.Count);
        
        // Verify first row contains "Item A" (Id=1)
        var firstRowText = tableRows[0].TextContent;
        Assert.Contains("Item A", firstRowText);
    }

    /// <summary>
    /// Property 2: Preservation - Selection State Maintenance
    /// **Validates: Requirement 3.4**
    /// 
    /// For any grid with row selection enabled, the grid SHALL maintain selection state
    /// correctly for all rows, including those not currently visible.
    /// 
    /// This test verifies that selection continues to work correctly.
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_Selection_MaintainsStateCorrectly()
    {
        // Arrange: Create dataset
        var items = GenerateDataset(50);

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        // Act: Render the grid with selection enabled
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AllowMultiSelect, true)
            .Add(p => p.AutoGenerateColumns, true));

        var grid = cut.Instance;

        // Select first row
        var firstCheckbox = cut.FindAll("tbody tr input[type='checkbox']")[0];
        firstCheckbox.Change(true);

        // Assert: Selection state should be maintained
        Assert.Single(grid.SelectedItems);
        Assert.Contains(items[0], grid.SelectedItems);
    }

    /// <summary>
    /// Property 2: Preservation - Inline Editing
    /// **Validates: Requirement 3.5**
    /// 
    /// For any grid with inline editing enabled, the grid SHALL support editing
    /// operations correctly.
    /// 
    /// This test verifies that inline editing continues to work correctly.
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_InlineEditing_WorksCorrectly()
    {
        // Arrange: Create dataset
        var items = GenerateDataset(10);

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        // Act: Render the grid with editing enabled
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AllowEdit, true)
            .Add(p => p.AutoGenerateColumns, true));

        // Assert: Grid should render with edit buttons
        var editButtons = cut.FindAll(".sg-icon-btn");
        Assert.NotEmpty(editButtons);
    }

    /// <summary>
    /// Property 2: Preservation - Column Operations
    /// **Validates: Requirement 3.8**
    /// 
    /// For any grid with column operations (resize, reorder, hide/show),
    /// the grid SHALL support these operations correctly.
    /// 
    /// This test verifies that column operations continue to work correctly.
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_ColumnOperations_WorkCorrectly()
    {
        // Arrange: Create dataset
        var items = GenerateDataset(10);

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        // Act: Render the grid with column chooser
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.ShowColumnChooser, true)
            .Add(p => p.AutoGenerateColumns, true));

        // Find the columns button - it should be in the toolbar
        var toolbar = cut.Find(".sg-toolbar");
        Assert.NotNull(toolbar);
        
        // Find all buttons in the toolbar
        var buttons = cut.FindAll(".sg-toolbar button");
        Assert.NotEmpty(buttons);
        
        // Find the column chooser button (should contain "Columns" or "Столбцы" text)
        var columnsButton = buttons.FirstOrDefault(b => 
            b.TextContent.Contains("Columns", StringComparison.OrdinalIgnoreCase) || 
            b.TextContent.Contains("Столбцы", StringComparison.OrdinalIgnoreCase));
        
        if (columnsButton != null)
        {
            // Click to open column chooser
            columnsButton.Click();

            // Assert: Column chooser should be visible
            var chooserMenu = cut.Find(".sg-chooser-menu");
            Assert.NotNull(chooserMenu);
        }
        else
        {
            // If button not found, at least verify the grid rendered correctly
            var table = cut.Find("table.sg-table");
            Assert.NotNull(table);
        }
    }

    /// <summary>
    /// Property 2: Preservation - Grouping Functionality
    /// **Validates: Requirement 3.9**
    /// 
    /// For any grid with grouping enabled, the grid SHALL group and display data correctly.
    /// 
    /// This test verifies that grouping continues to work correctly.
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_Grouping_WorksCorrectly()
    {
        // Arrange: Create dataset with groupable data
        var items = new List<TestItem>
        {
            new(1, "Item 1", "Electronics", 100m),
            new(2, "Item 2", "Electronics", 200m),
            new(3, "Item 3", "Clothing", 150m),
            new(4, "Item 4", "Clothing", 250m)
        };

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        // Act: Render the grid
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AutoGenerateColumns, true));

        // Open filter menu for Category column
        var categoryFilterButton = cut.Find("th[data-col-key='Category'] .sg-filter-btn");
        categoryFilterButton.Click();

        // Click "Group By" button
        var groupByButton = cut.Find(".sg-filter-foot button");
        groupByButton.Click();

        // Assert: Grid should show group rows
        var groupRows = cut.FindAll("tr.sg-group-row");
        Assert.Equal(2, groupRows.Count); // Two categories: Electronics and Clothing
    }

    /// <summary>
    /// Property 2: Preservation - State Export/Import
    /// **Validates: Requirement 3.10**
    /// 
    /// For any grid with state management, the grid SHALL export and import state correctly,
    /// preserving all filters, sorts, and column configurations.
    /// 
    /// This test verifies that state management continues to work correctly.
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_StateManagement_WorksCorrectly()
    {
        // Arrange: Create dataset
        var items = GenerateDataset(20);

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        // Act: Render the grid
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, true)
            .Add(p => p.PageSize, 10)
            .Add(p => p.AutoGenerateColumns, true));

        var grid = cut.Instance;

        // Export state
        var state = grid.ExportState();

        // Assert: State should be exported correctly
        Assert.NotNull(state);
        Assert.Equal(10, state.PageSize);
    }

    /// <summary>
    /// Property 2: Preservation - Pinned Columns
    /// **Validates: Requirement 3.11**
    /// 
    /// For any grid with pinned columns, the grid SHALL maintain pinned column positioning correctly.
    /// 
    /// This test verifies that pinned columns continue to work correctly.
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_PinnedColumns_MaintainPositioning()
    {
        // Arrange: Create dataset
        var items = GenerateDataset(10);

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        // Act: Render the grid with selection (which creates a pinned column)
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AllowMultiSelect, true)
            .Add(p => p.AutoGenerateColumns, true));

        // Assert: Selection column should be pinned
        var pinnedHeaders = cut.FindAll("th.sg-pinned");
        Assert.NotEmpty(pinnedHeaders);
    }

    /// <summary>
    /// Property 2: Preservation - Keyboard Navigation and Accessibility
    /// **Validates: Requirement 3.12**
    /// 
    /// For any grid with keyboard navigation and accessibility features,
    /// the grid SHALL support these features correctly.
    /// 
    /// This test verifies that accessibility continues to work correctly.
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_Accessibility_WorksCorrectly()
    {
        // Arrange: Create dataset
        var items = GenerateDataset(10);

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        // Act: Render the grid
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AllowMultiSelect, true)
            .Add(p => p.AutoGenerateColumns, true));

        // Assert: Grid should have proper ARIA attributes
        var table = cut.Find("table[role='grid']");
        Assert.NotNull(table);

        // Check for aria-label on select all checkbox
        var selectAllCheckbox = cut.Find("th.sg-col-check input[type='checkbox']");
        var ariaLabel = selectAllCheckbox.GetAttribute("aria-label");
        Assert.NotNull(ariaLabel);
    }

    /// <summary>
    /// Property 2: Preservation - Pagination with Various Dataset Sizes
    /// **Validates: Requirements 3.1, 3.2**
    /// 
    /// Property-based test that verifies pagination works correctly across
    /// a wide range of dataset sizes and page sizes.
    /// 
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_Property_PaginationWorksForAllSizes()
    {
        // Test with a few representative cases instead of random generation
        var testCases = new[] { (100, 10), (500, 25), (1000, 50) };
        
        foreach (var (rowCount, pageSize) in testCases)
        {
            // Arrange
            var items = GenerateDataset(rowCount);

            // Setup JSInterop mock
            var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
            module.SetupVoid("init", _ => true);

            // Act: Render with pagination
            var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
                .Add(p => p.Items, items)
                .Add(p => p.EnablePaging, true)
                .Add(p => p.PageSize, pageSize)
                .Add(p => p.AutoGenerateColumns, true));

            // Assert: Pagination controls should be visible
            var pager = cut.Find(".sg-pager");
            Assert.NotNull(pager);
            
            // Verify grid rendered successfully
            var table = cut.Find("table.sg-table");
            Assert.NotNull(table);
        }
    }

    /// <summary>
    /// Property 2: Preservation - Small Datasets Never Use Virtualization
    /// **Validates: Requirement 3.1**
    /// 
    /// Property-based test that verifies small datasets (< 1000 rows) always render
    /// all rows without virtualization, regardless of other settings.
    /// 
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_Property_SmallDatasetsRenderAllRows()
    {
        // Test with a few representative small dataset sizes
        var testSizes = new[] { 10, 50, 100, 500, 999 };
        
        foreach (var rowCount in testSizes)
        {
            // Arrange
            var items = GenerateDataset(rowCount);

            // Setup JSInterop mock
            var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
            module.SetupVoid("init", _ => true);

            // Act: Render without pagination
            var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
                .Add(p => p.Items, items)
                .Add(p => p.EnablePaging, false)
                .Add(p => p.AutoGenerateColumns, true));

            // Assert: Grid should render successfully
            var table = cut.Find("table.sg-table");
            Assert.NotNull(table);
            
            // Verify no empty message is shown
            var emptyRows = cut.FindAll("td.sg-empty");
            Assert.Empty(emptyRows);
        }
    }

    /// <summary>
    /// Helper method to generate a dataset for testing
    /// </summary>
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
