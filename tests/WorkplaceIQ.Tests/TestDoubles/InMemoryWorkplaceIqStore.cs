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

    public Task<IReadOnlyList<Container>> GetContainersAsync(
        string? type = null,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Container>>(
            Containers
                .Where(container => string.IsNullOrWhiteSpace(type) || container.Type == type)
                .OrderBy(container => container.Title)
                .ToList());
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

    public Task<Post?> GetPostByIdAsync(
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Posts.FirstOrDefault(post => post.Id == postId));
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

    public Task<Post> UpdatePostAsync(
        Post post,
        CancellationToken cancellationToken = default)
    {
        var index = Posts.FindIndex(candidate => candidate.Id == post.Id);
        if (index >= 0)
        {
            Posts[index] = post;
        }

        return Task.FromResult(post);
    }

    public Task DeletePostAsync(
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        Posts.RemoveAll(post => post.Id == postId);
        return Task.CompletedTask;
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

    public Task DeleteContentAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default)
    {
        var item = ContentItems.FirstOrDefault(content => content.Id == contentItemId);
        if (item is not null)
        {
            item.Status = "archived";
        }

        return Task.CompletedTask;
    }

    public Task AddLabelToContentAsync(
        Guid contentItemId,
        LabelName label,
        CancellationToken cancellationToken = default)
    {
        var content = ContentItems.FirstOrDefault(item => item.Id == contentItemId);
        if (content is null)
        {
            return Task.CompletedTask;
        }

        var entity = GetOrCreateLabel(label);
        content.ContentLabels.Add(new ContentLabel
        {
            ContentItem = content,
            ContentItemId = content.Id,
            Label = entity,
            LabelId = entity.Id
        });
        return Task.CompletedTask;
    }

    public Task AddLabelToPostAsync(
        Guid postId,
        LabelName label,
        CancellationToken cancellationToken = default)
    {
        var post = Posts.FirstOrDefault(item => item.Id == postId);
        if (post is null)
        {
            return Task.CompletedTask;
        }

        var entity = GetOrCreateLabel(label);
        post.PostLabels.Add(new PostLabel
        {
            Post = post,
            PostId = post.Id,
            Label = entity,
            LabelId = entity.Id
        });
        return Task.CompletedTask;
    }

    private Label GetOrCreateLabel(LabelName labelName)
    {
        var label = Labels.FirstOrDefault(candidate => candidate.NormalizedName == labelName.NormalizedName);
        if (label is not null)
        {
            return label;
        }

        label = new Label
        {
            Name = labelName.Name,
            NormalizedName = labelName.NormalizedName,
            Slug = labelName.Slug
        };
        Labels.Add(label);
        return label;
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
