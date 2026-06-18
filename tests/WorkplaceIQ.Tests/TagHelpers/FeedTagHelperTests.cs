using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;
using WorkplaceIQ.AspNet.Rendering;
using WorkplaceIQ.AspNet.TagHelpers;
using WorkplaceIQ.Content;
using WorkplaceIQ.Feeds;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Posts;
using WorkplaceIQ.Tests.TestDoubles;

namespace WorkplaceIQ.Tests.TagHelpers;

public class FeedTagHelperTests
{
    [Test]
    public async Task ProcessAsync_ResolvesFeedByIdAndRendersTitleAndPosts()
    {
        var service = new RecordingFeedComponentService(new FeedComponentResult(
            new Content.Content
            {
                Id = Guid.NewGuid(),
                Name = "CompanyNews",
                ContentType = ContentTypes.FeedContainer,
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
                                Slug = "operations",
                                Color = "#2563eb"
                            }
                        }
                    ]
                }
            ],
            [],
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
        Assert.That(output.Content.GetContent(), Does.Contain("<li class=\"iq-label\" style=\"--iq-label-color: #2563eb\">"));
        Assert.That(output.Content.GetContent(), Does.Contain("<span class=\"iq-label__dot\" style=\"background-color: #2563eb\"></span>#Operations"));
        Assert.That(output.Content.GetContent(), Does.Contain("data-iq-action=\"label\""));
        Assert.That(output.Content.GetContent(), Does.Contain("data-iq-action=\"edit\""));
        Assert.That(output.Content.GetContent(), Does.Contain("data-iq-action=\"delete\""));
    }

    [Test]
    public async Task ProcessAsync_RendersEmptyStateWhenFeedHasNoPosts()
    {
        var service = new RecordingFeedComponentService(new FeedComponentResult(
            new Content.Content
            {
                Id = Guid.NewGuid(),
                Name = "CompanyNews",
                ContentType = ContentTypes.FeedContainer,
                Title = "News Feed"
            },
            [],
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
            new Content.Content
            {
                Id = Guid.NewGuid(),
                Name = "CompanyNews",
                ContentType = ContentTypes.FeedContainer,
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
            [],
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

    [Test]
    public async Task ProcessAsync_RendersContentItemsInFeed()
    {
        var service = new RecordingFeedComponentService(new FeedComponentResult(
            new Content.Content
            {
                Id = Guid.NewGuid(),
                Name = "PowerOutages",
                ContentType = ContentTypes.FeedContainer,
                Title = "Power Outages"
            },
            [],
            [
                new Content.Content
                {
                    Title = "Generator 3 outage",
                    Body = "Generator 3 lost power."
                }
            ],
            false,
            false,
            "Power Outages"));

        var tagHelper = new FeedTagHelper(service, new TestHostEnvironment("Development"), CreateRenderer())
        {
            Id = "PowerOutages",
            Title = "Power Outages"
        };

        var output = TagHelperOutputFactory.Create();

        await tagHelper.ProcessAsync(CreateContext(), output);

        Assert.That(output.Content.GetContent(), Does.Contain("<h3 class=\"iq-feed__item-title\">Generator 3 outage</h3>"));
        Assert.That(output.Content.GetContent(), Does.Contain("<p class=\"iq-feed__item-body\">Generator 3 lost power.</p>"));
    }

    [Test]
    public async Task ProcessAsync_SystemManagedFeedDisablesMutationActionsButAllowsCommentAndLabel()
    {
        var service = new RecordingFeedComponentService(new FeedComponentResult(
            new Content.Content
            {
                Id = Guid.NewGuid(),
                Name = "PowerOutages",
                ContentType = ContentTypes.FeedContainer,
                Title = "Power Outages"
            },
            [],
            [
                new Content.Content
                {
                    Title = "Generator 3 outage",
                    Body = "Generator 3 lost power."
                }
            ],
            false,
            false,
            "Power Outages"));

        var tagHelper = new FeedTagHelper(service, new TestHostEnvironment("Development"), CreateRenderer())
        {
            Id = "PowerOutages",
            Title = "Power Outages",
            SystemManaged = true
        };

        var output = TagHelperOutputFactory.Create();

        await tagHelper.ProcessAsync(CreateContext(), output);

        var html = output.Content.GetContent();
        Assert.That(output.Attributes["data-allow-add"].Value, Is.EqualTo("false"));
        Assert.That(output.Attributes["data-allow-edit"].Value, Is.EqualTo("false"));
        Assert.That(output.Attributes["data-allow-delete"].Value, Is.EqualTo("false"));
        Assert.That(output.Attributes["data-allow-comment"].Value, Is.EqualTo("true"));
        Assert.That(output.Attributes["data-allow-label"].Value, Is.EqualTo("true"));
        Assert.That(html, Does.Contain("data-iq-action=\"comment\""));
        Assert.That(html, Does.Contain("data-iq-action=\"label\""));
        Assert.That(html, Does.Not.Contain("data-iq-action=\"edit\""));
        Assert.That(html, Does.Not.Contain("data-iq-action=\"delete\""));
    }

    [Test]
    public async Task ProcessAsync_DisableAttributesRemoveSpecificInteractions()
    {
        var service = new RecordingFeedComponentService(new FeedComponentResult(
            new Content.Content
            {
                Id = Guid.NewGuid(),
                Name = "CompanyNews",
                ContentType = ContentTypes.FeedContainer,
                Title = "News Feed"
            },
            [
                new Post
                {
                    Title = "Quarterly update",
                    Body = "Results are ready."
                }
            ],
            [],
            false,
            false,
            "News Feed"));

        var tagHelper = new FeedTagHelper(service, new TestHostEnvironment("Development"), CreateRenderer())
        {
            Id = "CompanyNews",
            Title = "News Feed",
            DisableComment = true,
            DisableLabel = true
        };

        var output = TagHelperOutputFactory.Create();

        await tagHelper.ProcessAsync(CreateContext(), output);

        var html = output.Content.GetContent();
        Assert.That(output.Attributes["data-allow-comment"].Value, Is.EqualTo("false"));
        Assert.That(output.Attributes["data-allow-label"].Value, Is.EqualTo("false"));
        Assert.That(html, Does.Not.Contain("data-iq-action=\"label\""));
        Assert.That(html, Does.Contain("data-iq-action=\"edit\""));
        Assert.That(html, Does.Contain("data-iq-action=\"delete\""));
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
