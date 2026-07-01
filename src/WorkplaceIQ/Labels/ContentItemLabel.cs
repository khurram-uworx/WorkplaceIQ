using WorkplaceIQ.Content;

namespace WorkplaceIQ.Labels;

public sealed class ContentItemLabel
{
    public Guid ContentItemId { get; set; }
    public ContentItem? ContentItem { get; set; }
    public Guid LabelId { get; set; }
    public Label? Label { get; set; }
}
