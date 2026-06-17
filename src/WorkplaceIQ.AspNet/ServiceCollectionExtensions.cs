using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkplaceIQ.AspNet.Data;
using WorkplaceIQ.AspNet.Rendering;
using WorkplaceIQ.Components;
using WorkplaceIQ.Feeds;
using WorkplaceIQ.Forums;

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
        services.AddSingleton(HtmlEncoder.Default);
        services.AddScoped<LabelHtmlRenderer>();
        services.AddScoped<ComponentHtmlRenderer>();

        return services;
    }
}
