using Microsoft.EntityFrameworkCore;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Feeds;

namespace WorkplaceIQ.AspNet.Data;

public sealed class WorkplaceIqDbContext(DbContextOptions<WorkplaceIqDbContext> options) : DbContext(options)
{
    public DbSet<Container> Containers => Set<Container>();

    public DbSet<FeedPost> FeedPosts => Set<FeedPost>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Container>(entity =>
        {
        });

        modelBuilder.Entity<FeedPost>(entity =>
        {
            entity.HasIndex(post => post.ContainerId);
        });
    }
}
