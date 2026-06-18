using WorkplaceIQ.Labels;

namespace WorkplaceIQ.Entities;

public sealed class EntityLabel
{
    public Guid EntityId { get; set; }

    public BusinessEntity? Entity { get; set; }

    public Guid LabelId { get; set; }

    public Label? Label { get; set; }
}
