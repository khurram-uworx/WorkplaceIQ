using WorkplaceIQ.Content;

namespace WorkplaceIQ.Files;

public sealed record FileComponentResult(
    FolderContent? Container,
    IReadOnlyList<FileObject> Files,
    bool Created,
    bool Missing,
    string DisplayTitle);
