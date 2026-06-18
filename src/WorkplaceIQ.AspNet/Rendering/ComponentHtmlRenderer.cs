using System.Text;
using System.Text.Encodings.Web;
using WorkplaceIQ.Content;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.AspNet.Rendering;

public sealed class ComponentHtmlRenderer(HtmlEncoder htmlEncoder, LabelHtmlRenderer labelHtmlRenderer)
{
    public string RenderFeed(
        string displayTitle,
        IReadOnlyList<Post> posts,
        IReadOnlyList<ContentItem> contentItems,
        ComponentInteractionOptions interactions)
    {
        return Render(
            "iq-feed",
            displayTitle,
            posts,
            contentItems,
            "No feed items yet.",
            interactions);
    }

    public string RenderForum(
        string displayTitle,
        IReadOnlyList<Post> posts,
        ComponentInteractionOptions interactions)
    {
        return Render(
            "iq-forum",
            displayTitle,
            posts,
            [],
            "No forum threads yet.",
            interactions);
    }

    private string Render(
        string blockClass,
        string displayTitle,
        IReadOnlyList<Post> posts,
        IReadOnlyList<ContentItem> contentItems,
        string emptyText,
        ComponentInteractionOptions interactions)
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

            html.Append(RenderItemActions(blockClass, interactions));
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
            html.Append(RenderItemActions(blockClass, interactions));
            html.Append("</article></li>");
        }

        html.Append("</ul>");

        return html.ToString();
    }

    public static void ApplyInteractionAttributes(
        Microsoft.AspNetCore.Razor.TagHelpers.TagHelperOutput output,
        ComponentInteractionOptions interactions)
    {
        output.Attributes.SetAttribute("data-allow-add", interactions.AllowAdd.ToString().ToLowerInvariant());
        output.Attributes.SetAttribute("data-allow-edit", interactions.AllowEdit.ToString().ToLowerInvariant());
        output.Attributes.SetAttribute("data-allow-delete", interactions.AllowDelete.ToString().ToLowerInvariant());
        output.Attributes.SetAttribute("data-allow-comment", interactions.AllowComment.ToString().ToLowerInvariant());
        output.Attributes.SetAttribute("data-allow-label", interactions.AllowLabel.ToString().ToLowerInvariant());
    }

    private static string RenderItemActions(
        string blockClass,
        ComponentInteractionOptions interactions)
    {
        if (!interactions.AllowEdit
            && !interactions.AllowDelete
            && !interactions.AllowComment
            && !interactions.AllowLabel)
        {
            return string.Empty;
        }

        var html = new StringBuilder();
        html.Append("<div class=\"");
        html.Append(blockClass);
        html.Append("__item-actions\">");

        AppendAction(html, blockClass, "comment", "Comment", interactions.AllowComment);
        AppendAction(html, blockClass, "label", "Label", interactions.AllowLabel);
        AppendAction(html, blockClass, "edit", "Edit", interactions.AllowEdit);
        AppendAction(html, blockClass, "delete", "Delete", interactions.AllowDelete);

        html.Append("</div>");
        return html.ToString();
    }

    private static void AppendAction(
        StringBuilder html,
        string blockClass,
        string action,
        string label,
        bool enabled)
    {
        if (!enabled)
        {
            return;
        }

        html.Append("<button type=\"button\" class=\"");
        html.Append(blockClass);
        html.Append("__item-action\" data-iq-action=\"");
        html.Append(action);
        html.Append("\">");
        html.Append(label);
        html.Append("</button>");
    }
}
