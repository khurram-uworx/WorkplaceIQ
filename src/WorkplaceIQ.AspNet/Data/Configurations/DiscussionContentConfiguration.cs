using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceIQ.Content;

namespace WorkplaceIQ.AspNet.Data.Configurations;

public sealed class DiscussionContentConfiguration : IEntityTypeConfiguration<DiscussionContent>
{
    public void Configure(EntityTypeBuilder<DiscussionContent> entity)
    {
        entity.ToTable("DiscussionContents");

        entity.HasIndex(c => c.Name).IsUnique();
        entity.HasIndex(c => c.Status);
    }
}
