using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkplaceIQ.AspNet.Data;
using WorkplaceIQ.Feeds;

namespace WorkplaceIQ.AspNet;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkplaceIqAspNet(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        services.AddDbContext<WorkplaceIqDbContext>(configureDbContext);
        services.AddScoped<IWorkplaceIqStore, EfWorkplaceIqStore>();
        services.AddScoped<IFeedComponentService, FeedComponentService>();

        return services;
    }
}
