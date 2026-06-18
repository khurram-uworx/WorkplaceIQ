namespace WorkplaceIQ.Tests.TestDoubles;

using WorkplaceIQ;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Content;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Metrics;
using WorkplaceIQ.Posts;

internal sealed class InMemoryWorkplaceIqStore : IWorkplaceIqStore
{
    public List<Container> Containers { get; } = [];

    public List<Post> Posts { get; } = [];

    public List<Label> Labels { get; } = [];

    public List<ContentItem> ContentItems { get; } = [];

    public List<MetricDefinition> MetricDefinitions { get; } = [];

    public Task<Container?> GetContainerByKeyAsync(
        string key,
        string type,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Containers.FirstOrDefault(container =>
            container.Key == key && container.Type == type));
    }

    public Task<Container> CreateContainerAsync(
        string key,
        string type,
        string title,
        CancellationToken cancellationToken = default)
    {
        var container = new Container
        {
            Key = key,
            Type = type,
            Title = title,
            RendererKey = type
        };

        Containers.Add(container);

        return Task.FromResult(container);
    }

    public Task<IReadOnlyList<Post>> GetPostsAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Post>>(
            Posts.Where(post => post.ContainerId == containerId).ToList());
    }

    public Task<Post> CreatePostAsync(
        Guid containerId,
        string title,
        string body,
        IReadOnlyList<LabelName> labels,
        Guid? contentId = null,
        string? postType = null,
        string? authorUserId = null,
        bool isSystemGenerated = false,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        var containerType = Containers.FirstOrDefault(container => container.Id == containerId)?.Type;
        var post = new Post
        {
            ContainerId = containerId,
            Title = title,
            Body = body,
            ContentId = contentId,
            PostType = postType ?? InferPostType(contentId, containerType),
            AuthorUserId = authorUserId,
            IsSystemGenerated = isSystemGenerated,
            MetadataJson = metadataJson
        };

        foreach (var labelName in labels)
        {
            var label = Labels.FirstOrDefault(candidate =>
                candidate.NormalizedName == labelName.NormalizedName);

            if (label is null)
            {
                label = new Label
                {
                    Name = labelName.Name,
                    NormalizedName = labelName.NormalizedName,
                    Slug = labelName.Slug
                };
                Labels.Add(label);
            }

            post.PostLabels.Add(new PostLabel
            {
                Post = post,
                PostId = post.Id,
                Label = label,
                LabelId = label.Id
            });
        }

        Posts.Add(post);

        return Task.FromResult(post);
    }

    private static string InferPostType(Guid? contentId, string? containerType)
    {
        if (contentId.HasValue)
        {
            return PostTypes.Comment;
        }

        return containerType == ContainerTypes.Forum
            ? PostTypes.Thread
            : PostTypes.Post;
    }

    public Task<IReadOnlyList<ContentItem>> GetContentByContainerAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<ContentItem>>(
            ContentItems.Where(c => c.ContainerId == containerId).ToList());
    }

    public Task<ContentItem?> GetContentByIdAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ContentItems.FirstOrDefault(c => c.Id == contentItemId));
    }

    public Task<ContentItem> CreateContentAsync(
        ContentItem item,
        CancellationToken cancellationToken = default)
    {
        ContentItems.Add(item);
        return Task.FromResult(item);
    }

    public Task<ContentItem> UpdateContentAsync(
        ContentItem item,
        CancellationToken cancellationToken = default)
    {
        var index = ContentItems.FindIndex(c => c.Id == item.Id);
        if (index >= 0)
        {
            ContentItems[index] = item;
        }
        return Task.FromResult(item);
    }

    public Task<MetricDefinition?> GetMetricDefinitionByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(MetricDefinitions.FirstOrDefault(m => m.Name == name));
    }
}
