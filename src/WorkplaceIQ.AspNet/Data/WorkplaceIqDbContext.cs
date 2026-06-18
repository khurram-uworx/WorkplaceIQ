using Microsoft.EntityFrameworkCore;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Content;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Metrics;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.AspNet.Data;

public sealed class WorkplaceIqDbContext(DbContextOptions<WorkplaceIqDbContext> options) : DbContext(options)
{
    public DbSet<Container> Containers => Set<Container>();

    public DbSet<Post> Posts => Set<Post>();

    public DbSet<Label> Labels => Set<Label>();

    public DbSet<PostLabel> PostLabels => Set<PostLabel>();

    public DbSet<ContentItem> ContentItems => Set<ContentItem>();

    public DbSet<ContentLabel> ContentLabels => Set<ContentLabel>();

    public DbSet<MetricDefinition> MetricDefinitions => Set<MetricDefinition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Container>(entity =>
        {
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasIndex(post => post.ContainerId);
            entity.HasIndex(post => post.ContentId);
            entity.HasIndex(post => post.PostType);

            entity
                .HasOne(post => post.Content)
                .WithMany(c => c.Posts)
                .HasForeignKey(post => post.ContentId)
                .IsRequired(false);
        });

        modelBuilder.Entity<Label>(entity =>
        {
            entity.HasIndex(label => label.NormalizedName).IsUnique();
            entity.HasIndex(label => label.Slug);
        });

        modelBuilder.Entity<PostLabel>(entity =>
        {
            entity.HasKey(postLabel => new { postLabel.PostId, postLabel.LabelId });

            entity
                .HasOne(postLabel => postLabel.Post)
                .WithMany(post => post.PostLabels)
                .HasForeignKey(postLabel => postLabel.PostId);

            entity
                .HasOne(postLabel => postLabel.Label)
                .WithMany(label => label.PostLabels)
                .HasForeignKey(postLabel => postLabel.LabelId);
        });

        modelBuilder.Entity<ContentItem>(entity =>
        {
            entity.HasIndex(c => c.ContainerId);
            entity.HasIndex(c => c.ContentType);

            entity
                .HasOne(c => c.Container)
                .WithMany()
                .HasForeignKey(c => c.ContainerId);
        });

        modelBuilder.Entity<MetricDefinition>(entity =>
        {
            entity.HasIndex(m => m.Name).IsUnique();
        });

        modelBuilder.Entity<ContentLabel>(entity =>
        {
            entity.HasKey(cl => new { cl.ContentItemId, cl.LabelId });

            entity
                .HasOne(cl => cl.ContentItem)
                .WithMany(c => c.ContentLabels)
                .HasForeignKey(cl => cl.ContentItemId);

            entity
                .HasOne(cl => cl.Label)
                .WithMany()
                .HasForeignKey(cl => cl.LabelId);
        });
    }
}
