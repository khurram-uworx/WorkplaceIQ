using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceIQ.Content;

namespace WorkplaceIQ.AspNet.Data.Configurations;

public sealed class FeedContentConfiguration : IEntityTypeConfiguration<FeedContent>
{
    public void Configure(EntityTypeBuilder<FeedContent> entity)
    {
        entity.ToTable("FeedContents");

        entity.HasIndex(c => c.Name).IsUnique();
        entity.HasIndex(c => c.Status);
    }
}
