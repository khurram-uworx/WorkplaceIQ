using WorkplaceIQ.Content;

namespace WorkplaceIQ.Files;

public sealed record FileComponentResult(
    Content.Content? Container,
    IReadOnlyList<FileObject> Files,
    bool Created,
    bool Missing,
    string DisplayTitle);
