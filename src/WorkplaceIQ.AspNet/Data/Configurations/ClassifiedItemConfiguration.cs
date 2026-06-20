using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceIQ.Content;

namespace WorkplaceIQ.AspNet.Data.Configurations;

public sealed class ClassifiedItemConfiguration : IEntityTypeConfiguration<ClassifiedItem>
{
    public void Configure(EntityTypeBuilder<ClassifiedItem> entity)
    {
        entity.HasIndex(item => item.LabelId);
        entity.HasIndex(item => item.ContentId);
        entity.HasIndex(item => item.ClassificationSource);
        entity.HasIndex(item => item.ClassifiedAt);

        entity
            .HasOne(item => item.RssItem)
            .WithMany()
            .HasForeignKey(item => item.ContentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(item => item.SignalLabel)
            .WithMany()
            .HasForeignKey(item => item.LabelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
