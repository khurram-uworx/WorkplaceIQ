using Microsoft.EntityFrameworkCore;
using WorkplaceIQ;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Content;
using WorkplaceIQ.Entities;
using WorkplaceIQ.Files;
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

    public async Task<IReadOnlyList<Container>> GetContainersAsync(
        string? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Containers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(container => container.Type == type);
        }

        return await query
            .OrderBy(container => container.Title)
            .ToListAsync(cancellationToken);
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

    public Task<Post?> GetPostByIdAsync(
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Posts
            .AsNoTracking()
            .Include(post => post.PostLabels)
                .ThenInclude(postLabel => postLabel.Label)
            .FirstOrDefaultAsync(post => post.Id == postId, cancellationToken);
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

    public async Task<Post> UpdatePostAsync(
        Post post,
        CancellationToken cancellationToken = default)
    {
        dbContext.Posts.Update(post);
        await dbContext.SaveChangesAsync(cancellationToken);
        return post;
    }

    public async Task DeletePostAsync(
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        var post = await dbContext.Posts.FirstOrDefaultAsync(candidate => candidate.Id == postId, cancellationToken);
        if (post is null)
        {
            return;
        }

        dbContext.Posts.Remove(post);
        await dbContext.SaveChangesAsync(cancellationToken);
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
            .Include(c => c.Posts)
                .ThenInclude(post => post.PostLabels)
                    .ThenInclude(postLabel => postLabel.Label)
            .Where(c => c.ContainerId == containerId)
            .Where(c => c.Status != "archived")
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
            .Include(c => c.Posts)
                .ThenInclude(post => post.PostLabels)
                    .ThenInclude(postLabel => postLabel.Label)
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

    public async Task DeleteContentAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.ContentItems.FirstOrDefaultAsync(candidate => candidate.Id == contentItemId, cancellationToken);
        if (item is null)
        {
            return;
        }

        item.Status = "archived";
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FileObject>> GetFilesByContainerAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.FileRecords
            .AsNoTracking()
            .Include(file => file.ContentItem!)
                .ThenInclude(content => content.ContentLabels)
                    .ThenInclude(contentLabel => contentLabel.Label)
            .Include(file => file.ContentItem!)
                .ThenInclude(content => content.Posts)
                    .ThenInclude(post => post.PostLabels)
                        .ThenInclude(postLabel => postLabel.Label)
            .Where(file => file.ContentItem != null)
            .Where(file => file.ContentItem!.ContainerId == containerId)
            .Where(file => file.ContentItem!.ContentType == FileContentTypes.File)
            .Where(file => file.ContentItem!.Status != "archived")
            .ToListAsync(cancellationToken);

        return rows
            .Select(file => new FileObject(file.ContentItem!, file))
            .OrderByDescending(file => file.ContentItem.UpdatedAt)
            .ToList();
    }

    public async Task<FileObject?> GetFileByContentIdAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default)
    {
        var file = await dbContext.FileRecords
            .AsNoTracking()
            .Include(candidate => candidate.ContentItem!)
                .ThenInclude(content => content.ContentLabels)
                    .ThenInclude(contentLabel => contentLabel.Label)
            .Include(candidate => candidate.ContentItem!)
                .ThenInclude(content => content.Posts)
                    .ThenInclude(post => post.PostLabels)
                        .ThenInclude(postLabel => postLabel.Label)
            .FirstOrDefaultAsync(candidate => candidate.ContentItemId == contentItemId, cancellationToken);

        if (file?.ContentItem is null || file.ContentItem.Status == "archived")
        {
            return null;
        }

        return new FileObject(file.ContentItem, file);
    }

    public async Task<FileObject> CreateFileRecordAsync(
        FileRecord fileRecord,
        CancellationToken cancellationToken = default)
    {
        dbContext.FileRecords.Add(fileRecord);
        await dbContext.SaveChangesAsync(cancellationToken);

        var file = await GetFileByContentIdAsync(fileRecord.ContentItemId, cancellationToken);
        return file ?? throw new InvalidOperationException($"File content '{fileRecord.ContentItemId}' was not found after creation.");
    }

    public async Task<IReadOnlyList<BusinessEntity>> GetEntitiesByContainerAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.Entities
            .AsNoTracking()
            .Include(entity => entity.EntityLabels)
                .ThenInclude(entityLabel => entityLabel.Label)
            .Include(entity => entity.SourceRelationships)
                .ThenInclude(relationship => relationship.TargetEntity)
            .Where(entity => entity.ContainerId == containerId)
            .Where(entity => entity.Status != EntityStatuses.Archived)
            .ToListAsync(cancellationToken);

        return entities
            .OrderBy(entity => entity.Title)
            .ToList();
    }

    public Task<BusinessEntity?> GetEntityByIdAsync(
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Entities
            .AsNoTracking()
            .Include(entity => entity.EntityLabels)
                .ThenInclude(entityLabel => entityLabel.Label)
            .Include(entity => entity.SourceRelationships)
                .ThenInclude(relationship => relationship.TargetEntity)
            .Include(entity => entity.TargetRelationships)
                .ThenInclude(relationship => relationship.SourceEntity)
            .FirstOrDefaultAsync(
                entity => entity.Id == entityId && entity.Status != EntityStatuses.Archived,
                cancellationToken);
    }

    public async Task<BusinessEntity> CreateEntityAsync(
        BusinessEntity entity,
        IReadOnlyList<LabelName> labels,
        CancellationToken cancellationToken = default)
    {
        dbContext.Entities.Add(entity);

        foreach (var labelName in labels)
        {
            var label = await GetOrCreateLabelAsync(labelName, cancellationToken);
            entity.EntityLabels.Add(new EntityLabel
            {
                Entity = entity,
                Label = label
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<EntityRelationship> CreateEntityRelationshipAsync(
        EntityRelationship relationship,
        CancellationToken cancellationToken = default)
    {
        dbContext.EntityRelationships.Add(relationship);
        await dbContext.SaveChangesAsync(cancellationToken);
        return relationship;
    }

    public async Task AddLabelToContentAsync(
        Guid contentItemId,
        LabelName label,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetOrCreateLabelAsync(label, cancellationToken);
        var exists = await dbContext.ContentLabels.AnyAsync(
            contentLabel => contentLabel.ContentItemId == contentItemId && contentLabel.LabelId == entity.Id,
            cancellationToken);

        if (!exists)
        {
            dbContext.ContentLabels.Add(new ContentLabel
            {
                ContentItemId = contentItemId,
                LabelId = entity.Id
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task AddLabelToEntityAsync(
        Guid entityId,
        LabelName label,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetOrCreateLabelAsync(label, cancellationToken);
        var exists = await dbContext.EntityLabels.AnyAsync(
            entityLabel => entityLabel.EntityId == entityId && entityLabel.LabelId == entity.Id,
            cancellationToken);

        if (!exists)
        {
            dbContext.EntityLabels.Add(new EntityLabel
            {
                EntityId = entityId,
                LabelId = entity.Id
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task AddLabelToPostAsync(
        Guid postId,
        LabelName label,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetOrCreateLabelAsync(label, cancellationToken);
        var exists = await dbContext.PostLabels.AnyAsync(
            postLabel => postLabel.PostId == postId && postLabel.LabelId == entity.Id,
            cancellationToken);

        if (!exists)
        {
            dbContext.PostLabels.Add(new PostLabel
            {
                PostId = postId,
                LabelId = entity.Id
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<Label> GetOrCreateLabelAsync(
        LabelName labelName,
        CancellationToken cancellationToken)
    {
        var label = await dbContext.Labels.FirstOrDefaultAsync(
            candidate => candidate.NormalizedName == labelName.NormalizedName,
            cancellationToken);

        if (label is not null)
        {
            return label;
        }

        label = new Label
        {
            Name = labelName.Name,
            NormalizedName = labelName.NormalizedName,
            Slug = labelName.Slug,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.Labels.Add(label);
        await dbContext.SaveChangesAsync(cancellationToken);
        return label;
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
