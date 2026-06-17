namespace WorkplaceIQ.Tests.TagHelpers;

using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;
using WorkplaceIQ.AspNet.Rendering;
using WorkplaceIQ.AspNet.TagHelpers;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Feeds;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Posts;
using WorkplaceIQ.Tests.TestDoubles;

public class FeedTagHelperTests
{
    [Test]
    public async Task ProcessAsync_ResolvesFeedByIdAndRendersTitleAndPosts()
    {
        var service = new RecordingFeedComponentService(new FeedComponentResult(
            new Container
            {
                Id = Guid.NewGuid(),
                Key = "CompanyNews",
                Type = ContainerTypes.Feed,
                Title = "News Feed"
            },
            [
                new Post
                {
                    ContainerId = Guid.NewGuid(),
                    Title = "Quarterly update",
                    Body = "Results are ready.",
                    PostLabels =
                    [
                        new PostLabel
                        {
                            Label = new Label
                            {
                                Name = "Operations",
                                NormalizedName = "OPERATIONS",
                                Slug = "operations"
                            }
                        }
                    ]
                }
            ],
            false,
            false,
            "News Feed"));

        var tagHelper = new FeedTagHelper(service, new TestHostEnvironment("Development"), CreateRenderer())
        {
            Id = "CompanyNews",
            Title = "Ignored After Binding"
        };

        var output = TagHelperOutputFactory.Create();

        await tagHelper.ProcessAsync(CreateContext(), output);

        Assert.That(service.Request?.Id, Is.EqualTo("CompanyNews"));
        Assert.That(service.Request?.AutoProvision, Is.True);
        Assert.That(output.TagName, Is.EqualTo("section"));
        Assert.That(output.Content.GetContent(), Does.Contain("<h2 class=\"iq-feed__title\">News Feed</h2>"));
        Assert.That(output.Content.GetContent(), Does.Contain("<h3 class=\"iq-feed__item-title\">Quarterly update</h3>"));
        Assert.That(output.Content.GetContent(), Does.Contain("<p class=\"iq-feed__item-body\">Results are ready.</p>"));
        Assert.That(output.Content.GetContent(), Does.Contain("<li class=\"iq-label\">#Operations</li>"));
    }

    [Test]
    public async Task ProcessAsync_RendersEmptyStateWhenFeedHasNoPosts()
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

        var tagHelper = new FeedTagHelper(service, new TestHostEnvironment("Development"), CreateRenderer())
        {
            Id = "CompanyNews",
            Title = "News Feed"
        };

        var output = TagHelperOutputFactory.Create();

        await tagHelper.ProcessAsync(CreateContext(), output);

        Assert.That(output.Content.GetContent(), Does.Contain("No feed items yet."));
    }

    [Test]
    public async Task ProcessAsync_EncodesTitleAndPostContent()
    {
        var service = new RecordingFeedComponentService(new FeedComponentResult(
            new Container
            {
                Id = Guid.NewGuid(),
                Key = "CompanyNews",
                Type = ContainerTypes.Feed,
                Title = "<News>"
            },
            [
                new Post
                {
                    ContainerId = Guid.NewGuid(),
                    Title = "<Quarterly>",
                    Body = "Use <iq-feed>",
                    PostLabels =
                    [
                        new PostLabel
                        {
                            Label = new Label
                            {
                                Name = "<script>",
                                NormalizedName = "<SCRIPT>",
                                Slug = "script"
                            }
                        }
                    ]
                }
            ],
            false,
            false,
            "<News>"));

        var tagHelper = new FeedTagHelper(service, new TestHostEnvironment("Development"), CreateRenderer())
        {
            Id = "CompanyNews",
            Title = "<Ignored>"
        };

        var output = TagHelperOutputFactory.Create();

        await tagHelper.ProcessAsync(CreateContext(), output);

        var html = output.Content.GetContent();
        Assert.That(html, Does.Contain("&lt;News&gt;"));
        Assert.That(html, Does.Contain("&lt;Quarterly&gt;"));
        Assert.That(html, Does.Contain("Use &lt;iq-feed&gt;"));
        Assert.That(html, Does.Contain("#&lt;script&gt;"));
        Assert.That(html, Does.Not.Contain("<News>"));
        Assert.That(html, Does.Not.Contain("<Quarterly>"));
        Assert.That(html, Does.Not.Contain("<script>"));
    }

    private static TagHelperContext CreateContext()
    {
        return new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString());
    }

    private static ComponentHtmlRenderer CreateRenderer()
    {
        var labelRenderer = new LabelHtmlRenderer(HtmlEncoder.Default);
        return new ComponentHtmlRenderer(HtmlEncoder.Default, labelRenderer);
    }
}
