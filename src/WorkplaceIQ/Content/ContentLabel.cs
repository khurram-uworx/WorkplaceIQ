using WorkplaceIQ.Labels;

namespace WorkplaceIQ.Content;

public sealed class ContentLabel
{
    public Guid ContentItemId { get; set; }

    public ContentItem? ContentItem { get; set; }

    public Guid LabelId { get; set; }

    public Label? Label { get; set; }
}

