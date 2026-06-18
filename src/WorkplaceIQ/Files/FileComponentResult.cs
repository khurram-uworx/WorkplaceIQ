using WorkplaceIQ.Containers;

namespace WorkplaceIQ.Files;

public sealed record FileComponentResult(
    Container? Container,
    IReadOnlyList<FileObject> Files,
    bool Created,
    bool Missing,
    string DisplayTitle);
