namespace WorkplaceIQ.Entities;

public sealed record EntityCreateRequest(
    string EntityListId,
    string EntityType,
    string Name,
    string Title,
    string? Description = null,
    string? Status = null,
    string? MetadataJson = null,
    string? Labels = null);
