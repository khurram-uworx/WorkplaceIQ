namespace WorkplaceIQ.Tests.Feeds;

using WorkplaceIQ.Containers;
using WorkplaceIQ.Components;
using WorkplaceIQ.Content;
using WorkplaceIQ.Feeds;
using WorkplaceIQ.Forums;
using WorkplaceIQ.Tests.TestDoubles;

public class FeedComponentServiceTests
{
    [Test]
    public async Task ResolveFeedAsync_AutoProvisionsMissingFeedInDevelopment()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = CreateFeedService(store);

        var first = await service.ResolveFeedAsync(new FeedComponentRequest(
            "CompanyNews",
            "News Feed",
            true));
        var second = await service.ResolveFeedAsync(new FeedComponentRequest(
            "CompanyNews",
            "Changed Title",
            true));

        Assert.That(first.Created, Is.True);
        Assert.That(first.DisplayTitle, Is.EqualTo("News Feed"));
        Assert.That(second.Created, Is.False);
        Assert.That(second.DisplayTitle, Is.EqualTo("News Feed"));
        Assert.That(store.Containers, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ResolveFeedAsync_ReturnsContentItemsForFeedContainer()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = CreateFeedService(store);

        var feed = await service.ResolveFeedAsync(new FeedComponentRequest(
            "PowerOutages",
            "Power Outages",
            true));
        store.ContentItems.Add(new ContentItem
        {
            ContainerId = feed.Container!.Id,
            ContentType = "Outage",
            Name = "generator-3-outage",
            Title = "Generator 3 outage"
        });

        var result = await service.ResolveFeedAsync(new FeedComponentRequest(
            "PowerOutages",
            "Power Outages",
            true));

        Assert.That(result.ContentItems.Select(item => item.Title), Is.EquivalentTo(new[] { "Generator 3 outage" }));
    }

    [Test]
    public async Task ResolveFeedAsync_DoesNotAutoProvisionMissingFeedInProduction()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = CreateFeedService(store);

        var result = await service.ResolveFeedAsync(new FeedComponentRequest(
            "CompanyNews",
            "News Feed",
            false));

