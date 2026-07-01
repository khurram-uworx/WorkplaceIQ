using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Text;
using System.Text.Encodings.Web;
using WorkplaceIQ.Content;
using WorkplaceIQ.Metrics;

namespace WorkplaceIQ.AspNet.TagHelpers;

[HtmlTargetElement("iq-metric")]
public sealed class MetricTagHelper(
    IMetricService metricService,
    IWorkplaceIqStore store,
    HtmlEncoder htmlEncoder) : TagHelper
{
    [HtmlAttributeName("name")]
    public string Name { get; set; } = string.Empty;

    [HtmlAttributeName("container-id")]
    public Guid? ContainerId { get; set; }

    [HtmlAttributeName("source")]
    public string? Source { get; set; }

    [HtmlAttributeName("container-type")]
    public string ContainerType { get; set; } = "feed";

    [HtmlAttributeName("content-type")]
    public string? ContentType { get; set; }

    [HtmlAttributeName("source-field")]
    public string? SourceField { get; set; }

    [HtmlAttributeName("window")]
    public string? Window { get; set; }

    [HtmlAttributeName("unit")]
    public string? Unit { get; set; }

    [HtmlAttributeName("display-unit")]
    public string? DisplayUnit { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "iq-metric");
        output.Attributes.SetAttribute("data-iq-metric", Name);

        if (string.IsNullOrWhiteSpace(Name))
        {
            output.Content.SetContent("<!-- iq-metric: name attribute is required -->");
            return;
        }

        MetricResult result;
        try
        {
            var containerId = ContainerId;
            if (containerId is null && !string.IsNullOrWhiteSpace(Source))
            {
                var container = await store.GetContainerByNameAsync<Container>(Source.Trim());
                containerId = container?.Id;
            }

            result = await metricService.ComputeAsync(new MetricRequest(
                Name.Trim(),
                containerId,
                ContainerType,
                ContentType,
                SourceField,
                Window,
                Unit,
                DisplayUnit));
        }
        catch (Exception)
        {
            output.Content.SetContent("<!-- iq-metric: error computing metric -->");
            return;
        }

        var html = new StringBuilder();
        html.Append("<span class=\"iq-metric__value\">");

        if (result.DisplayValue is not null)
        {
            html.Append(htmlEncoder.Encode(result.DisplayValue));
        }
        else
        {
            html.Append(htmlEncoder.Encode(result.Value.ToString("N0")));
        }

        html.Append("</span>");

        if (!string.IsNullOrWhiteSpace(result.DisplayUnit ?? result.Unit))
        {
            html.Append("<span class=\"iq-metric__unit\">");
            html.Append(htmlEncoder.Encode(result.DisplayUnit ?? result.Unit));
            html.Append("</span>");
        }

        output.Content.SetHtmlContent(html.ToString());
    }
}
