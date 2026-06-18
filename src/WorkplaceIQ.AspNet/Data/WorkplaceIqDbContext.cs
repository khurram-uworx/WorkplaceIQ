using Microsoft.EntityFrameworkCore;
using WorkplaceIQ.Content;
using WorkplaceIQ.Files;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Metrics;
using WorkplaceIQ.Posts;
using ContentLabel = WorkplaceIQ.Labels.ContentLabel;

namespace WorkplaceIQ.AspNet.Data;

public sealed class WorkplaceIqDbContext(DbContextOptions<WorkplaceIqDbContext> options) : DbContext(options)
{
    public DbSet<Content.Content> Contents => Set<Content.Content>();

    public DbSet<Post> Posts => Set<Post>();

    public DbSet<Label> Labels => Set<Label>();

    public DbSet<PostLabel> PostLabels => Set<PostLabel>();

    public DbSet<ContentLabel> ContentLabels => Set<ContentLabel>();

    public DbSet<ContentRelationship> ContentRelationships => Set<ContentRelationship>();

    public DbSet<FileRecord> FileRecords => Set<FileRecord>();

    public DbSet<MetricDefinition> MetricDefinitions => Set<MetricDefinition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Content.Content>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
            entity.HasIndex(c => c.ContentType);
            entity.HasIndex(c => c.ParentId);
            entity.HasIndex(c => c.Status);

            entity
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasMany(c => c.SourceRelationships)
                .WithOne(r => r.SourceContent)
                .HasForeignKey(r => r.SourceContentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasMany(c => c.TargetRelationships)
                .WithOne(r => r.TargetContent)
                .HasForeignKey(r => r.TargetContentId)
                .OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<ContentRelationship>(entity =>
        {
            entity.HasIndex(r => r.SourceContentId);
            entity.HasIndex(r => r.TargetContentId);
            entity.HasIndex(r => r.RelationshipType);

            entity
                .HasOne(r => r.SourceContent)
                .WithMany(c => c.SourceRelationships)
                .HasForeignKey(r => r.SourceContentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(r => r.TargetContent)
                .WithMany(c => c.TargetRelationships)
                .HasForeignKey(r => r.TargetContentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FileRecord>(entity =>
        {
            entity.HasIndex(file => file.ContentId).IsUnique();
            entity.HasIndex(file => file.ObjectKey);

            entity
                .HasOne(file => file.Content)
                .WithOne()
                .HasForeignKey<FileRecord>(file => file.ContentId);
        });

        modelBuilder.Entity<ContentLabel>(entity =>
        {
            entity.HasKey(cl => new { cl.ContentId, cl.LabelId });

            entity
                .HasOne(cl => cl.Content)
                .WithMany(c => c.ContentLabels)
                .HasForeignKey(cl => cl.ContentId);

            entity
                .HasOne(cl => cl.Label)
                .WithMany()
                .HasForeignKey(cl => cl.LabelId);
        });

        modelBuilder.Entity<MetricDefinition>(entity =>
        {
            entity.HasIndex(m => m.Name).IsUnique();
        });
    }
}
