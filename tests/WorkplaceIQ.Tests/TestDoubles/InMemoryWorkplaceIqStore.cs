using WorkplaceIQ.Content;
using WorkplaceIQ.Files;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Metrics;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.Tests.TestDoubles;

internal sealed class InMemoryWorkplaceIqStore : IWorkplaceIqStore
{
    public List<Content.Content> Contents { get; } = [];

    public List<Post> Posts { get; } = [];

    public List<Label> Labels { get; } = [];

    public List<FileRecord> FileRecords { get; } = [];

    public List<ContentRelationship> ContentRelationships { get; } = [];

    public List<MetricDefinition> MetricDefinitions { get; } = [];

    public Task<Content.Content?> GetContentByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Contents.FirstOrDefault(c => c.Name == name));
    }

    public Task<Content.Content?> GetContentByIdAsync(
        Guid contentId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Contents.FirstOrDefault(c => c.Id == contentId));
    }

    public Task<IReadOnlyList<Content.Content>> GetChildrenAsync(
        Guid parentId,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        var query = Contents
            .Where(c => c.ParentId == parentId)
            .Where(c => c.Status != "archived");

        if (!string.IsNullOrWhiteSpace(contentType))
        {
            query = query.Where(c => c.ContentType == contentType);
        }

        return Task.FromResult<IReadOnlyList<Content.Content>>(
            query.OrderBy(c => c.Title).ToList());
    }

    public Task<IReadOnlyList<Content.Content>> GetContentByTypeAsync(
        string contentType,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Content.Content>>(
            Contents
                .Where(c => c.ContentType == contentType)
                .Where(c => c.Status != "archived")
                .OrderBy(c => c.Title)
                .ToList());
    }

    public Task<Content.Content> CreateContentAsync(
        Content.Content content,
        CancellationToken cancellationToken = default)
    {
        Contents.Add(content);
        return Task.FromResult(content);
    }

    public Task<Content.Content> UpdateContentAsync(
        Content.Content content,
        CancellationToken cancellationToken = default)
    {
        var index = Contents.FindIndex(c => c.Id == content.Id);
        if (index >= 0)
        {
            Contents[index] = content;
        }
        return Task.FromResult(content);
    }

    public Task DeleteContentAsync(
        Guid contentId,
        CancellationToken cancellationToken = default)
    {
        var content = Contents.FirstOrDefault(c => c.Id == contentId);
        if (content is not null)
        {
            content.Status = "archived";
        }
        return Task.CompletedTask;
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
        var containerType = Contents.FirstOrDefault(c => c.Id == containerId)?.ContentType;
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

        return containerType == ContentTypes.ForumContainer
            ? PostTypes.Thread
            : PostTypes.Post;
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
                    Content = Contents.FirstOrDefault(content => content.Id == file.ContentId)
                })
                .Where(row => row.Content is not null)
                .Where(row => row.Content!.ParentId == containerId)
                .Where(row => row.Content!.ContentType == FileContentTypes.File)
                .Where(row => row.Content!.Status != "archived")
                .OrderByDescending(row => row.Content!.UpdatedAt)
                .Select(row => new FileObject(row.Content!, row.FileRecord))
                .ToList());
    }

    public Task<FileObject?> GetFileByContentIdAsync(
        Guid contentId,
        CancellationToken cancellationToken = default)
    {
        var file = FileRecords.FirstOrDefault(candidate => candidate.ContentId == contentId);
        var content = Contents.FirstOrDefault(candidate => candidate.Id == contentId);
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
        var content = Contents.First(c => c.Id == fileRecord.ContentId);
        return Task.FromResult(new FileObject(content, fileRecord));
    }

    public Task<ContentRelationship> CreateContentRelationshipAsync(
        ContentRelationship relationship,
        CancellationToken cancellationToken = default)
    {
        relationship.SourceContent = Contents.FirstOrDefault(c => c.Id == relationship.SourceContentId);
        relationship.TargetContent = Contents.FirstOrDefault(c => c.Id == relationship.TargetContentId);
        ContentRelationships.Add(relationship);
        relationship.SourceContent?.SourceRelationships.Add(relationship);
        relationship.TargetContent?.TargetRelationships.Add(relationship);
        return Task.FromResult(relationship);
    }

    public Task AddLabelToContentAsync(
        Guid contentId,
        LabelName label,
        CancellationToken cancellationToken = default)
    {
        var content = Contents.FirstOrDefault(item => item.Id == contentId);
        if (content is null)
        {
            return Task.CompletedTask;
        }

        var labelEntity = GetOrCreateLabel(label);
        content.ContentLabels.Add(new ContentLabel
        {
            Content = content,
            ContentId = content.Id,
            Label = labelEntity,
            LabelId = labelEntity.Id
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

        var labelEntity = GetOrCreateLabel(label);
        post.PostLabels.Add(new PostLabel
        {
            Post = post,
            PostId = post.Id,
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

    public Task<MetricDefinition?> GetMetricDefinitionByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(MetricDefinitions.FirstOrDefault(m => m.Name == name));
    }
}
