using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceIQ.Labels;

namespace WorkplaceIQ.AspNet.Data.Configurations;

public sealed class ContentItemLabelConfiguration : IEntityTypeConfiguration<ContentItemLabel>
{
    public void Configure(EntityTypeBuilder<ContentItemLabel> entity)
    {
        entity.HasKey(l => new { l.ContentItemId, l.LabelId });

        entity
            .HasOne(l => l.ContentItem)
            .WithMany(ci => ci.Labels)
            .HasForeignKey(l => l.ContentItemId)
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(l => l.Label)
            .WithMany(lbl => lbl.ContentItemLabels)
            .HasForeignKey(l => l.LabelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
