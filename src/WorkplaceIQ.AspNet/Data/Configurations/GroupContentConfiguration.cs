using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceIQ.Content;

namespace WorkplaceIQ.AspNet.Data.Configurations;

public sealed class GroupContentConfiguration : IEntityTypeConfiguration<GroupContent>
{
    public void Configure(EntityTypeBuilder<GroupContent> entity)
    {
        entity.ToTable("GroupContents");

        entity.HasIndex(c => c.Name).IsUnique();
        entity.HasIndex(c => c.Status);
    }
}
