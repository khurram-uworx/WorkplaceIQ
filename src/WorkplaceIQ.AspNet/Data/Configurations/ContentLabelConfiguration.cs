using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceIQ.Labels;

namespace WorkplaceIQ.AspNet.Data.Configurations;

public sealed class ContentLabelConfiguration : IEntityTypeConfiguration<ContentLabel>
{
    public void Configure(EntityTypeBuilder<ContentLabel> entity)
    {
        entity.HasKey(cl => new { cl.ContentId, cl.LabelId });

        entity
            .HasOne(cl => cl.Content)
            .WithMany(c => c.ContentLabels)
            .HasForeignKey(cl => cl.ContentId);

        entity
            .HasOne(cl => cl.Label)
            .WithMany()
            .HasForeignKey(cl => cl.LabelId);
    }
}
