using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WorkplaceIQ.AspNet.Data.Configurations;

public sealed class ContentConfiguration : IEntityTypeConfiguration<Content.Content>
{
    public void Configure(EntityTypeBuilder<Content.Content> entity)
    {
        entity.ToTable("Content");
        entity.UseTptMappingStrategy();

        entity.HasKey(c => c.Id);

        entity
            .HasMany(c => c.ContentLabels)
            .WithOne(cl => cl.Content)
            .HasForeignKey(cl => cl.ContentId)
            .OnDelete(DeleteBehavior.Cascade);

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
