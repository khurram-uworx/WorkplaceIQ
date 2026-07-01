using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceIQ.Content;

namespace WorkplaceIQ.AspNet.Data.Configurations;

public sealed class FolderContentConfiguration : IEntityTypeConfiguration<FolderContent>
{
    public void Configure(EntityTypeBuilder<FolderContent> entity)
    {
        entity.ToTable("FolderContents");

        entity.HasIndex(c => c.Name).IsUnique();
        entity.HasIndex(c => c.Status);
    }
}
