using Microsoft.AspNetCore.Razor.TagHelpers;
using WorkplaceIQ.AspNet.Rendering;
using WorkplaceIQ.Entities;

namespace WorkplaceIQ.AspNet.TagHelpers;

[HtmlTargetElement("iq-entity")]
public sealed class EntityTagHelper(
    IEntityComponentService entityComponentService,
    ComponentHtmlRenderer renderer) : TagHelper
{
    [HtmlAttributeName("id")]
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Type { get; set; } = "Entity";

    [HtmlAttributeName("container")]
    public string? ContainerName { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var content = string.IsNullOrWhiteSpace(Id)
            ? null
            : await entityComponentService.ResolveDetailAsync(Id);

        output.TagName = "section";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "iq-entity");
        output.Attributes.SetAttribute("data-iq-entity-id", Id);
        output.Attributes.SetAttribute("data-iq-entity-type", Type);

        if (!string.IsNullOrWhiteSpace(ContainerName))
        {
            output.Attributes.SetAttribute("data-iq-entity-container", ContainerName);
        }

        if (content is null)
        {
            output.Attributes.SetAttribute("data-iq-missing", "true");
            output.Content.SetHtmlContent(renderer.RenderEntityDetail(new Content.Content
            {
                Name = Id,
                Title = string.IsNullOrWhiteSpace(Title) ? Id : Title,
                ContentType = Type
            }));
            return;
        }

        output.Content.SetHtmlContent(renderer.RenderEntityDetail(content));
    }
}
