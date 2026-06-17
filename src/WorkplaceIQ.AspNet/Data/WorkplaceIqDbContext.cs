using Microsoft.EntityFrameworkCore;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.AspNet.Data;

public sealed class WorkplaceIqDbContext(DbContextOptions<WorkplaceIqDbContext> options) : DbContext(options)
{
    public DbSet<Container> Containers => Set<Container>();

    public DbSet<Post> Posts => Set<Post>();

    public DbSet<Label> Labels => Set<Label>();

    public DbSet<PostLabel> PostLabels => Set<PostLabel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Container>(entity =>
        {
        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasIndex(post => post.ContainerId);
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
    }
}
