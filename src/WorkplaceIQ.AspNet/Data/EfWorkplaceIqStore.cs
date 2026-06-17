using Microsoft.EntityFrameworkCore;
using WorkplaceIQ;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Feeds;

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

    public async Task<IReadOnlyList<FeedPost>> GetFeedPostsAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.FeedPosts
            .AsNoTracking()
            .Where(post => post.ContainerId == containerId)
            .OrderByDescending(post => post.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
