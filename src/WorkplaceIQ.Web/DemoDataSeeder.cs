using Microsoft.EntityFrameworkCore;
using WorkplaceIQ.AspNet.Data;
using WorkplaceIQ.Feeds;
using WorkplaceIQ.Forums;

namespace WorkplaceIQ.Web;

internal static class DemoDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var dbContext = services.GetRequiredService<WorkplaceIqDbContext>();
        var feedService = services.GetRequiredService<IFeedComponentService>();
        var forumService = services.GetRequiredService<IForumComponentService>();

        var feed = await feedService.ResolveFeedAsync(new FeedComponentRequest(
            "CompanyNews",
            "News Feed",
            true));
        var forum = await forumService.ResolveForumAsync(new ForumComponentRequest(
            "MaintenanceForum",
            "Maintenance Forum",
            true));

        if (feed.Container is not null)
        {
            var existingFeedCount = await dbContext.Posts
                .CountAsync(post => post.ContainerId == feed.Container.Id);

            if (existingFeedCount == 0)
            {
                await feedService.CreatePostAsync(
                    "CompanyNews",
                    "Welcome to WorkplaceIQ",
                    "This feed is backed by a database and rendered through a WorkplaceIQ Tag Helper.",
                    "Announcement, Operations");
                await feedService.CreatePostAsync(
                    "CompanyNews",
                    "Declarative components",
                    "The page declares a feed with <iq-feed>, and the platform resolves the persisted container.",
                    "Platform");
                await feedService.CreatePostAsync(
                    "CompanyNews",
                    "Shared content model",
                    "Labels, posts, and containers are reused across feed and forum components.",
                    "Platform, Labels");
            }
        }

        if (forum.Container is not null)
        {
            var existingForumCount = await dbContext.Posts
                .CountAsync(post => post.ContainerId == forum.Container.Id);

            if (existingForumCount == 0)
            {
                await forumService.CreateThreadAsync(
                    "MaintenanceForum",
                    "Lobby HVAC inspection",
                    "Facilities will inspect the lobby HVAC unit before the next tenant event.",
                    "Maintenance, Facilities");
                await forumService.CreateThreadAsync(
                    "MaintenanceForum",
                    "Parking garage lighting",
                    "Report any remaining dark zones after the level two fixture replacement.",
                    "Safety, Maintenance");
            }
        }
    }
}
