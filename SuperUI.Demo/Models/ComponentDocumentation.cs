using SuperUI.Components;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SuperUI.Demo.Models;

// Button Component
public class SgButtonDoc
{
    [SgProperty(Category = "Content", Description = "Text displayed on the button")]
    public string Text { get; set; } = "Click me";

    [SgProperty(Category = "Appearance", Description = "Button style variant")]
    public string Variant { get; set; } = "default";

    [SgProperty(Category = "Appearance", Description = "Button size")]
    public string Size { get; set; } = "md";

    [SgProperty(Category = "Appearance", Description = "Button type")]
    public string Type { get; set; } = "button";

    [SgProperty(Category = "State", Description = "Disable the button")]
    public bool Disabled { get; set; } = false;

    [SgProperty(Category = "State", Description = "Show loading state")]
    public bool Loading { get; set; } = false;

    [SgProperty(Category = "Layout", Description = "Full width button")]
    public bool Block { get; set; } = false;

    [SgProperty(Category = "Content", Description = "Tooltip text")]
    public string Title { get; set; } = "";
}

// TextBox Component
public class SgTextBoxDoc
{
    [SgProperty(Category = "Content", Description = "Current value")]
    public string Value { get; set; } = "";

    [SgProperty(Category = "Labels", Description = "Field label")]
    public string Label { get; set; } = "Input";

    [SgProperty(Category = "Labels", Description = "Placeholder text")]
    public string Placeholder { get; set; } = "Enter text...";

    [SgProperty(Category = "Labels", Description = "Helper text")]
    public string Hint { get; set; } = "";

    [SgProperty(Category = "Validation", Description = "Error message")]
    public string ErrorText { get; set; } = "";

    [SgProperty(Category = "Appearance", Description = "Input type")]
    public string Type { get; set; } = "text";

    [SgProperty(Category = "Appearance", Description = "Enable multiline")]
    public bool Multiline { get; set; } = false;

    [SgProperty(Category = "Appearance", Description = "Number of rows")]
    public int Rows { get; set; } = 3;

    [SgProperty(Category = "Validation", Description = "Field is required")]
    public bool Required { get; set; } = false;

    [SgProperty(Category = "State", Description = "Disable the field")]
    public bool Disabled { get; set; } = false;

    [SgProperty(Category = "State", Description = "Read-only mode")]
    public bool ReadOnly { get; set; } = false;

    [SgProperty(Category = "Layout", Description = "Full width field")]
    public bool Block { get; set; } = false;
}

// Select Component
public class SgSelectDoc
{
    [SgProperty(Category = "Content", Description = "Current selected value")]
    public string Value { get; set; } = "";

    [SgProperty(Category = "Labels", Description = "Field label")]
    public string Label { get; set; } = "Select";

    [SgProperty(Category = "Labels", Description = "Placeholder text")]
    public string Placeholder { get; set; } = "Choose...";

    [SgProperty(Category = "Labels", Description = "Helper text")]
    public string Hint { get; set; } = "";

    [SgProperty(Category = "Validation", Description = "Error message")]
    public string ErrorText { get; set; } = "";

    [SgProperty(Category = "Features", Description = "Enable search")]
    public bool Searchable { get; set; } = false;

    [SgProperty(Category = "Features", Description = "Allow clearing value")]
    public bool AllowClear { get; set; } = false;

    [SgProperty(Category = "Validation", Description = "Field is required")]
    public bool Required { get; set; } = false;

    [SgProperty(Category = "State", Description = "Disable the field")]
    public bool Disabled { get; set; } = false;

    [SgProperty(Category = "Layout", Description = "Full width field")]
    public bool Block { get; set; } = false;
}

// Breadcrumb Component
public class SgBreadcrumbDoc
{
    [SgProperty(Category = "Appearance", Description = "Separator character")]
    public string Separator { get; set; } = "/";

    [SgProperty(Category = "Content", Description = "Breadcrumb items")]
    public string Items { get; set; } = "IEnumerable<BreadcrumbItem>";
}

// Stepper Component
public class SgStepperDoc
{
    [SgProperty(Category = "Content", Description = "List of steps")]
    public string Steps { get; set; } = "IEnumerable<StepperItem>";

    [SgProperty(Category = "State", Description = "Currently active step index")]
    public int Active { get; set; } = 0;

    [SgProperty(Category = "Appearance", Description = "Vertical orientation")]
    public bool Vertical { get; set; } = false;

    [SgProperty(Category = "Interaction", Description = "Allow clicking steps")]
    public bool Clickable { get; set; } = true;
}

// Tabs Component
public class SgTabsDoc
{
    [SgProperty(Category = "Content", Description = "Tab panels")]
    public string ChildContent { get; set; } = "RenderFragment";

    [SgProperty(Category = "State", Description = "Active tab title")]
    public string ActiveTitle { get; set; } = "";

    [SgProperty(Category = "Appearance", Description = "Tab position")]
    public string Position { get; set; } = "top";

    [SgProperty(Category = "Appearance", Description = "Tab type")]
    public string Type { get; set; } = "line";
}

// Pagination Component
public class SgPaginationDoc
{
    [SgProperty(Category = "Data", Description = "Total number of items")]
    public int TotalItems { get; set; } = 100;

    [SgProperty(Category = "Data", Description = "Items per page")]
    public int PageSize { get; set; } = 10;

