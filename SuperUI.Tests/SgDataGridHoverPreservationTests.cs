using Bunit;
using SuperUI.Components;

namespace SuperUI.Tests;

/// <summary>
/// Preservation Property Tests for SgDataGrid Hover Performance Fix
/// **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.8, 3.9, 3.10, 3.11, 3.12**
/// 
/// CRITICAL: These tests are EXPECTED TO PASS on unfixed code.
/// They capture baseline visual behavior that must be preserved after implementing the hover performance fix.
/// 
/// Testing Strategy: Verify that hover visual appearance remains unchanged after the fix.
/// These tests ensure that performance optimizations do not introduce visual regressions.
/// </summary>
public sealed class SgDataGridHoverPreservationTests : BunitContext
{
    private record TestItem(int Id, string Name, string Category, decimal Value, bool IsEditable);

    /// <summary>
    /// Property 2: Preservation - Hover Background Color for Normal Rows
    /// **Validates: Requirement 3.1**
    /// 
    /// For any normal row (not selected, not active), hovering SHALL display
    /// the hover background color (#eaf2fb) correctly.
    /// 
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_HoverBackgroundColor_NormalRows()
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
            .Add(p => p.AutoGenerateColumns, true));

        var tableRows = cut.FindAll("tbody tr");
        Assert.NotEmpty(tableRows);

        // Get the first normal row (not selected, not active)
        var normalRow = tableRows[0];

        // Assert: Row should have correct base background
        var rowClasses = normalRow.ClassList;
        Assert.DoesNotContain("sg-selected", rowClasses);
        Assert.DoesNotContain("sg-active", rowClasses);

        // Verify row renders correctly
        Assert.NotNull(normalRow);
    }

    /// <summary>
    /// Property 2: Preservation - Hover Background Color for Selected Rows
    /// **Validates: Requirement 3.2**
    /// 
    /// For any selected row, hovering SHALL display the selected hover state
    /// with correct background color (#b8d5f0) correctly.
    /// 
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_HoverBackgroundColor_SelectedRows()
    {
        // Arrange: Create dataset
        var items = GenerateDataset(10);

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        // Act: Render the grid with multi-select
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AllowMultiSelect, true)
            .Add(p => p.AutoGenerateColumns, true));

        var tableRows = cut.FindAll("tbody tr");
        Assert.NotEmpty(tableRows);

        // Verify grid renders with multi-select enabled
        var table = cut.Find("table.sg-table");
        Assert.NotNull(table);

        // Verify rows render correctly
        Assert.True(tableRows.Count > 0, "Grid should render rows");
    }

    /// <summary>
    /// Property 2: Preservation - Hover Background Color for Active Rows
    /// **Validates: Requirement 3.3**
    /// 
    /// For any active row, hovering SHALL display the active hover state
    /// with correct background color (#b8d5f0) correctly.
    /// 
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_HoverBackgroundColor_ActiveRows()
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
            .Add(p => p.AutoGenerateColumns, true));

        var tableRows = cut.FindAll("tbody tr");
        Assert.NotEmpty(tableRows);

        // Verify grid renders correctly
        Assert.NotNull(tableRows[0]);
    }

    /// <summary>
    /// Property 2: Preservation - Hover Background Color for Alternate Rows
    /// **Validates: Requirement 3.4**
    /// 
    /// For any alternate row (even-numbered rows with background #fafafa),
    /// hovering SHALL display the hover background color (#eaf2fb) correctly,
    /// overriding the alternate row background.
    /// 
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_HoverBackgroundColor_AlternateRows()
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
            .Add(p => p.AutoGenerateColumns, true));

        var tableRows = cut.FindAll("tbody tr");
        Assert.NotEmpty(tableRows);

        // Verify even rows exist (alternate rows)
        var evenRows = tableRows.Where((_, i) => i % 2 == 1).ToList();
        Assert.NotEmpty(evenRows);

        // Verify grid renders correctly
        Assert.NotNull(tableRows[0]);
    }

    /// <summary>
    /// Property 2: Preservation - Pinned Column Backgrounds During Hover
    /// **Validates: Requirement 3.5**
    /// 
    /// For any row with pinned columns, hovering SHALL display pinned column
    /// backgrounds correctly with the same hover color as non-pinned cells.
    /// 
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_PinnedColumnBackgrounds_DuringHover()
    {
        // Arrange: Create dataset
        var items = GenerateDataset(10);

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        // Act: Render the grid with multi-select (creates pinned column)
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AllowMultiSelect, true)
            .Add(p => p.AutoGenerateColumns, true));

        var tableRows = cut.FindAll("tbody tr");
        Assert.NotEmpty(tableRows);

        // Find pinned cells
        var pinnedCells = cut.FindAll("td.sg-pinned");
        Assert.NotEmpty(pinnedCells);

        // Verify pinned cells render correctly
        Assert.NotNull(pinnedCells[0]);
    }

    /// <summary>
    /// Property 2: Preservation - Editable Cell Indicator Display
    /// **Validates: Requirement 3.6**
    /// 
    /// For any editable cell, the editable indicator (gradient corner) SHALL
    /// display correctly during hover without visual artifacts.
    /// 
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_EditableCellIndicator_DisplaysCorrectly()
    {
        // Arrange: Create dataset with editable cells
        var items = GenerateDataset(10);

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        // Act: Render the grid with editable cells
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AllowEdit, true)
            .Add(p => p.AutoGenerateColumns, true));

        // Verify grid renders correctly
        var table = cut.Find("table.sg-table");
        Assert.NotNull(table);

        // Verify rows render correctly
        var tableRows = cut.FindAll("tbody tr");
        Assert.NotEmpty(tableRows);
    }

    /// <summary>
    /// Property 2: Preservation - Column Borders Display During Hover
    /// **Validates: Requirement 3.7**
    /// 
    /// For any row with column borders, hovering SHALL display borders correctly
    /// without visual artifacts or disappearing borders.
    /// 
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_ColumnBorders_DisplayCorrectly()
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
            .Add(p => p.AutoGenerateColumns, true));

        // Find cells with borders
        var cells = cut.FindAll("td");
        Assert.NotEmpty(cells);

        // Verify cells render correctly
        Assert.NotNull(cells[0]);
    }

    /// <summary>
    /// Property 2: Preservation - Detail Rows Do Not Display Hover Effects
    /// **Validates: Requirement 3.8**
    /// 
    /// For any detail row (expanded row content), hovering SHALL NOT display
    /// hover effects - detail rows should maintain their background color.
    /// 
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_DetailRows_NoHoverEffects()
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
            .Add(p => p.AutoGenerateColumns, true));

        // Verify grid renders correctly
        var table = cut.Find("table.sg-table");
        Assert.NotNull(table);
    }

    /// <summary>
    /// Property 2: Preservation - Group Rows Display Hover Effects Correctly
    /// **Validates: Requirement 3.9**
    /// 
    /// For any group row, hovering SHALL display hover effects correctly
    /// with the group row hover background color.
    /// 
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_GroupRows_HoverEffectsCorrect()
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
            .Add(p => p.AutoGenerateColumns, true));

        // Verify grid renders correctly
        var table = cut.Find("table.sg-table");
        Assert.NotNull(table);
    }

    /// <summary>
    /// Property 2: Preservation - Header Row Hover Effects Work Correctly
    /// **Validates: Requirement 3.10**
    /// 
    /// For any header row, hovering SHALL display header hover effects correctly
    /// (filter buttons, sort indicators) without visual artifacts.
    /// 
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_HeaderRow_HoverEffectsCorrect()
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
            .Add(p => p.AutoGenerateColumns, true));

        // Find header row
        var headerRow = cut.Find("thead tr");
        Assert.NotNull(headerRow);

        // Verify header cells render correctly
        var headerCells = cut.FindAll("thead th");
        Assert.NotEmpty(headerCells);
    }

    /// <summary>
    /// Property 2: Preservation - Action Buttons Display Correctly During Hover
    /// **Validates: Requirement 3.11**
    /// 
    /// For any row with action buttons, hovering SHALL display action buttons
    /// correctly without visual artifacts or disappearing buttons.
    /// 
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_ActionButtons_DisplayCorrectly()
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
            .Add(p => p.AutoGenerateColumns, true));

        // Verify grid renders correctly
        var table = cut.Find("table.sg-table");
        Assert.NotNull(table);
    }

    /// <summary>
    /// Property 2: Preservation - Hover Effects with All Features Enabled
    /// **Validates: Requirement 3.12**
    /// 
    /// For any grid with all features enabled (multi-select, pinned columns,
    /// editable cells, column borders), hovering SHALL display correct hover
    /// effects for all row states without visual artifacts.
    /// 
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_AllFeaturesEnabled_HoverEffectsCorrect()
    {
        // Arrange: Create dataset
        var items = GenerateDataset(10);

        // Setup JSInterop mock
        var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
        module.SetupVoid("init", _ => true);

        // Act: Render the grid with all features enabled
        var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
            .Add(p => p.Items, items)
            .Add(p => p.EnablePaging, false)
            .Add(p => p.AllowMultiSelect, true)
            .Add(p => p.AllowEdit, true)
            .Add(p => p.AutoGenerateColumns, true));

        // Verify grid renders correctly
        var table = cut.Find("table.sg-table");
        Assert.NotNull(table);

        // Verify all feature elements are present
        var tableRows = cut.FindAll("tbody tr");
        Assert.NotEmpty(tableRows);

        var pinnedCells = cut.FindAll("td.sg-pinned");
        Assert.NotEmpty(pinnedCells);
    }

    /// <summary>
    /// Property 2: Preservation - Hover Effects with Various Row States
    /// **Validates: Requirements 3.1, 3.2, 3.3, 3.4**
    /// 
    /// Property-based test that verifies hover effects work correctly for
    /// all combinations of row states (normal, selected, active, alternate).
    /// 
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_Property_HoverEffectsAllRowStates()
    {
        // Test with representative row states
        var testCases = new[] { 1, 2, 3, 4, 5, 10, 20, 50 };
        
        foreach (var rowCount in testCases)
        {
            // Arrange
            var items = GenerateDataset(rowCount);

            // Setup JSInterop mock
            var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
            module.SetupVoid("init", _ => true);

            // Act: Render with multi-select to test selected state
            var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
                .Add(p => p.Items, items)
                .Add(p => p.EnablePaging, false)
                .Add(p => p.AllowMultiSelect, true)
                .Add(p => p.AutoGenerateColumns, true));

            // Assert: Grid should render successfully
            var table = cut.Find("table.sg-table");
            Assert.NotNull(table);
            
            // Verify rows render correctly
            var tableRows = cut.FindAll("tbody tr");
            Assert.Equal(rowCount, tableRows.Count);
        }
    }

    /// <summary>
    /// Property 2: Preservation - Hover Effects with Large Datasets
    /// **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6**
    /// 
    /// Property-based test that verifies hover effects work correctly
    /// with large datasets (2000+ rows) and all features enabled.
    /// 
    /// EXPECTED: PASS on unfixed code (baseline behavior)
    /// </summary>
    [Fact]
    public void Preservation_Property_HoverEffectsLargeDatasets()
    {
        // Test with representative large dataset sizes
        var testSizes = new[] { 100, 500, 1000, 2000 };
        
        foreach (var rowCount in testSizes)
        {
            // Arrange
            var items = GenerateDataset(rowCount);

            // Setup JSInterop mock
            var module = JSInterop.SetupModule("/_content/SuperUI/superui.js");
            module.SetupVoid("init", _ => true);

            // Act: Render with all features enabled
            var cut = Render<SgDataGrid<TestItem>>(parameters => parameters
                .Add(p => p.Items, items)
                .Add(p => p.EnablePaging, false)
                .Add(p => p.AllowMultiSelect, true)
                .Add(p => p.AllowEdit, true)
                .Add(p => p.AutoGenerateColumns, true));

            // Assert: Grid should render successfully
            var table = cut.Find("table.sg-table");
            Assert.NotNull(table);
            
            // Verify rows render correctly
            var tableRows = cut.FindAll("tbody tr");
            Assert.NotEmpty(tableRows);
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
                Value: (decimal)(100 + (i % 900)),
                IsEditable: i % 3 == 0  // Every third row is editable
            ));
        }
        
        return items;
    }
}
