using WorkplaceIQ.Components;
using WorkplaceIQ.Content;

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
                "ForumContainer",
                request.AutoProvision,
                "forum"),
            cancellationToken);

        return new ForumComponentResult(
            result.Container as DiscussionContent,
            result.Items,
            result.Created,
            result.Missing,
            result.DisplayTitle);
    }

    public Task<ContentItem> CreateThreadAsync(
        string forumId,
        string title,
        string body,
        string? labels = null,
        CancellationToken cancellationToken = default)
    {
        return componentService.CreatePostAsync(
            forumId,
            "ForumContainer",
            "forum",
            title,
            body,
            labels,
            discriminator: "topic",
            cancellationToken: cancellationToken);
    }
}
