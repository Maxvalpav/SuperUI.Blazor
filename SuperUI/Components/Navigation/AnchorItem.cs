namespace SuperUI.Components;

/// <summary>Represents a single anchor link item for the <see cref="SgAnchor"/> component.</summary>
public class AnchorItem
{
    /// <summary>The target element ID to scroll to.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Display text for the anchor link.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Heading level (2 for h2, 3 for h3, etc.).</summary>
    public int Level { get; set; }
    /// <summary>Whether this item is the currently active anchor.</summary>
    public bool IsActive { get; set; }
    /// <summary>Optional icon displayed before the link text.</summary>
    public string? Icon { get; set; }
    
    public AnchorItem() { }
    
    public AnchorItem(string id, string title, int level, bool isActive = false, string? icon = null)
    {
        Id = id;
        Title = title;
        Level = level;
        IsActive = isActive;
        Icon = icon;
    }
    
    /// <summary>
    /// Returns a new <see cref="AnchorItem"/> with the specified active state.
    /// </summary>
    public AnchorItem WithIsActive(bool isActive)
    {
        return new AnchorItem(Id, Title, Level, isActive, Icon);
    }
}