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

    [HtmlAttributeName("system-managed")]
    public bool SystemManaged { get; set; }

    [HtmlAttributeName("allow-add")]
    public bool? AllowAdd { get; set; }

    [HtmlAttributeName("allow-edit")]
    public bool? AllowEdit { get; set; }

    [HtmlAttributeName("allow-delete")]
    public bool? AllowDelete { get; set; }

    [HtmlAttributeName("allow-comment")]
    public bool? AllowComment { get; set; }

    [HtmlAttributeName("allow-label")]
    public bool? AllowLabel { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var result = await forumComponentService.ResolveForumAsync(
            new ForumComponentRequest(Id, Title, environment.IsDevelopment()));

        output.TagName = "section";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "iq-forum");
        output.Attributes.SetAttribute("data-iq-forum-id", Id);
        var interactions = GetInteractionOptions();
        ComponentHtmlRenderer.ApplyInteractionAttributes(output, interactions);

        if (result.Missing)
        {
            output.Attributes.SetAttribute("data-iq-missing", "true");
        }

        output.Content.SetHtmlContent(renderer.RenderForum(result.DisplayTitle, result.Posts, interactions));
    }

    private ComponentInteractionOptions GetInteractionOptions()
    {
        var defaults = SystemManaged
            ? ComponentInteractionOptions.SystemManaged()
            : new ComponentInteractionOptions();

        return defaults with
        {
            AllowAdd = AllowAdd ?? defaults.AllowAdd,
            AllowEdit = AllowEdit ?? defaults.AllowEdit,
            AllowDelete = AllowDelete ?? defaults.AllowDelete,
            AllowComment = AllowComment ?? defaults.AllowComment,
            AllowLabel = AllowLabel ?? defaults.AllowLabel
        };
    }
}
