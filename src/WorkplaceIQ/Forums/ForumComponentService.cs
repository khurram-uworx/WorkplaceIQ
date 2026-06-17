using WorkplaceIQ.Components;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.Forums;

public sealed class ForumComponentService(IComponentService componentService) : IForumComponentService
{
    public async Task<ForumComponentResult> ResolveForumAsync(
        ForumComponentRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await componentService.ResolveAsync(
            new ComponentRequest(
                request.Id,
                request.Title,
                ContainerTypes.Forum,
                request.AutoProvision,
                "forum"),
            cancellationToken);

        return new ForumComponentResult(
            result.Container,
            result.Posts,
            result.Created,
            result.Missing,
            result.DisplayTitle);
    }

    public Task<Post> CreateThreadAsync(
        string forumId,
        string title,
        string body,
        string? labels = null,
        CancellationToken cancellationToken = default)
    {
        return componentService.CreatePostAsync(
            forumId,
            ContainerTypes.Forum,
            "forum",
            title,
            body,
            labels,
            cancellationToken);
    }
}
