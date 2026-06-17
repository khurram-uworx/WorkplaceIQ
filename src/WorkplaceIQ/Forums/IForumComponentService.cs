using WorkplaceIQ.Posts;

namespace WorkplaceIQ.Forums;

public interface IForumComponentService
{
    Task<ForumComponentResult> ResolveForumAsync(
        ForumComponentRequest request,
        CancellationToken cancellationToken = default);

    Task<Post> CreateThreadAsync(
        string forumId,
        string title,
        string body,
        string? labels = null,
        CancellationToken cancellationToken = default);
}
