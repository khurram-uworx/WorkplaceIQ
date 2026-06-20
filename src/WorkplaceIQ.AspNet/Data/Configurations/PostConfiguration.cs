using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.AspNet.Data.Configurations;

public sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> entity)
    {
        entity.HasIndex(post => post.ContainerId);
        entity.HasIndex(post => post.ContentId);
        entity.HasIndex(post => post.PostType);

        entity
            .HasOne(post => post.Content)
            .WithMany(c => c.Posts)
            .HasForeignKey(post => post.ContentId)
            .IsRequired(false);
    }
}