        Assert.That(result.Missing, Is.True);
        Assert.That(result.Created, Is.False);
        Assert.That(store.Containers, Is.Empty);
    }

    [Test]
    public void ResolveFeedAsync_RejectsMissingFeedId()
    {
        var service = CreateFeedService(new InMemoryWorkplaceIqStore());

        var exception = Assert.ThrowsAsync<ArgumentException>(() =>
            service.ResolveFeedAsync(new FeedComponentRequest(" ", "News Feed", true)));

        Assert.That(exception!.Message, Does.Contain("A feed id is required."));
    }

    [Test]
    public async Task ResolveFeedAsync_TrimsFeedIdAndInitialTitle()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = CreateFeedService(store);

        var result = await service.ResolveFeedAsync(new FeedComponentRequest(
            " CompanyNews ",
            " News Feed ",
            true));

        Assert.That(result.DisplayTitle, Is.EqualTo("News Feed"));
        Assert.That(store.Containers[0].Key, Is.EqualTo("CompanyNews"));
        Assert.That(store.Containers[0].Title, Is.EqualTo("News Feed"));
    }

    [Test]
    public async Task CreatePostAsync_CreatesPostForExistingFeed()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = CreateFeedService(store);
        var container = await store.CreateContainerAsync(
            "CompanyNews",
            ContainerTypes.Feed,
            "News Feed");

        var post = await service.CreatePostAsync(
            " CompanyNews ",
            " First item ",
            " Hello from the feed. ");

        Assert.That(post.ContainerId, Is.EqualTo(container.Id));
        Assert.That(store.Posts, Has.Count.EqualTo(1));
        Assert.That(store.Posts[0].Title, Is.EqualTo("First item"));
        Assert.That(store.Posts[0].Body, Is.EqualTo("Hello from the feed."));
        Assert.That(post.PostType, Is.EqualTo("post"));
    }

    [TestCase("", "Title", "Body", "A feed id is required.")]
    [TestCase("CompanyNews", "", "Body", "A feed post title is required.")]
    [TestCase("CompanyNews", "Title", "", "A feed post body is required.")]
    public void CreatePostAsync_RejectsInvalidInput(
        string feedId,
        string title,
        string body,
        string expectedMessage)
    {
        var service = CreateFeedService(new InMemoryWorkplaceIqStore());

        var exception = Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreatePostAsync(feedId, title, body));

        Assert.That(exception!.Message, Does.Contain(expectedMessage));
    }

    [Test]
    public void CreatePostAsync_RejectsMissingFeed()
    {
        var service = CreateFeedService(new InMemoryWorkplaceIqStore());

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreatePostAsync("CompanyNews", "First item", "Hello from the feed."));

        Assert.That(exception!.Message, Does.Contain("Feed 'CompanyNews' does not exist."));
    }

    [Test]
    public async Task FeedAndForumContainers_AreProvisionedIndependently()
    {
        var store = new InMemoryWorkplaceIqStore();
        var componentService = new ComponentService(store);
        var feedService = new FeedComponentService(componentService, store);
        var forumService = new ForumComponentService(componentService);

        await feedService.ResolveFeedAsync(new FeedComponentRequest(
            "CompanyNews",
            "News Feed",
            true));
        await forumService.ResolveForumAsync(new ForumComponentRequest(
            "CompanyNews",
            "Company Forum",
            true));

        Assert.That(store.Containers, Has.Count.EqualTo(2));
        Assert.That(store.Containers.Single(container => container.Type == ContainerTypes.Feed).Title, Is.EqualTo("News Feed"));
        Assert.That(store.Containers.Single(container => container.Type == ContainerTypes.Forum).Title, Is.EqualTo("Company Forum"));
    }

    [Test]
    public async Task CreatePostAsync_NormalizesAndAttachesLabels()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = CreateFeedService(store);
        await store.CreateContainerAsync(
            "CompanyNews",
            ContainerTypes.Feed,
            "News Feed");

        var post = await service.CreatePostAsync(
            "CompanyNews",
            "First item",
            "Hello from the feed.",
            " Safety, safety, Facilities ");

        Assert.That(post.PostLabels, Has.Count.EqualTo(2));
        Assert.That(store.Labels.Select(label => label.NormalizedName), Is.EquivalentTo(new[] { "SAFETY", "FACILITIES" }));
        Assert.That(store.Labels.Select(label => label.Slug), Does.Contain("safety"));
        Assert.That(store.Labels.Select(label => label.Slug), Does.Contain("facilities"));
    }

    [Test]
    public async Task FeedAndForumPosts_UseCorrectContainersAndSharedLabels()
    {
        var store = new InMemoryWorkplaceIqStore();
        var componentService = new ComponentService(store);
        var feedService = new FeedComponentService(componentService, store);
        var forumService = new ForumComponentService(componentService);

        var feed = await feedService.ResolveFeedAsync(new FeedComponentRequest(
            "CompanyNews",
            "News Feed",
            true));
        var forum = await forumService.ResolveForumAsync(new ForumComponentRequest(
            "MaintenanceForum",
            "Maintenance Forum",
            true));

        var feedPost = await feedService.CreatePostAsync(
            "CompanyNews",
            "Feed item",
            "Feed body",
            "Maintenance");
        var forumPost = await forumService.CreateThreadAsync(
            "MaintenanceForum",
            "Forum thread",
            "Forum body",
            "maintenance");

        Assert.That(feedPost.ContainerId, Is.EqualTo(feed.Container!.Id));
        Assert.That(forumPost.ContainerId, Is.EqualTo(forum.Container!.Id));
        Assert.That(feedPost.PostType, Is.EqualTo("post"));
        Assert.That(forumPost.PostType, Is.EqualTo("thread"));
        Assert.That(store.Labels, Has.Count.EqualTo(1));
        Assert.That(feedPost.PostLabels.Single().LabelId, Is.EqualTo(forumPost.PostLabels.Single().LabelId));
    }

    [Test]
    public async Task StoreCreatePostAsync_InfersPostTypeFromContainerAndContent()
    {
        var store = new InMemoryWorkplaceIqStore();
        var feed = await store.CreateContainerAsync("CompanyNews", ContainerTypes.Feed, "News Feed");
        var forum = await store.CreateContainerAsync("MaintenanceForum", ContainerTypes.Forum, "Maintenance Forum");
        var contentId = Guid.NewGuid();

        var feedPost = await store.CreatePostAsync(feed.Id, "Feed item", "Body", []);
        var forumThread = await store.CreatePostAsync(forum.Id, "Thread", "Body", []);
        var comment = await store.CreatePostAsync(feed.Id, "Comment", "Body", [], contentId: contentId);

        Assert.That(feedPost.PostType, Is.EqualTo("post"));
        Assert.That(forumThread.PostType, Is.EqualTo("thread"));
        Assert.That(comment.PostType, Is.EqualTo("comment"));
    }

    private static FeedComponentService CreateFeedService(InMemoryWorkplaceIqStore store)
    {
        return new FeedComponentService(new ComponentService(store), store);
    }
}
