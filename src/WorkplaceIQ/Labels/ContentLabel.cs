using WorkplaceIQ.Content;

namespace WorkplaceIQ.Labels;

public sealed class ContentLabel
{
    public Guid ContentId { get; set; }
    public Content.Content? Content { get; set; }
    public Guid LabelId { get; set; }
    public Label? Label { get; set; }
}
