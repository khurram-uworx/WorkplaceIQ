using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text.Encodings.Web;
using WorkplaceIQ.AspNet.Data;
using WorkplaceIQ.AspNet.Files;
using WorkplaceIQ.AspNet.Rendering;
using WorkplaceIQ.AspNet.TagHelpers;
using WorkplaceIQ.Components;
using WorkplaceIQ.Content;
using WorkplaceIQ.Entities;
using WorkplaceIQ.Feeds;
using WorkplaceIQ.Files;
using WorkplaceIQ.Forums;
using WorkplaceIQ.Metrics;

namespace WorkplaceIQ.AspNet;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkplaceIqAspNet(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        services.AddDbContextFactory<WorkplaceIqDbContext>(configureDbContext);
        services.AddScoped<IWorkplaceIqStore, EfWorkplaceIqStore>();
        services.AddScoped<IComponentService, ComponentService>();
        services.AddScoped<IFeedComponentService, FeedComponentService>();
        services.AddScoped<IForumComponentService, ForumComponentService>();
        services.AddScoped<IFileComponentService, FileComponentService>();
        services.AddScoped<IEntityComponentService, EntityComponentService>();
        services.AddScoped<IFileObjectStorage, S3FileObjectStorage>();
        services.AddScoped<IContainerService, ContainerService>();
        services.AddScoped<IContentItemService, ContentItemService>();
        services.AddScoped<IMetricService, MetricService>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMetricProvider, ContentCountMetricProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMetricProvider>(
            new MetadataAggregationMetricProvider(MetricNames.MetadataSum, values => values.Sum())));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMetricProvider>(
            new MetadataAggregationMetricProvider(MetricNames.MetadataAverage, values => values.Average())));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMetricProvider>(
            new MetadataAggregationMetricProvider(MetricNames.MetadataMin, values => values.Min())));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IMetricProvider>(
            new MetadataAggregationMetricProvider(MetricNames.MetadataMax, values => values.Max())));
        services.AddSingleton(HtmlEncoder.Default);
        services.AddScoped<LabelHtmlRenderer>();
        services.AddScoped<ComponentHtmlRenderer>();
        services.AddTransient<MetricTagHelper>();

        return services;
    }

    public static IServiceCollection AddWorkplaceIqSqliteStorage(
        this IServiceCollection services,
        string connectionString)
        => services.AddWorkplaceIqAspNet(options => options.UseSqlite(connectionString));

    public static IServiceCollection AddWorkplaceIqSqlServerStorage(
        this IServiceCollection services,
        string connectionString)
        => services.AddWorkplaceIqAspNet(options => options.UseSqlServer(connectionString));

    public static IServiceCollection AddWorkplaceIqPgVectorStorage(
        this IServiceCollection services,
        string connectionString)
        => services.AddWorkplaceIqAspNet(options => options.UseNpgsql(connectionString));

    public static IServiceCollection AddWorkplaceIqInMemoryStorage(
        this IServiceCollection services,
        string databaseName = "workplaceiq")
        => services.AddWorkplaceIqAspNet(options => options.UseInMemoryDatabase(databaseName));
}
