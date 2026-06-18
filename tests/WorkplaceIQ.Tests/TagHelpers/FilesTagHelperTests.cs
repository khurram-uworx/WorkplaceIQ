namespace WorkplaceIQ.Tests.TagHelpers;

using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text.Encodings.Web;
using WorkplaceIQ.AspNet.Rendering;
using WorkplaceIQ.AspNet.TagHelpers;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Content;
using WorkplaceIQ.Files;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Posts;
using WorkplaceIQ.Tests.TestDoubles;

public class FilesTagHelperTests
{
    [Test]
    public async Task ProcessAsync_ResolvesFilesByIdAndRendersFileList()
    {
        var contentId = Guid.NewGuid();
        var service = new RecordingFileComponentService(new FileComponentResult(
            new Container
            {
                Id = Guid.NewGuid(),
                Key = "HRDocs",
                Type = ContainerTypes.Files,
                Title = "HR Documents"
            },
            [
                new FileObject(
                    new ContentItem
                    {
                        Id = contentId,
                        Title = "Leave Policy",
                        Body = "Annual leave rules",
                        UpdatedAt = new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero),
                        ContentLabels =
                        [
                            new ContentLabel
                            {
                                Label = new Label
                                {
                                    Name = "HR",
                                    NormalizedName = "HR",
                                    Slug = "hr",
                                    Color = "#2563eb"
                                }
                            }
                        ],
                        Posts =
                        [
                            new Post
                            {
                                PostType = PostTypes.Comment,
                                Body = "Reviewed"
                            }
                        ]
                    },
                    new FileRecord
                    {
                        ContentItemId = contentId,
                        FileName = "Leave Policy.pdf",
                        ContentType = "application/pdf",
                        SizeBytes = 2048,
                        BucketName = "test-files",
                        StorageProvider = "Memory",
                        ObjectKey = "object"
                    })
            ],
            false,
            false,
            "HR Documents"));

        var tagHelper = new FilesTagHelper(service, new TestHostEnvironment("Development"), CreateRenderer())
        {
            Id = "HRDocs",
            Title = "Ignored"
        };
        var output = TagHelperOutputFactory.Create("iq-files");

        await tagHelper.ProcessAsync(CreateContext(), output);

        var html = output.Content.GetContent();
        Assert.That(service.Request?.Id, Is.EqualTo("HRDocs"));
        Assert.That(service.Request?.AutoProvision, Is.True);
        Assert.That(output.TagName, Is.EqualTo("section"));
        Assert.That(html, Does.Contain("<h2 class=\"iq-files__title\">HR Documents</h2>"));
        Assert.That(html, Does.Contain("<h3 class=\"iq-files__item-title\">Leave Policy</h3>"));
        Assert.That(html, Does.Contain("<p class=\"iq-files__item-body\">Annual leave rules</p>"));
        Assert.That(html, Does.Contain("PDF · 2 KB · Updated Jun 18, 2026"));
        Assert.That(html, Does.Contain($"/Files/Download/{contentId}"));
        Assert.That(html, Does.Contain("data-iq-action=\"comment\""));
        Assert.That(html, Does.Contain("data-iq-action=\"label\""));
        Assert.That(html, Does.Contain("data-iq-action=\"edit\""));
        Assert.That(html, Does.Contain("data-iq-action=\"delete\""));
        Assert.That(html, Does.Contain("#HR"));
        Assert.That(html, Does.Contain("Reviewed"));
    }

    [Test]
    public async Task ProcessAsync_RendersEmptyStateWhenFilesLibraryHasNoFiles()
    {
        var service = new RecordingFileComponentService(new FileComponentResult(
            new Container
            {
                Id = Guid.NewGuid(),
                Key = "ITDocs",
                Type = ContainerTypes.Files,
                Title = "IT Documents"
            },
            [],
            false,
            false,
            "IT Documents"));
        var tagHelper = new FilesTagHelper(service, new TestHostEnvironment("Development"), CreateRenderer())
        {
            Id = "ITDocs",
            Title = "IT Documents"
        };
        var output = TagHelperOutputFactory.Create("iq-files");

        await tagHelper.ProcessAsync(CreateContext(), output);

        Assert.That(output.Content.GetContent(), Does.Contain("No files yet."));
    }

    [Test]
    public async Task ProcessAsync_EncodesFileContent()
    {
        var service = new RecordingFileComponentService(new FileComponentResult(
            new Container
            {
                Id = Guid.NewGuid(),
                Key = "HRDocs",
                Type = ContainerTypes.Files,
                Title = "<HR>"
            },
            [
                new FileObject(
                    new ContentItem
                    {
                        Id = Guid.NewGuid(),
                        Title = "<Policy>",
                        Body = "Use <iq-files>"
                    },
                    new FileRecord
                    {
                        FileName = "policy.pdf",
                        ContentType = "application/pdf",
                        SizeBytes = 100,
                        BucketName = "test-files",
                        StorageProvider = "Memory",
                        ObjectKey = "object"
                    })
            ],
            false,
            false,
            "<HR>"));
        var tagHelper = new FilesTagHelper(service, new TestHostEnvironment("Development"), CreateRenderer())
        {
            Id = "HRDocs",
            Title = "<Ignored>"
        };
        var output = TagHelperOutputFactory.Create("iq-files");

        await tagHelper.ProcessAsync(CreateContext(), output);

        var html = output.Content.GetContent();
        Assert.That(html, Does.Contain("&lt;HR&gt;"));
        Assert.That(html, Does.Contain("&lt;Policy&gt;"));
        Assert.That(html, Does.Contain("Use &lt;iq-files&gt;"));
        Assert.That(html, Does.Not.Contain("<Policy>"));
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
