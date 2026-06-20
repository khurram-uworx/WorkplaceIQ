using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using WorkplaceIQ.Content;
using WorkplaceIQ.Files;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Metrics;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.AspNet.Data;

public sealed class EfWorkplaceIqStore(IDbContextFactory<WorkplaceIqDbContext> dbContextFactory) : IWorkplaceIqStore
{
    public async Task<Content.Content?> GetContentByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Contents
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
    }

    public async Task<Content.Content?> GetContentByIdAsync(
        Guid contentId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Contents
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
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Contents
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
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Contents
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
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.Contents.Add(content);
        await db.SaveChangesAsync(cancellationToken);
        return content;
    }

    public async Task<Content.Content> UpdateContentAsync(
        Content.Content content,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.Contents.Update(content);
        await db.SaveChangesAsync(cancellationToken);
        return content;
    }

    public async Task DeleteContentAsync(
        Guid contentId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var content = await db.Contents.FirstOrDefaultAsync(c => c.Id == contentId, cancellationToken);
        if (content is null)
        {
            return;
        }

        content.Status = "archived";
        content.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Post>> GetPostsAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var posts = await db.Posts
            .AsNoTracking()
            .Include(post => post.PostLabels)
                .ThenInclude(postLabel => postLabel.Label)
            .Where(post => post.ContainerId == containerId)
            .ToListAsync(cancellationToken);

        return posts
            .OrderByDescending(post => post.CreatedAt)
            .ToList();
    }

    public async Task<Post?> GetPostByIdAsync(
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Posts
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
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var containerType = await db.Contents
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

        db.Posts.Add(post);

        foreach (var labelName in labels)
        {
            var label = await db.Labels.FirstOrDefaultAsync(
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
                db.Labels.Add(label);
            }

            post.PostLabels.Add(new PostLabel
            {
                Post = post,
                Label = label
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        return post;
    }

    public async Task<Post> UpdatePostAsync(
        Post post,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.Posts.Update(post);
        await db.SaveChangesAsync(cancellationToken);
        return post;
    }

    public async Task DeletePostAsync(
        Guid postId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var post = await db.Posts.FirstOrDefaultAsync(candidate => candidate.Id == postId, cancellationToken);
        if (post is null)
        {
            return;
        }

        db.Posts.Remove(post);
        await db.SaveChangesAsync(cancellationToken);
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
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.FileRecords
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
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var file = await db.FileRecords
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
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.FileRecords.Add(fileRecord);
        await db.SaveChangesAsync(cancellationToken);
        db.FileRecords.Remove(fileRecord);

        var file = await GetFileByContentIdAsync(fileRecord.ContentId, cancellationToken);
        return file ?? throw new InvalidOperationException($"File content '{fileRecord.ContentId}' was not found after creation.");
    }

    public async Task<ContentRelationship> CreateContentRelationshipAsync(
        ContentRelationship relationship,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.ContentRelationships.Add(relationship);
        await db.SaveChangesAsync(cancellationToken);
        return relationship;
    }

    public async Task AddLabelToContentAsync(
        Guid contentId,
        LabelName label,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var labelEntity = await GetOrCreateLabelAsync(db, label, cancellationToken);
        var exists = await db.ContentLabels.AnyAsync(
            cl => cl.ContentId == contentId && cl.LabelId == labelEntity.Id,
            cancellationToken);

        if (!exists)
        {
            db.ContentLabels.Add(new ContentLabel
            {
                ContentId = contentId,
                LabelId = labelEntity.Id
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task AddLabelToPostAsync(
        Guid postId,
        LabelName label,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var labelEntity = await GetOrCreateLabelAsync(db, label, cancellationToken);
        var exists = await db.PostLabels.AnyAsync(
            postLabel => postLabel.PostId == postId && postLabel.LabelId == labelEntity.Id,
            cancellationToken);

        if (!exists)
        {
            db.PostLabels.Add(new PostLabel
            {
                PostId = postId,
                LabelId = labelEntity.Id
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<Label> GetOrCreateLabelAsync(
        WorkplaceIqDbContext db,
        LabelName labelName,
        CancellationToken cancellationToken)
    {
        var label = await db.Labels.FirstOrDefaultAsync(
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
        db.Labels.Add(label);
        await db.SaveChangesAsync(cancellationToken);
        return label;
    }

    // ----- Label queries -----

    public async Task<Label?> GetLabelByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Labels
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.NormalizedName == name.ToLowerInvariant(), cancellationToken);
    }

    public async Task<Label> CreateLabelAsync(
        Label label,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.Labels.Add(label);
        await db.SaveChangesAsync(cancellationToken);
        return label;
    }

    // ----- Classification queries -----

    /// <summary>
    /// Base query for read-only classified item access. Encapsulates the
    /// AsNoTrackingWithIdentityResolution + Include boilerplate shared by all
    /// classified item queries. Not used for write operations (Upsert, Update)
    /// which require change tracking.
    /// </summary>
    private IQueryable<ClassifiedItem> ClassifiedItemQuery(WorkplaceIqDbContext db)
        => db.ClassifiedItems
            .AsNoTrackingWithIdentityResolution()
            .Include(item => item.RssItem)
            .Include(item => item.SignalLabel);

    public async Task<ClassifiedItem?> GetClassifiedItemByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ClassifiedItemQuery(db)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<ClassifiedItem?> GetClassifiedByContentIdAsync(
        Guid contentId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ClassifiedItemQuery(db)
            .FirstOrDefaultAsync(item => item.ContentId == contentId, cancellationToken);
    }

    public async Task<IReadOnlyList<ClassifiedItem>> GetClassifiedItemsByLabelAsync(
        Guid labelId,
        int offset = 0,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ClassifiedItemQuery(db)
            .Where(item => item.LabelId == labelId)
            .OrderByDescending(item => item.ClassifiedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClassifiedItem>> GetRecentClassifiedItemsAsync(
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ClassifiedItemQuery(db)
            .Where(item => !item.IsNoise)
            .OrderByDescending(item => item.ClassifiedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Upserts a classification by ContentId. Ensures the one-classification-per-content invariant.
    /// When ADR 02 refactors Content into Container/ContentItem, this method must be updated to
    /// match the new entity hierarchy while preserving the one-per-content invariant.
    /// </summary>
    public async Task<ClassifiedItem> UpsertClassifiedItemAsync(
        ClassifiedItem item,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Find existing classification for this content. If found, update in-place
        // preserving the original Id (stable reference) but overwriting all other fields.
        // This is the "last classification wins" behavior.
        var existing = await db.ClassifiedItems
            .FirstOrDefaultAsync(ci => ci.ContentId == item.ContentId, cancellationToken);

        if (existing is not null)
        {
            existing.LabelId = item.LabelId;
            existing.Reasoning = item.Reasoning;
            existing.IsNoise = item.IsNoise;
            existing.AttemptCount = item.AttemptCount;
            existing.HallucinatedSignal = item.HallucinatedSignal;
            existing.Embedding = item.Embedding;
            existing.ClassificationSource = item.ClassificationSource;
            existing.ClassifiedAt = item.ClassifiedAt;
            await db.SaveChangesAsync(cancellationToken);
            return existing;
        }

        db.ClassifiedItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<ClassifiedItem> UpdateClassifiedItemAsync(
        ClassifiedItem item,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.ClassifiedItems.Update(item);
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<Dictionary<Guid, int>> GetSignalCountsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.ClassifiedItems
            .GroupBy(item => item.LabelId)
            .Select(group => new { LabelId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(g => g.LabelId, g => g.Count, cancellationToken);
    }

    public async Task DeleteClassifiedItemAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.ClassifiedItems
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (item is not null)
        {
            db.ClassifiedItems.Remove(item);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async IAsyncEnumerable<Content.Content> GetUnclassifiedContentsAsync(
        int limit,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var items = await db.Contents
            .AsNoTracking()
            .Include(c => c.ContentLabels)
                .ThenInclude(cl => cl.Label)
            .Where(c => c.Status != "archived")
            .Where(c => c.RetryCount < 5)
            .Where(c => !db.ClassifiedItems.Any(ci => ci.ContentId == c.Id))
            .OrderByDescending(c => c.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            yield return item;
        }
    }

    public async Task<MetricDefinition?> GetMetricDefinitionByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.MetricDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Name == name, cancellationToken);
    }
}
