using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;
using WorkplaceIQ.AspNet.Rendering;
using WorkplaceIQ.AspNet.TagHelpers;
using WorkplaceIQ.Content;
using WorkplaceIQ.Forums;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Posts;
using WorkplaceIQ.Tests.TestDoubles;

namespace WorkplaceIQ.Tests.TagHelpers;

public class ForumTagHelperTests
{
    [Test]
    public async Task ProcessAsync_ResolvesForumByIdAndRendersTitleThreadsAndLabels()
    {
        var service = new RecordingForumComponentService(new ForumComponentResult(
            new Content.Content
            {
                Id = Guid.NewGuid(),
                Name = "MaintenanceForum",
                ContentType = ContentTypes.ForumContainer,
                Title = "Maintenance Forum"
            },
            [
                new Post
                {
                    ContainerId = Guid.NewGuid(),
                    Title = "Parking lights",
                    Body = "Level two needs inspection.",
                    PostLabels =
                    [
                        new PostLabel
                        {
                            Label = new Label
                            {
                                Name = "Safety",
                                NormalizedName = "SAFETY",
                                Slug = "safety"
                            }
                        }
                    ]
                }
            ],
            false,
            false,
            "Maintenance Forum"));

        var tagHelper = new ForumTagHelper(service, new TestHostEnvironment("Development"), CreateRenderer())
        {
            Id = "MaintenanceForum",
            Title = "Ignored After Binding"
        };

        var output = TagHelperOutputFactory.Create("iq-forum");

        await tagHelper.ProcessAsync(CreateContext(), output);

        Assert.That(service.Request?.Id, Is.EqualTo("MaintenanceForum"));
        Assert.That(service.Request?.AutoProvision, Is.True);
        Assert.That(output.TagName, Is.EqualTo("section"));
        Assert.That(output.Content.GetContent(), Does.Contain("<h2 class=\"iq-forum__title\">Maintenance Forum</h2>"));
        Assert.That(output.Content.GetContent(), Does.Contain("<h3 class=\"iq-forum__item-title\">Parking lights</h3>"));
        Assert.That(output.Content.GetContent(), Does.Contain("<p class=\"iq-forum__item-body\">Level two needs inspection.</p>"));
        Assert.That(output.Content.GetContent(), Does.Contain("<span class=\"iq-label__dot\"></span>#Safety"));
        Assert.That(output.Content.GetContent(), Does.Contain("data-iq-action=\"label\""));
        Assert.That(output.Content.GetContent(), Does.Contain("data-iq-action=\"edit\""));
        Assert.That(output.Content.GetContent(), Does.Contain("data-iq-action=\"delete\""));
    }

    [Test]
    public async Task ProcessAsync_RendersEmptyStateWhenForumHasNoPosts()
    {
        var service = new RecordingForumComponentService(new ForumComponentResult(
            new Content.Content
            {
                Id = Guid.NewGuid(),
                Name = "MaintenanceForum",
                ContentType = ContentTypes.ForumContainer,
                Title = "Maintenance Forum"
            },
            [],
            false,
            false,
            "Maintenance Forum"));

        var tagHelper = new ForumTagHelper(service, new TestHostEnvironment("Development"), CreateRenderer())
        {
            Id = "MaintenanceForum",
            Title = "Maintenance Forum"
        };

        var output = TagHelperOutputFactory.Create("iq-forum");

        await tagHelper.ProcessAsync(CreateContext(), output);

        Assert.That(output.Content.GetContent(), Does.Contain("No forum threads yet."));
    }

    [Test]
    public async Task ProcessAsync_EncodesTitleThreadContentAndLabels()
    {
        var service = new RecordingForumComponentService(new ForumComponentResult(
            new Content.Content
            {
                Id = Guid.NewGuid(),
                Name = "MaintenanceForum",
                ContentType = ContentTypes.ForumContainer,
                Title = "<Forum>"
            },
            [
                new Post
                {
                    ContainerId = Guid.NewGuid(),
                    Title = "<Thread>",
                    Body = "Use <iq-forum>",
                    PostLabels =
                    [
                        new PostLabel
                        {
                            Label = new Label
                            {
                                Name = "<label>",
                                NormalizedName = "<LABEL>",
                                Slug = "label"
                            }
                        }
                    ]
                }
            ],
            false,
            false,
            "<Forum>"));

        var tagHelper = new ForumTagHelper(service, new TestHostEnvironment("Development"), CreateRenderer())
        {
            Id = "MaintenanceForum",
            Title = "<Ignored>"
        };

        var output = TagHelperOutputFactory.Create("iq-forum");

        await tagHelper.ProcessAsync(CreateContext(), output);

        var html = output.Content.GetContent();
        Assert.That(html, Does.Contain("&lt;Forum&gt;"));
        Assert.That(html, Does.Contain("&lt;Thread&gt;"));
        Assert.That(html, Does.Contain("Use &lt;iq-forum&gt;"));
        Assert.That(html, Does.Contain("#&lt;label&gt;"));
        Assert.That(html, Does.Not.Contain("<Forum>"));
        Assert.That(html, Does.Not.Contain("<Thread>"));
        Assert.That(html, Does.Not.Contain("<label>"));
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
