using Microsoft.EntityFrameworkCore;
using WorkplaceIQ;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Labels;
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
        CancellationToken cancellationToken = default)
    {
        var post = new Post
        {
            ContainerId = containerId,
            Title = title,
            Body = body,
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
}
