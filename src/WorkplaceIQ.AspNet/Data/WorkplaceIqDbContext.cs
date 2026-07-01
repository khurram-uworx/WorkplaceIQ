using Microsoft.EntityFrameworkCore;
using WorkplaceIQ.AspNet.Data.Configurations;
using WorkplaceIQ.Content;

namespace WorkplaceIQ.AspNet.Data;

public sealed class WorkplaceIqDbContext(DbContextOptions<WorkplaceIqDbContext> options) : DbContext(options)
{
    public DbSet<Content.Content> Contents => Set<Content.Content>();
    public DbSet<DiscussionContent> DiscussionContents => Set<DiscussionContent>();
    public DbSet<FolderContent> FolderContents => Set<FolderContent>();
    public DbSet<FeedContent> FeedContents => Set<FeedContent>();
    public DbSet<GroupContent> GroupContents => Set<GroupContent>();
    public DbSet<ContentItem> ContentItems => Set<ContentItem>();
    public DbSet<ContentFile> ContentFiles => Set<ContentFile>();
    public DbSet<Labels.Label> Labels => Set<Labels.Label>();
    public DbSet<Labels.ContentLabel> ContentLabels => Set<Labels.ContentLabel>();
    public DbSet<Labels.ContentItemLabel> ContentItemLabels => Set<Labels.ContentItemLabel>();
    public DbSet<Content.ContentRelationship> ContentRelationships => Set<Content.ContentRelationship>();
    public DbSet<Content.ClassifiedItem> ClassifiedItems => Set<Content.ClassifiedItem>();
    public DbSet<Metrics.MetricDefinition> MetricDefinitions => Set<Metrics.MetricDefinition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ContentConfiguration());
        modelBuilder.ApplyConfiguration(new DiscussionContentConfiguration());
        modelBuilder.ApplyConfiguration(new FolderContentConfiguration());
        modelBuilder.ApplyConfiguration(new FeedContentConfiguration());
        modelBuilder.ApplyConfiguration(new GroupContentConfiguration());
        modelBuilder.ApplyConfiguration(new ContentItemConfiguration());
        modelBuilder.ApplyConfiguration(new ContentFileConfiguration());
        modelBuilder.ApplyConfiguration(new LabelConfiguration());
        modelBuilder.ApplyConfiguration(new ContentLabelConfiguration());
        modelBuilder.ApplyConfiguration(new ContentItemLabelConfiguration());
        modelBuilder.ApplyConfiguration(new ContentRelationshipConfiguration());
        modelBuilder.ApplyConfiguration(new MetricDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new ClassifiedItemConfiguration());
    }
}
