using Microsoft.EntityFrameworkCore;
using WorkplaceIQ;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Content;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Metrics;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.AspNet.Data;

public sealed class EfWorkplaceIqStore(WorkplaceIqDbContext dbContext) : IWorkplaceIqStore
{
    public Task<Container?> GetContainerByKeyAsync(
        string key,
        string type,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Containers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                container => container.Key == key && container.Type == type,
                cancellationToken);
    }

    public async Task<Container> CreateContainerAsync(
        string key,
        string type,
        string title,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var container = new Container
        {
            Key = key,
            Type = type,
            Title = title,
            RendererKey = type,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Containers.Add(container);
        await dbContext.SaveChangesAsync(cancellationToken);

        return container;
    }

    public async Task<IReadOnlyList<Post>> GetPostsAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        var posts = await dbContext.Posts
            .AsNoTracking()
            .Include(post => post.PostLabels)
                .ThenInclude(postLabel => postLabel.Label)
            .Where(post => post.ContainerId == containerId)
            .ToListAsync(cancellationToken);

        return posts
            .OrderByDescending(post => post.CreatedAt)
            .ToList();
    }

    public async Task<Post> CreatePostAsync(
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
        var containerType = await dbContext.Containers
            .AsNoTracking()
            .Where(container => container.Id == containerId)
            .Select(container => container.Type)
            .FirstOrDefaultAsync(cancellationToken);

        var post = new Post
        {
            ContainerId = containerId,
            Title = title,
            Body = body,
            ContentId = contentId,
            PostType = postType ?? InferPostType(contentId, containerType),
            AuthorUserId = authorUserId,
            IsSystemGenerated = isSystemGenerated,
            MetadataJson = metadataJson,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Posts.Add(post);

        foreach (var labelName in labels)
        {
            var label = await dbContext.Labels.FirstOrDefaultAsync(
                candidate => candidate.NormalizedName == labelName.NormalizedName,
                cancellationToken);

            if (label is null)
            {
                label = new Label
                {
                    Name = labelName.Name,
                    NormalizedName = labelName.NormalizedName,
                    Slug = labelName.Slug,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                dbContext.Labels.Add(label);
            }

            post.PostLabels.Add(new PostLabel
            {
                Post = post,
                Label = label
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return post;
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

    public async Task<IReadOnlyList<ContentItem>> GetContentByContainerAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        var items = await dbContext.ContentItems
            .AsNoTracking()
            .Include(c => c.ContentLabels)
                .ThenInclude(cl => cl.Label)
            .Where(c => c.ContainerId == containerId)
            .ToListAsync(cancellationToken);

        return items
            .OrderByDescending(c => c.CreatedAt)
            .ToList();
    }

    public Task<ContentItem?> GetContentByIdAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ContentItems
            .AsNoTracking()
            .Include(c => c.ContentLabels)
                .ThenInclude(cl => cl.Label)
            .FirstOrDefaultAsync(c => c.Id == contentItemId, cancellationToken);
    }

    public async Task<ContentItem> CreateContentAsync(
        ContentItem item,
        CancellationToken cancellationToken = default)
    {
        dbContext.ContentItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<ContentItem> UpdateContentAsync(
        ContentItem item,
        CancellationToken cancellationToken = default)
    {
        dbContext.ContentItems.Update(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return item;
    }

    public Task<MetricDefinition?> GetMetricDefinitionByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return dbContext.MetricDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Name == name, cancellationToken);
    }
}
