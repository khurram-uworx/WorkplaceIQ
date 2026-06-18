namespace WorkplaceIQ.Tests.TestDoubles;

using WorkplaceIQ;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Content;
using WorkplaceIQ.Entities;
using WorkplaceIQ.Files;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Metrics;
using WorkplaceIQ.Posts;

internal sealed class InMemoryWorkplaceIqStore : IWorkplaceIqStore
{
    public List<Container> Containers { get; } = [];

    public List<Post> Posts { get; } = [];

    public List<Label> Labels { get; } = [];

    public List<ContentItem> ContentItems { get; } = [];

    public List<FileRecord> FileRecords { get; } = [];

    public List<BusinessEntity> Entities { get; } = [];

    public List<EntityRelationship> EntityRelationships { get; } = [];

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

    public Task<IReadOnlyList<FileObject>> GetFilesByContainerAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<FileObject>>(
            FileRecords
                .Select(file => new
                {
                    FileRecord = file,
                    ContentItem = ContentItems.FirstOrDefault(content => content.Id == file.ContentItemId)
                })
                .Where(row => row.ContentItem is not null)
                .Where(row => row.ContentItem!.ContainerId == containerId)
                .Where(row => row.ContentItem!.ContentType == FileContentTypes.File)
                .Where(row => row.ContentItem!.Status != "archived")
                .OrderByDescending(row => row.ContentItem!.UpdatedAt)
                .Select(row => new FileObject(row.ContentItem!, row.FileRecord))
                .ToList());
    }

    public Task<FileObject?> GetFileByContentIdAsync(
        Guid contentItemId,
        CancellationToken cancellationToken = default)
    {
        var file = FileRecords.FirstOrDefault(candidate => candidate.ContentItemId == contentItemId);
        var content = ContentItems.FirstOrDefault(candidate => candidate.Id == contentItemId);
        if (file is null || content is null || content.Status == "archived")
        {
            return Task.FromResult<FileObject?>(null);
        }

        return Task.FromResult<FileObject?>(new FileObject(content, file));
    }

    public Task<FileObject> CreateFileRecordAsync(
        FileRecord fileRecord,
        CancellationToken cancellationToken = default)
    {
        FileRecords.Add(fileRecord);
        var content = ContentItems.First(content => content.Id == fileRecord.ContentItemId);
        return Task.FromResult(new FileObject(content, fileRecord));
    }

    public Task<IReadOnlyList<BusinessEntity>> GetEntitiesByContainerAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<BusinessEntity>>(
            Entities
                .Where(entity => entity.ContainerId == containerId)
                .Where(entity => entity.Status != EntityStatuses.Archived)
                .OrderBy(entity => entity.Title)
                .ToList());
    }

    public Task<BusinessEntity?> GetEntityByIdAsync(
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Entities.FirstOrDefault(entity =>
            entity.Id == entityId && entity.Status != EntityStatuses.Archived));
    }

    public Task<BusinessEntity> CreateEntityAsync(
        BusinessEntity entity,
        IReadOnlyList<LabelName> labels,
        CancellationToken cancellationToken = default)
    {
        foreach (var labelName in labels)
        {
            var label = GetOrCreateLabel(labelName);
            entity.EntityLabels.Add(new EntityLabel
            {
                Entity = entity,
                EntityId = entity.Id,
                Label = label,
                LabelId = label.Id
            });
        }

        Entities.Add(entity);
        return Task.FromResult(entity);
    }

    public Task<EntityRelationship> CreateEntityRelationshipAsync(
        EntityRelationship relationship,
        CancellationToken cancellationToken = default)
    {
        relationship.SourceEntity = Entities.FirstOrDefault(entity => entity.Id == relationship.SourceEntityId);
        relationship.TargetEntity = Entities.FirstOrDefault(entity => entity.Id == relationship.TargetEntityId);
        EntityRelationships.Add(relationship);
        relationship.SourceEntity?.SourceRelationships.Add(relationship);
        relationship.TargetEntity?.TargetRelationships.Add(relationship);
        return Task.FromResult(relationship);
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

    public Task AddLabelToEntityAsync(
        Guid entityId,
        LabelName label,
        CancellationToken cancellationToken = default)
    {
        var item = Entities.FirstOrDefault(entity => entity.Id == entityId);
        if (item is null)
        {
            return Task.CompletedTask;
        }

        var labelEntity = GetOrCreateLabel(label);
        item.EntityLabels.Add(new EntityLabel
        {
            Entity = item,
            EntityId = item.Id,
            Label = labelEntity,
            LabelId = labelEntity.Id
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
