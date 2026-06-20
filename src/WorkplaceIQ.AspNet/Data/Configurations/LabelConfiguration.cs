using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceIQ.Labels;

namespace WorkplaceIQ.AspNet.Data.Configurations;

public sealed class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> entity)
    {
        entity.HasIndex(label => label.NormalizedName).IsUnique();
        entity.HasIndex(label => label.Slug);
    }
}
