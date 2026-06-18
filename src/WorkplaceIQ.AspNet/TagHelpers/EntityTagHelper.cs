using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Hosting;
using WorkplaceIQ.AspNet.Rendering;
using WorkplaceIQ.Entities;

namespace WorkplaceIQ.AspNet.TagHelpers;

[HtmlTargetElement("iq-entity")]
[HtmlTargetElement("iq-entity-list")]
public sealed class EntityTagHelper(
    IEntityComponentService entityComponentService,
    IHostEnvironment environment,
    ComponentHtmlRenderer renderer) : TagHelper
{
    [HtmlAttributeName("id")]
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Type { get; set; } = "Entity";

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var result = await entityComponentService.ResolveEntitiesAsync(
            new EntityComponentRequest(Id, Title, Type, environment.IsDevelopment()));

        output.TagName = "section";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "iq-entity-list");
        output.Attributes.SetAttribute("data-iq-entity-list-id", Id);
        output.Attributes.SetAttribute("data-iq-entity-type", result.EntityType);

        if (result.Missing)
        {
            output.Attributes.SetAttribute("data-iq-missing", "true");
        }

        output.Content.SetHtmlContent(renderer.RenderEntities(result.DisplayTitle, result.EntityType, result.Entities));
    }
}
