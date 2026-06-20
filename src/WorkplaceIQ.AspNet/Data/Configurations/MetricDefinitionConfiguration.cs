using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkplaceIQ.Metrics;

namespace WorkplaceIQ.AspNet.Data.Configurations;

public sealed class MetricDefinitionConfiguration : IEntityTypeConfiguration<MetricDefinition>
{
    public void Configure(EntityTypeBuilder<MetricDefinition> entity)
    {
        entity.HasIndex(m => m.Name).IsUnique();
    }
}
