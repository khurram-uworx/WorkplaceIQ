using Microsoft.EntityFrameworkCore;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Content;
using WorkplaceIQ.Entities;
using WorkplaceIQ.Files;
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

    public DbSet<FileRecord> FileRecords => Set<FileRecord>();

    public DbSet<BusinessEntity> Entities => Set<BusinessEntity>();

    public DbSet<EntityLabel> EntityLabels => Set<EntityLabel>();

    public DbSet<EntityRelationship> EntityRelationships => Set<EntityRelationship>();

    public DbSet<EntityContentLink> EntityContentLinks => Set<EntityContentLink>();

    public DbSet<EntityPostLink> EntityPostLinks => Set<EntityPostLink>();

    public DbSet<EntityFileLink> EntityFileLinks => Set<EntityFileLink>();

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

        modelBuilder.Entity<FileRecord>(entity =>
        {
            entity.HasIndex(file => file.ContentItemId).IsUnique();
            entity.HasIndex(file => file.ObjectKey);

            entity
                .HasOne(file => file.ContentItem)
                .WithOne()
                .HasForeignKey<FileRecord>(file => file.ContentItemId);
        });

        modelBuilder.Entity<BusinessEntity>(entity =>
        {
            entity.HasIndex(e => e.ContainerId);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.Status);

            entity
                .HasOne(e => e.Container)
                .WithMany()
                .HasForeignKey(e => e.ContainerId);
        });

        modelBuilder.Entity<EntityLabel>(entity =>
        {
            entity.HasKey(label => new { label.EntityId, label.LabelId });

            entity
                .HasOne(label => label.Entity)
                .WithMany(e => e.EntityLabels)
                .HasForeignKey(label => label.EntityId);

            entity
                .HasOne(label => label.Label)
                .WithMany()
                .HasForeignKey(label => label.LabelId);
        });

        modelBuilder.Entity<EntityRelationship>(entity =>
        {
            entity.HasIndex(relationship => relationship.SourceEntityId);
            entity.HasIndex(relationship => relationship.TargetEntityId);
            entity.HasIndex(relationship => relationship.RelationshipType);

            entity
                .HasOne(relationship => relationship.SourceEntity)
                .WithMany(e => e.SourceRelationships)
                .HasForeignKey(relationship => relationship.SourceEntityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(relationship => relationship.TargetEntity)
                .WithMany(e => e.TargetRelationships)
                .HasForeignKey(relationship => relationship.TargetEntityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EntityContentLink>(entity =>
        {
            entity.HasKey(link => new { link.EntityId, link.ContentItemId });

            entity
                .HasOne(link => link.Entity)
                .WithMany(e => e.ContentLinks)
                .HasForeignKey(link => link.EntityId);

            entity
                .HasOne(link => link.ContentItem)
                .WithMany()
                .HasForeignKey(link => link.ContentItemId);
        });

        modelBuilder.Entity<EntityPostLink>(entity =>
        {
            entity.HasKey(link => new { link.EntityId, link.PostId });

            entity
                .HasOne(link => link.Entity)
                .WithMany(e => e.PostLinks)
                .HasForeignKey(link => link.EntityId);

            entity
                .HasOne(link => link.Post)
                .WithMany()
                .HasForeignKey(link => link.PostId);
        });

        modelBuilder.Entity<EntityFileLink>(entity =>
        {
            entity.HasKey(link => new { link.EntityId, link.FileRecordId });

            entity
                .HasOne(link => link.Entity)
                .WithMany(e => e.FileLinks)
                .HasForeignKey(link => link.EntityId);

            entity
                .HasOne(link => link.FileRecord)
                .WithMany()
                .HasForeignKey(link => link.FileRecordId);
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
