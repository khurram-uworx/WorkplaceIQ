using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceIQ.Content;

namespace WorkplaceIQ.AspNet.Data.Configurations;

public sealed class ContentFileConfiguration : IEntityTypeConfiguration<ContentFile>
{
    public void Configure(EntityTypeBuilder<ContentFile> entity)
    {
        entity.ToTable("ContentFiles");

        entity.HasKey(f => f.Id);

        entity.HasIndex(f => f.ObjectKey);

        entity
            .HasOne(f => f.ContentItem)
            .WithOne()
            .HasForeignKey<ContentFile>(f => f.Id)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
