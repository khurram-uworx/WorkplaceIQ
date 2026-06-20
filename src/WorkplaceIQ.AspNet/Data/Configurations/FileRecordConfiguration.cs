using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceIQ.Files;

namespace WorkplaceIQ.AspNet.Data.Configurations;

public sealed class FileRecordConfiguration : IEntityTypeConfiguration<FileRecord>
{
    public void Configure(EntityTypeBuilder<FileRecord> entity)
    {
        entity.HasIndex(file => file.ContentId).IsUnique();
        entity.HasIndex(file => file.ObjectKey);

        entity
            .HasOne(file => file.Content)
            .WithOne()
            .HasForeignKey<FileRecord>(file => file.ContentId);
    }
}
