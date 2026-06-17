namespace WorkplaceIQ.Tests;

using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using WorkplaceIQ;
using WorkplaceIQ.AspNet.TagHelpers;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Feeds;

public class FeedTests
{
    sealed class RecordingFeedComponentService(FeedComponentResult result) : IFeedComponentService
    {
        public FeedComponentRequest? Request { get; set; }

        public Task<FeedComponentResult> ResolveFeedAsync(
            FeedComponentRequest request,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            return Task.FromResult(result);
        }
    }

    sealed class InMemoryWorkplaceIqStore : IWorkplaceIqStore
    {
        public List<Container> Containers { get; } = [];

        public List<FeedPost> Posts { get; } = [];

        public Task<Container?> GetContainerByKeyAsync(
            string key,
            string type,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Containers.FirstOrDefault(container =>
                container.Key == key && container.Type == type));
        }

        public Task<Container> CreateContainerAsync(
            string key,
            string type,
            string title,
            CancellationToken cancellationToken = default)
        {
            var container = new Container
            {
                Key = key,
                Type = type,
                Title = title
            };

            Containers.Add(container);

            return Task.FromResult(container);
        }

        public Task<IReadOnlyList<FeedPost>> GetFeedPostsAsync(
            Guid containerId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<FeedPost>>(
                Posts.Where(post => post.ContainerId == containerId).ToList());
        }
    }

    sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "WorkplaceIQ.Tests";

        public string ContentRootPath { get; set; } = TestContext.CurrentContext.WorkDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    static TagHelperOutput createTagHelperOutput()
        => new TagHelperOutput(
            "wi-feed",
            new TagHelperAttributeList(),
            (_, _) =>
            {
                var content = new DefaultTagHelperContent();
                return Task.FromResult<TagHelperContent>(content);
            });

    [Test]
    public async Task WiFeedTagHelper_ResolvesFeedByIdAndRendersTitle()
    {
        var service = new RecordingFeedComponentService(new FeedComponentResult(
            new Container
            {
                Id = Guid.NewGuid(),
                Key = "CompanyNews",
                Type = ContainerTypes.Feed,
                Title = "News Feed"
            },
            [],
            false,
            false,
            "News Feed"));

        var tagHelper = new WiFeedTagHelper(service, new TestHostEnvironment("Development"))
        {
            Id = "CompanyNews",
            Title = "Ignored After Binding"
        };

        var output = createTagHelperOutput();

        await tagHelper.ProcessAsync(
            new TagHelperContext(
                new TagHelperAttributeList(),
                new Dictionary<object, object>(),
                Guid.NewGuid().ToString()),
            output);

        Assert.That(service.Request?.Id, Is.EqualTo("CompanyNews"));
        Assert.That(service.Request?.AutoProvision, Is.True);
        Assert.That(output.TagName, Is.EqualTo("section"));
        Assert.That(output.Content.GetContent(), Is.EqualTo("<h2>News Feed</h2>"));
    }

    [Test]
    public async Task FeedComponentService_AutoProvisionsMissingFeedInDevelopment()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = new FeedComponentService(store);

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
    public async Task FeedComponentService_DoesNotAutoProvisionMissingFeedInProduction()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = new FeedComponentService(store);

        var result = await service.ResolveFeedAsync(new FeedComponentRequest(
            "CompanyNews",
            "News Feed",
            false));

        Assert.That(result.Missing, Is.True);
        Assert.That(result.Created, Is.False);
        Assert.That(store.Containers, Is.Empty);
    }

}
