using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Hosting;
using WorkplaceIQ.AspNet.Rendering;
using WorkplaceIQ.Entities;

namespace WorkplaceIQ.AspNet.TagHelpers;

[HtmlTargetElement("iq-entity-list")]
public sealed class EntityListTagHelper(
    IEntityComponentService entityComponentService,
    IHostEnvironment environment,
    ComponentHtmlRenderer renderer) : TagHelper
{
    [HtmlAttributeName("id")]
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Type { get; set; } = "Entity";

    [HtmlAttributeName("system-managed")]
    public bool SystemManaged { get; set; }

    [HtmlAttributeName("disable-add")]
    public bool DisableAdd { get; set; }

    [HtmlAttributeName("disable-edit")]
    public bool DisableEdit { get; set; }

    [HtmlAttributeName("disable-delete")]
    public bool DisableDelete { get; set; }

    [HtmlAttributeName("disable-comment")]
    public bool DisableComment { get; set; }

    [HtmlAttributeName("disable-label")]
    public bool DisableLabel { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        var result = await entityComponentService.ResolveEntitiesAsync(
            new EntityComponentRequest(Id, Title, Type, environment.IsDevelopment()));

        output.TagName = "section";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "iq-entity-list");
        output.Attributes.SetAttribute("data-iq-entity-list-id", Id);
        output.Attributes.SetAttribute("data-iq-entity-type", result.EntityType);
        var interactions = GetInteractionOptions();
        ComponentHtmlRenderer.ApplyInteractionAttributes(output, interactions);

        if (result.Missing)
        {
            output.Attributes.SetAttribute("data-iq-missing", "true");
        }

        output.Content.SetHtmlContent(renderer.RenderEntities(result.DisplayTitle, result.EntityType, result.Entities, interactions));
    }

    private ComponentInteractionOptions GetInteractionOptions()
    {
        var defaults = SystemManaged
            ? ComponentInteractionOptions.SystemManaged()
            : new ComponentInteractionOptions();

        return defaults with
        {
            AllowAdd = defaults.AllowAdd && !DisableAdd,
            AllowEdit = defaults.AllowEdit && !DisableEdit,
            AllowDelete = defaults.AllowDelete && !DisableDelete,
            AllowComment = defaults.AllowComment && !DisableComment,
            AllowLabel = defaults.AllowLabel && !DisableLabel
        };
    }
}
