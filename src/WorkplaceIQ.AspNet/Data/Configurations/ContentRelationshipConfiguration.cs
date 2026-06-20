using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceIQ.Content;

namespace WorkplaceIQ.AspNet.Data.Configurations;

public sealed class ContentRelationshipConfiguration : IEntityTypeConfiguration<ContentRelationship>
{
    public void Configure(EntityTypeBuilder<ContentRelationship> entity)
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
    }
}
