namespace SuperUI.Components;

public sealed class BreadcrumbItem
{
    public BreadcrumbItem() { }

    public BreadcrumbItem(string text, string? href = null)
    {
        Text = text;
        Href = href;
    }

    public string Text { get; set; } = string.Empty;
    public string? Href { get; set; }
}
