using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceIQ.Content;

namespace WorkplaceIQ.AspNet.Data.Configurations;

public sealed class ContentConfiguration : IEntityTypeConfiguration<Content.Content>
{
    public void Configure(EntityTypeBuilder<Content.Content> entity)
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
    }
}
