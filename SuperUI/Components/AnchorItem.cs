namespace SuperUI.Components;

public class AnchorItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool IsActive { get; set; }
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
    
    public AnchorItem WithIsActive(bool isActive)
    {
        return new AnchorItem(Id, Title, Level, isActive, Icon);
    }
}