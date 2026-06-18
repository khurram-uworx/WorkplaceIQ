using WorkplaceIQ.Files;

namespace WorkplaceIQ.Entities;

public sealed class EntityFileLink
{
    public Guid EntityId { get; set; }

    public BusinessEntity? Entity { get; set; }

    public Guid FileRecordId { get; set; }

    public FileRecord? FileRecord { get; set; }
}
