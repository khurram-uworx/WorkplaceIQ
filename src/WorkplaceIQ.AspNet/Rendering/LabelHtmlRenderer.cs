using System.Text;
using System.Text.Encodings.Web;
using WorkplaceIQ.Labels;

namespace WorkplaceIQ.AspNet.Rendering;

public sealed class LabelHtmlRenderer(HtmlEncoder htmlEncoder)
{
    public string Render(IEnumerable<PostLabel> postLabels)
    {
        var labels = postLabels
            .Select(postLabel => postLabel.Label)
            .Where(label => label is not null)
            .OrderBy(label => label!.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (labels.Count == 0)
        {
            return string.Empty;
        }

        var html = new StringBuilder();
        html.Append("<ul class=\"iq-labels\" aria-label=\"Labels\">");

        foreach (var label in labels)
        {
            html.Append("<li class=\"iq-label\">#");
            html.Append(htmlEncoder.Encode(label!.Name));
            html.Append("</li>");
        }

        html.Append("</ul>");

        return html.ToString();
    }
}
