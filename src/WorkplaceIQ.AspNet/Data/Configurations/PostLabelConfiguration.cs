using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceIQ.Labels;

namespace WorkplaceIQ.AspNet.Data.Configurations;

public sealed class PostLabelConfiguration : IEntityTypeConfiguration<PostLabel>
{
    public void Configure(EntityTypeBuilder<PostLabel> entity)
    {
        entity.HasKey(postLabel => new { postLabel.PostId, postLabel.LabelId });

        entity
            .HasOne(postLabel => postLabel.Post)
            .WithMany(post => post.PostLabels)
            .HasForeignKey(postLabel => postLabel.PostId);

        entity
            .HasOne(postLabel => postLabel.Label)
            .WithMany(label => label.PostLabels)
            .HasForeignKey(postLabel => postLabel.LabelId);
    }
}
