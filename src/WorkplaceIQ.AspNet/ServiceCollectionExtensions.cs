using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WorkplaceIQ.AspNet.Data;
using WorkplaceIQ.AspNet.Rendering;
using WorkplaceIQ.AspNet.TagHelpers;
using WorkplaceIQ.Components;
using WorkplaceIQ.Content;
using WorkplaceIQ.Feeds;
using WorkplaceIQ.Forums;
using WorkplaceIQ.Metrics;

namespace WorkplaceIQ.AspNet;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkplaceIqAspNet(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        services.AddDbContext<WorkplaceIqDbContext>(configureDbContext);
        services.AddScoped<IWorkplaceIqStore, EfWorkplaceIqStore>();
        services.AddScoped<IComponentService, ComponentService>();
        services.AddScoped<IFeedComponentService, FeedComponentService>();
        services.AddScoped<IForumComponentService, ForumComponentService>();
        services.AddScoped<IContentService, ContentService>();
        services.AddScoped<IMetricService, MetricService>();
        services.AddSingleton(HtmlEncoder.Default);
        services.AddScoped<LabelHtmlRenderer>();
        services.AddScoped<ComponentHtmlRenderer>();
        services.AddTransient<MetricTagHelper>();

        return services;
    }
}
