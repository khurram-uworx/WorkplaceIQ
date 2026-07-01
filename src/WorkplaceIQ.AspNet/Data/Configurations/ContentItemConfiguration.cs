using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceIQ.Content;

namespace WorkplaceIQ.AspNet.Data.Configurations;

public sealed class ContentItemConfiguration : IEntityTypeConfiguration<ContentItem>
{
    public void Configure(EntityTypeBuilder<ContentItem> entity)
    {
        entity.ToTable("ContentItems");

        entity.HasKey(ci => ci.Id);

        entity.HasIndex(ci => ci.ContainerId);
        entity.HasIndex(ci => ci.Discriminator);
        entity.HasIndex(ci => ci.Status);

        entity
            .HasOne(ci => ci.Container)
            .WithMany(c => c.Items)
            .HasForeignKey(ci => ci.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasMany(ci => ci.Labels)
            .WithOne(l => l.ContentItem)
            .HasForeignKey(l => l.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
