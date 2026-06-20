using Microsoft.EntityFrameworkCore;
using WorkplaceIQ.AspNet.Data.Configurations;

namespace WorkplaceIQ.AspNet.Data;

public sealed class WorkplaceIqDbContext(DbContextOptions<WorkplaceIqDbContext> options) : DbContext(options)
{
    public DbSet<Content.Content> Contents => Set<Content.Content>();

    public DbSet<Posts.Post> Posts => Set<Posts.Post>();

    public DbSet<Labels.Label> Labels => Set<Labels.Label>();

    public DbSet<Labels.PostLabel> PostLabels => Set<Labels.PostLabel>();

    public DbSet<Labels.ContentLabel> ContentLabels => Set<Labels.ContentLabel>();

    public DbSet<Content.ContentRelationship> ContentRelationships => Set<Content.ContentRelationship>();

    public DbSet<WorkplaceIQ.Files.FileRecord> FileRecords => Set<WorkplaceIQ.Files.FileRecord>();

    public DbSet<Metrics.MetricDefinition> MetricDefinitions => Set<Metrics.MetricDefinition>();

    public DbSet<Content.ClassifiedItem> ClassifiedItems => Set<Content.ClassifiedItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ContentConfiguration());
        modelBuilder.ApplyConfiguration(new PostConfiguration());
        modelBuilder.ApplyConfiguration(new LabelConfiguration());
        modelBuilder.ApplyConfiguration(new PostLabelConfiguration());
        modelBuilder.ApplyConfiguration(new ContentLabelConfiguration());
        modelBuilder.ApplyConfiguration(new ContentRelationshipConfiguration());
        modelBuilder.ApplyConfiguration(new FileRecordConfiguration());
        modelBuilder.ApplyConfiguration(new MetricDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new ClassifiedItemConfiguration());
    }
}
