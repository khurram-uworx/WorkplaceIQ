using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Hosting;
using WorkplaceIQ.AspNet.Rendering;
using WorkplaceIQ.Feeds;

namespace WorkplaceIQ.AspNet.TagHelpers;

[HtmlTargetElement("iq-feed")]
public sealed class FeedTagHelper(
    IFeedComponentService feedComponentService,
    IHostEnvironment environment,
    ComponentHtmlRenderer renderer) : TagHelper
{
    [HtmlAttributeName("id")]
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var result = await feedComponentService.ResolveFeedAsync(
            new FeedComponentRequest(Id, Title, environment.IsDevelopment()));

        output.TagName = "section";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "iq-feed");
        output.Attributes.SetAttribute("data-iq-feed-id", Id);

        if (result.Missing)
        {
            output.Attributes.SetAttribute("data-iq-missing", "true");
        }

        output.Content.SetHtmlContent(renderer.RenderFeed(result.DisplayTitle, result.Posts, result.ContentItems));
    }
}
