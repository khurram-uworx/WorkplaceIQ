using System.Text;
using System.Text.Encodings.Web;
using WorkplaceIQ.Content;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.AspNet.Rendering;

public sealed class ComponentHtmlRenderer(HtmlEncoder htmlEncoder, LabelHtmlRenderer labelHtmlRenderer)
{
    public string RenderFeed(string displayTitle, IReadOnlyList<Post> posts, IReadOnlyList<ContentItem> contentItems)
    {
        return Render(
            "iq-feed",
            displayTitle,
            posts,
            contentItems,
            "No feed items yet.");
    }

    public string RenderForum(string displayTitle, IReadOnlyList<Post> posts)
    {
        return Render(
            "iq-forum",
            displayTitle,
            posts,
            [],
            "No forum threads yet.");
    }

    private string Render(
        string blockClass,
        string displayTitle,
        IReadOnlyList<Post> posts,
        IReadOnlyList<ContentItem> contentItems,
        string emptyText)
    {
        var html = new StringBuilder();
        html.Append("<header class=\"");
        html.Append(blockClass);
        html.Append("__header\"><h2 class=\"");
        html.Append(blockClass);
        html.Append("__title\">");
        html.Append(htmlEncoder.Encode(displayTitle));
        html.Append("</h2></header>");

        if (posts.Count == 0 && contentItems.Count == 0)
        {
            html.Append("<p class=\"");
            html.Append(blockClass);
            html.Append("__empty\">");
            html.Append(htmlEncoder.Encode(emptyText));
            html.Append("</p>");
            return html.ToString();
        }

        html.Append("<ul class=\"");
        html.Append(blockClass);
        html.Append("__items\">");

        foreach (var item in contentItems)
        {
            html.Append("<li class=\"");
            html.Append(blockClass);
            html.Append("__item\"><article><h3 class=\"");
            html.Append(blockClass);
            html.Append("__item-title\">");
            html.Append(htmlEncoder.Encode(item.Title));
            html.Append("</h3>");

            if (!string.IsNullOrWhiteSpace(item.Body))
            {
                html.Append("<p class=\"");
                html.Append(blockClass);
                html.Append("__item-body\">");
                html.Append(htmlEncoder.Encode(item.Body));
                html.Append("</p>");
            }

            html.Append("</article></li>");
        }

        foreach (var post in posts)
        {
            html.Append("<li class=\"");
            html.Append(blockClass);
            html.Append("__item\"><article><h3 class=\"");
            html.Append(blockClass);
            html.Append("__item-title\">");
            html.Append(htmlEncoder.Encode(post.Title));
            html.Append("</h3>");

            if (!string.IsNullOrWhiteSpace(post.Body))
            {
                html.Append("<p class=\"");
                html.Append(blockClass);
                html.Append("__item-body\">");
                html.Append(htmlEncoder.Encode(post.Body));
                html.Append("</p>");
            }

            html.Append(labelHtmlRenderer.Render(post.PostLabels));
            html.Append("</article></li>");
        }

        html.Append("</ul>");

        return html.ToString();
    }
}
