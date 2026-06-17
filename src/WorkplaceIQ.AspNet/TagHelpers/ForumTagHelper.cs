using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Hosting;
using WorkplaceIQ.AspNet.Rendering;
using WorkplaceIQ.Forums;

namespace WorkplaceIQ.AspNet.TagHelpers;

[HtmlTargetElement("iq-forum")]
public sealed class ForumTagHelper(
    IForumComponentService forumComponentService,
    IHostEnvironment environment,
    ComponentHtmlRenderer renderer) : TagHelper
{
    [HtmlAttributeName("id")]
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var result = await forumComponentService.ResolveForumAsync(
            new ForumComponentRequest(Id, Title, environment.IsDevelopment()));

        output.TagName = "section";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "iq-forum");
        output.Attributes.SetAttribute("data-iq-forum-id", Id);

        if (result.Missing)
        {
            output.Attributes.SetAttribute("data-iq-missing", "true");
        }

        output.Content.SetHtmlContent(renderer.RenderForum(result.DisplayTitle, result.Posts));
    }
}
