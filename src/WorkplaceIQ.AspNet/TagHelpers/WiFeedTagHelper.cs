using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Hosting;
using System.Text.Encodings.Web;
using WorkplaceIQ.Feeds;

namespace WorkplaceIQ.AspNet.TagHelpers;

[HtmlTargetElement("wi-feed")]
public sealed class WiFeedTagHelper(IFeedComponentService feedComponentService, IHostEnvironment environment) : TagHelper
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
        output.Attributes.SetAttribute("class", "wi-feed");
        output.Attributes.SetAttribute("data-wi-feed-id", Id);

        if (result.Missing)
        {
            output.Attributes.SetAttribute("data-wi-missing", "true");
        }

        var title = HtmlEncoder.Default.Encode(result.DisplayTitle);
        output.Content.SetHtmlContent($"<h2>{title}</h2>");
    }
}
