using WorkplaceIQ.Posts;

namespace WorkplaceIQ.Labels;

public sealed class PostLabel
{
    public Guid PostId { get; set; }

    public Post? Post { get; set; }

    public Guid LabelId { get; set; }

    public Label? Label { get; set; }
}
