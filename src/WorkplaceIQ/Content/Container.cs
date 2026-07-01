namespace WorkplaceIQ.Content;

public abstract class Container : Content
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? VectorCollectionName { get; set; }
    public string? RendererKey { get; set; }
    public string Status { get; set; } = "active";
    public string? SettingsJson { get; set; }
    public bool IsSystemGenerated { get; set; }
    public ICollection<ContentItem> Items { get; set; } = [];
}
