using WorkplaceIQ.Content;

namespace WorkplaceIQ.Files;

public sealed record FileObject(
    ContentItem ContentItem,
    ContentFile ContentFile);