    [SgProperty(Category = "State", Description = "Current page")]
    public int Page { get; set; } = 1;

    [SgProperty(Category = "Appearance", Description = "Show item count")]
    public bool ShowInfo { get; set; } = false;

    [SgProperty(Category = "Appearance", Description = "Compact mode")]
    public bool Simple { get; set; } = false;
}

// Menu Component
public class SgMenuDoc
{
    [SgProperty(Category = "Content", Description = "Menu items")]
    public string ChildContent { get; set; } = "RenderFragment";

    [SgProperty(Category = "Appearance", Description = "Custom CSS class")]
    public string CssClass { get; set; } = "";
}

// Calendar Component
public class SgCalendarDoc
{
    [SgProperty(Category = "Data", Description = "Selected date")]
    public string Value { get; set; } = "DateTime";

    [SgProperty(Category = "Data", Description = "Calendar events")]
    public string Events { get; set; } = "IEnumerable<SgCalendarEvent>";

    [SgProperty(Category = "Appearance", Description = "Display mode")]
    public string View { get; set; } = "Month";

    [SgProperty(Category = "Appearance", Description = "Calendar height")]
    public string Height { get; set; } = "auto";

    [SgProperty(Category = "Features", Description = "Allow event creation")]
    public bool EnableCreation { get; set; } = false;

    [SgProperty(Category = "Features", Description = "Show weekends")]
    public bool ShowWeekends { get; set; } = true;
}

// DataGrid Component
public class SgDataGridDoc
{
    [SgProperty(Category = "Data", Description = "Grid data items")]
    public string Items { get; set; } = "IEnumerable<T>";

    [SgProperty(Category = "Data", Description = "Column definitions")]
    public string Columns { get; set; } = "IEnumerable<DataGridColumn>";

    [SgProperty(Category = "Appearance", Description = "Grid height")]
    public string Height { get; set; } = "auto";

    [SgProperty(Category = "Features", Description = "Enable sorting")]
    public bool Sortable { get; set; } = true;

    [SgProperty(Category = "Features", Description = "Enable filtering")]
    public bool Filterable { get; set; } = true;

    [SgProperty(Category = "Features", Description = "Enable pagination")]
    public bool Pageable { get; set; } = true;

    [SgProperty(Category = "Selection", Description = "Allow row selection")]
    public bool Selectable { get; set; } = false;

    [SgProperty(Category = "Selection", Description = "Multiple row selection")]
    public bool MultiSelect { get; set; } = false;
}

// Kanban Component
public class SgKanbanDoc
{
    [SgProperty(Category = "Data", Description = "Kanban columns")]
    public string Columns { get; set; } = "List<SgKanbanColumn>";

    [SgProperty(Category = "Data", Description = "Kanban tasks")]
    public string Tasks { get; set; } = "List<SgKanbanTask>";

    [SgProperty(Category = "Features", Description = "Enable drag and drop")]
    public bool Draggable { get; set; } = true;

    [SgProperty(Category = "Features", Description = "Allow task editing")]
    public bool Editable { get; set; } = true;

    [SgProperty(Category = "Appearance", Description = "Custom CSS class")]
    public string CssClass { get; set; } = "";
}

// Gantt Component
public class SgGanttDoc
{
    [SgProperty(Category = "Data", Description = "Gantt tasks")]
    public string Tasks { get; set; } = "List<SgGanttTask>";

    [SgProperty(Category = "Appearance", Description = "Day width in pixels")]
    public int DayWidth { get; set; } = 24;

    [SgProperty(Category = "Appearance", Description = "Custom CSS class")]
    public string CssClass { get; set; } = "";
}

// TreeView Component
public class SgTreeViewDoc
{
    [SgProperty(Category = "Data", Description = "Tree nodes")]
    public string Items { get; set; } = "IEnumerable<TreeNode>";

    [SgProperty(Category = "Features", Description = "Enable node selection")]
    public bool Selectable { get; set; } = true;

    [SgProperty(Category = "Features", Description = "Enable node expansion")]
    public bool Expandable { get; set; } = true;

    [SgProperty(Category = "Appearance", Description = "Show icons")]
    public bool ShowIcons { get; set; } = true;

    [SgProperty(Category = "Appearance", Description = "Custom CSS class")]
    public string CssClass { get; set; } = "";
}

// PropertyGrid Component
public class SgPropertyGridDoc
{
    [SgProperty(Category = "Data", Description = "Object to edit")]
    public string SelectedObject { get; set; } = "object";

    [SgProperty(Category = "Appearance", Description = "Show toolbar")]
    public bool ShowToolbar { get; set; } = true;

    [SgProperty(Category = "Appearance", Description = "Group by category")]
    public bool GroupByLines { get; set; } = true;

    [SgProperty(Category = "Validation", Description = "Show validation")]
    public bool ShowValidation { get; set; } = true;

    [SgProperty(Category = "Validation", Description = "Show validation summary")]
    public bool ShowValidationSummary { get; set; } = true;

    [SgProperty(Category = "Validation", Description = "Show required indicator")]
    public bool ShowRequiredIndicator { get; set; } = true;

    [SgProperty(Category = "Labels", Description = "Search placeholder")]
    public string SearchPlaceholder { get; set; } = "Search properties";

    [SgProperty(Category = "Labels", Description = "Empty text")]
    public string EmptyText { get; set; } = "No properties found";
}
