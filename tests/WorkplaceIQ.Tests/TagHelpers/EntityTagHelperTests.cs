using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;
using WorkplaceIQ.AspNet.Rendering;
using WorkplaceIQ.AspNet.TagHelpers;
using WorkplaceIQ.Content;
using WorkplaceIQ.Entities;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Tests.TestDoubles;

namespace WorkplaceIQ.Tests.TagHelpers;

public class EntityTagHelperTests
{
    [Test]
    public async Task ProcessAsync_ResolvesEntityListAndRendersEntities()
    {
        var entity = new ContentItem
        {
            Id = Guid.NewGuid(),
            Discriminator = "Machine",
            Title = "Press 12",
            Body = "Hydraulic press",
            ContentData = """{"floor":"A"}""",
            Labels =
            [
                new ContentItemLabel
                {
                    Label = new Label
                    {
                        Name = "Critical",
                        NormalizedName = "CRITICAL",
                        Slug = "critical"
                    }
                }
            ]
        };
        var service = new RecordingEntityComponentService(new EntityComponentResult(
            new GroupContent
            {
                Name = "Machines",
                Title = "Machines"
            },
            [entity],
            false,
            false,
            "Machines",
            "Machine"));
        var tagHelper = new EntityListTagHelper(service, new TestHostEnvironment("Development"), CreateRenderer())
        {
            Id = "Machines",
            Title = "Machines",
            Type = "Machine"
        };

        var output = TagHelperOutputFactory.Create();

        await tagHelper.ProcessAsync(CreateContext(), output);

        var html = output.Content.GetContent();
        Assert.That(service.Request?.Id, Is.EqualTo("Machines"));
        Assert.That(service.Request?.AutoProvision, Is.True);
        Assert.That(output.TagName, Is.EqualTo("section"));
        Assert.That(output.Attributes["data-iq-entity-type"].Value, Is.EqualTo("Machine"));
        Assert.That(html, Does.Contain("<h2 class=\"iq-entity-list__title\">Machines</h2>"));
        Assert.That(html, Does.Contain("<h3 class=\"iq-entity__title\">Press 12</h3>"));
        Assert.That(html, Does.Contain("<p class=\"iq-entity__description\">Hydraulic press</p>"));
        Assert.That(html, Does.Contain("#Critical"));
    }

    [Test]
    public async Task ProcessAsync_EncodesEntityContent()
    {
        var service = new RecordingEntityComponentService(new EntityComponentResult(
            new GroupContent
            {
                Name = "Machines",
                Title = "<Machines>"
            },
            [
                new ContentItem
                {
                    Discriminator = "<Machine>",
                    Title = "<Press>",
                    Body = "Use <iq-entity>"
                }
            ],
            false,
            false,
            "<Machines>",
            "<Machine>"));
        var tagHelper = new EntityListTagHelper(service, new TestHostEnvironment("Development"), CreateRenderer())
        {
            Id = "Machines",
            Title = "<Machines>",
            Type = "<Machine>"
        };

        var output = TagHelperOutputFactory.Create();

        await tagHelper.ProcessAsync(CreateContext(), output);

        var html = output.Content.GetContent();
        Assert.That(html, Does.Contain("&lt;Machines&gt;"));
        Assert.That(html, Does.Contain("&lt;Press&gt;"));
        Assert.That(html, Does.Contain("Use &lt;iq-entity&gt;"));
        Assert.That(html, Does.Not.Contain("<Press>"));
    }

    [Test]
    public async Task ProcessAsync_RendersEmptyState()
    {
        var service = new RecordingEntityComponentService(new EntityComponentResult(
            new GroupContent
            {
                Name = "Machines",
                Title = "Machines"
            },
            [],
            false,
            false,
            "Machines",
            "Machine"));
        var tagHelper = new EntityListTagHelper(service, new TestHostEnvironment("Development"), CreateRenderer())
        {
            Id = "Machines",
            Title = "Machines",
            Type = "Machine"
        };

        var output = TagHelperOutputFactory.Create();

        await tagHelper.ProcessAsync(CreateContext(), output);

        Assert.That(output.Content.GetContent(), Does.Contain("No machine entities yet."));
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
        return new ComponentHtmlRenderer(HtmlEncoder.Default, new LabelHtmlRenderer(HtmlEncoder.Default));
    }
}