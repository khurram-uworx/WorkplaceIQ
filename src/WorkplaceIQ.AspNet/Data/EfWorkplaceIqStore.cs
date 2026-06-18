using Microsoft.EntityFrameworkCore;
using WorkplaceIQ.Content;
using WorkplaceIQ.Files;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Metrics;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.AspNet.Data;

public sealed class EfWorkplaceIqStore(WorkplaceIqDbContext dbContext) : IWorkplaceIqStore
{
    public Task<Content.Content?> GetContentByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Contents
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
    }

    public Task<Content.Content?> GetContentByIdAsync(
        Guid contentId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Contents
            .AsNoTracking()
            .Include(c => c.ContentLabels)
                .ThenInclude(cl => cl.Label)
            .Include(c => c.SourceRelationships)
                .ThenInclude(r => r.TargetContent)
            .Include(c => c.TargetRelationships)
                .ThenInclude(r => r.SourceContent)
            .FirstOrDefaultAsync(c => c.Id == contentId, cancellationToken);
    }

    public async Task<IReadOnlyList<Content.Content>> GetChildrenAsync(
        Guid parentId,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Contents
            .AsNoTracking()
            .Include(c => c.ContentLabels)
                .ThenInclude(cl => cl.Label)
            .Where(c => c.ParentId == parentId)
            .Where(c => c.Status != "archived");

        if (!string.IsNullOrWhiteSpace(contentType))
        {
            query = query.Where(c => c.ContentType == contentType);
        }

        return await query
            .OrderBy(c => c.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Content.Content>> GetContentByTypeAsync(
        string contentType,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Contents
            .AsNoTracking()
            .Where(c => c.ContentType == contentType)
            .Where(c => c.Status != "archived")
            .OrderBy(c => c.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<Content.Content> CreateContentAsync(
        Content.Content content,
        CancellationToken cancellationToken = default)
    {
        dbContext.Contents.Add(content);
        await dbContext.SaveChangesAsync(cancellationToken);
        return content;
    }

    public async Task<Content.Content> UpdateContentAsync(
        Content.Content content,
        CancellationToken cancellationToken = default)
    {
        dbContext.Contents.Update(content);
        await dbContext.SaveChangesAsync(cancellationToken);
        return content;
    }

    public async Task DeleteContentAsync(
        Guid contentId,
        CancellationToken cancellationToken = default)
    {
        var content = await dbContext.Contents.FirstOrDefaultAsync(c => c.Id == contentId, cancellationToken);
        if (content is null)
        {
            return;
        }

        content.Status = "archived";
        content.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
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
        var containerType = await dbContext.Contents
            .AsNoTracking()
            .Where(c => c.Id == containerId)
            .Select(c => c.ContentType)
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

        return containerType == ContentTypes.ForumContainer
            ? PostTypes.Thread
            : PostTypes.Post;
    }

    public async Task<IReadOnlyList<FileObject>> GetFilesByContainerAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.FileRecords
            .AsNoTracking()
            .Include(file => file.Content!)
                .ThenInclude(content => content.ContentLabels)
                    .ThenInclude(contentLabel => contentLabel.Label)
            .Include(file => file.Content!)
                .ThenInclude(content => content.Posts)
                    .ThenInclude(post => post.PostLabels)
                        .ThenInclude(postLabel => postLabel.Label)
            .Where(file => file.Content != null)
            .Where(file => file.Content!.ParentId == containerId)
            .Where(file => file.Content!.ContentType == FileContentTypes.File)
            .Where(file => file.Content!.Status != "archived")
            .ToListAsync(cancellationToken);

        return rows
            .Select(file => new FileObject(file.Content!, file))
            .OrderByDescending(file => file.Content.UpdatedAt)
            .ToList();
    }

    public async Task<FileObject?> GetFileByContentIdAsync(
        Guid contentId,
        CancellationToken cancellationToken = default)
    {
        var file = await dbContext.FileRecords
            .AsNoTracking()
            .Include(candidate => candidate.Content!)
                .ThenInclude(content => content.ContentLabels)
                    .ThenInclude(contentLabel => contentLabel.Label)
            .Include(candidate => candidate.Content!)
                .ThenInclude(content => content.Posts)
                    .ThenInclude(post => post.PostLabels)
                        .ThenInclude(postLabel => postLabel.Label)
            .FirstOrDefaultAsync(candidate => candidate.ContentId == contentId, cancellationToken);

        if (file?.Content is null || file.Content.Status == "archived")
        {
            return null;
        }

        return new FileObject(file.Content, file);
    }

    public async Task<FileObject> CreateFileRecordAsync(
        FileRecord fileRecord,
        CancellationToken cancellationToken = default)
    {
        dbContext.FileRecords.Add(fileRecord);
        await dbContext.SaveChangesAsync(cancellationToken);

        var file = await GetFileByContentIdAsync(fileRecord.ContentId, cancellationToken);
        return file ?? throw new InvalidOperationException($"File content '{fileRecord.ContentId}' was not found after creation.");
    }

    public async Task<ContentRelationship> CreateContentRelationshipAsync(
        ContentRelationship relationship,
        CancellationToken cancellationToken = default)
    {
        dbContext.ContentRelationships.Add(relationship);
        await dbContext.SaveChangesAsync(cancellationToken);
        return relationship;
    }

    public async Task AddLabelToContentAsync(
        Guid contentId,
        LabelName label,
        CancellationToken cancellationToken = default)
    {
        var labelEntity = await GetOrCreateLabelAsync(label, cancellationToken);
        var exists = await dbContext.ContentLabels.AnyAsync(
            cl => cl.ContentId == contentId && cl.LabelId == labelEntity.Id,
            cancellationToken);

        if (!exists)
        {
            dbContext.ContentLabels.Add(new ContentLabel
            {
                ContentId = contentId,
                LabelId = labelEntity.Id
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task AddLabelToPostAsync(
        Guid postId,
        LabelName label,
        CancellationToken cancellationToken = default)
    {
        var labelEntity = await GetOrCreateLabelAsync(label, cancellationToken);
        var exists = await dbContext.PostLabels.AnyAsync(
            postLabel => postLabel.PostId == postId && postLabel.LabelId == labelEntity.Id,
            cancellationToken);

        if (!exists)
        {
            dbContext.PostLabels.Add(new PostLabel
            {
                PostId = postId,
                LabelId = labelEntity.Id
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

    public Task<MetricDefinition?> GetMetricDefinitionByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return dbContext.MetricDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Name == name, cancellationToken);
    }
}
