namespace WorkplaceIQ.Files;

public sealed record FileObject(
    Content.Content Content,
    FileRecord FileRecord);
