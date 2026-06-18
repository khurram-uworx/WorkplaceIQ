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
            html.Append("__item\" data-iq-item-type=\"content\" data-iq-item-id=\"");
            html.Append(item.Id);
            html.Append("\"><article><h3 class=\"");
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

            html.Append(labelHtmlRenderer.RenderContentLabels(item.ContentLabels));
            html.Append(RenderComments(blockClass, item.Posts));
            html.Append(RenderItemActions(blockClass, "content", item.Id, item.Title, item.Body, interactions));
            html.Append("</article></li>");
        }

        foreach (var post in posts)
        {
            html.Append("<li class=\"");
            html.Append(blockClass);
            html.Append("__item\" data-iq-item-type=\"post\" data-iq-item-id=\"");
            html.Append(post.Id);
            html.Append("\"><article><h3 class=\"");
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
            html.Append(RenderItemActions(blockClass, "post", post.Id, post.Title, post.Body, interactions));
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

    private string RenderItemActions(
        string blockClass,
        string itemType,
        Guid itemId,
        string title,
        string? body,
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

        if (interactions.AllowComment && itemType == "content")
        {
            AppendCommentForm(html, blockClass, itemType, itemId);
        }

        if (interactions.AllowLabel)
        {
            AppendLabelForm(html, blockClass, itemType, itemId);
        }

        if (interactions.AllowEdit)
        {
            AppendEditForm(html, blockClass, itemType, itemId, title, body);
        }

        if (interactions.AllowDelete)
        {
            AppendDeleteForm(html, blockClass, itemType, itemId);
        }

        html.Append("</div>");
        return html.ToString();
    }

    private void AppendCommentForm(
        StringBuilder html,
        string blockClass,
        string itemType,
        Guid itemId)
    {
        html.Append("<form method=\"post\" action=\"/Content/AddComment\" class=\"");
        html.Append(blockClass);
        html.Append("__item-action-form\" data-iq-action=\"comment\">");
        AppendTargetFields(html, itemType, itemId);
        html.Append("<input name=\"body\" class=\"");
        html.Append(blockClass);
        html.Append("__item-action-input\" placeholder=\"Add comment\" required>");
        AppendSubmit(html, blockClass, "comment", "Comment");
        html.Append("</form>");
    }

    private void AppendLabelForm(
        StringBuilder html,
        string blockClass,
        string itemType,
        Guid itemId)
    {
        html.Append("<form method=\"post\" action=\"/Content/AddLabel\" class=\"");
        html.Append(blockClass);
        html.Append("__item-action-form\" data-iq-action=\"label\">");
        AppendTargetFields(html, itemType, itemId);
        html.Append("<input name=\"label\" class=\"");
        html.Append(blockClass);
        html.Append("__item-action-input\" placeholder=\"Add label\" required>");
        AppendSubmit(html, blockClass, "tag", "Label");
        html.Append("</form>");
    }

    private void AppendEditForm(
        StringBuilder html,
        string blockClass,
        string itemType,
        Guid itemId,
        string title,
        string? body)
    {
        html.Append("<form method=\"post\" action=\"/Content/Edit\" class=\"");
        html.Append(blockClass);
        html.Append("__item-action-form ");
        html.Append(blockClass);
        html.Append("__item-action-form--edit\" data-iq-action=\"edit\">");
        AppendTargetFields(html, itemType, itemId);
        html.Append("<input name=\"title\" class=\"");
        html.Append(blockClass);
        html.Append("__item-action-input\" value=\"");
        html.Append(htmlEncoder.Encode(title));
        html.Append("\" required>");
        html.Append("<textarea name=\"body\" class=\"");
        html.Append(blockClass);
        html.Append("__item-action-input\" rows=\"2\">");
        html.Append(htmlEncoder.Encode(body ?? string.Empty));
        html.Append("</textarea>");
        AppendSubmit(html, blockClass, "save", "Save");
        html.Append("</form>");
    }

    private static void AppendDeleteForm(
        StringBuilder html,
        string blockClass,
        string itemType,
        Guid itemId)
    {
        html.Append("<form method=\"post\" action=\"/Content/Delete\" class=\"");
        html.Append(blockClass);
        html.Append("__item-action-form\" data-iq-action=\"delete\">");
        AppendTargetFields(html, itemType, itemId);
        AppendSubmit(html, blockClass, "trash", "Delete");
        html.Append("</form>");
    }

    private static void AppendTargetFields(
        StringBuilder html,
        string itemType,
        Guid itemId)
    {
        html.Append("<input type=\"hidden\" name=\"itemType\" value=\"");
        html.Append(itemType);
        html.Append("\"><input type=\"hidden\" name=\"itemId\" value=\"");
        html.Append(itemId);
        html.Append("\">");
    }

    private static void AppendSubmit(
        StringBuilder html,
        string blockClass,
        string icon,
        string label)
    {
        html.Append("<button type=\"submit\" class=\"");
        html.Append(blockClass);
        html.Append("__item-action\" aria-label=\"");
        html.Append(label);
        html.Append("\" title=\"");
        html.Append(label);
        html.Append("\">");
        html.Append(RenderIcon(icon));
        html.Append("<span class=\"visually-hidden\">");
        html.Append(label);
        html.Append("</span>");
        html.Append("</button>");
    }

    private static string RenderIcon(string icon)
    {
        var path = icon switch
        {
            "comment" => "<path d=\"M4 5.5A2.5 2.5 0 0 1 6.5 3h11A2.5 2.5 0 0 1 20 5.5v7A2.5 2.5 0 0 1 17.5 15H9l-5 4v-4.5A2.5 2.5 0 0 1 2 12V5.5Z\"/>",
            "tag" => "<path d=\"M3 11.5V4h7.5L21 14.5 14.5 21 4 10.5Z\"/><path d=\"M7.5 7.5h.01\"/>",
            "save" => "<path d=\"m5 12 4 4L19 6\"/>",
            "trash" => "<path d=\"M4 7h16\"/><path d=\"M10 11v6\"/><path d=\"M14 11v6\"/><path d=\"M6 7l1 14h10l1-14\"/><path d=\"M9 7V4h6v3\"/>",
            _ => string.Empty
        };

        return $"<svg class=\"iq-action-icon\" aria-hidden=\"true\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{path}</svg>";
    }

    private string RenderComments(
        string blockClass,
        IEnumerable<Post> comments)
    {
        var visibleComments = comments
            .Where(post => post.PostType == PostTypes.Comment)
            .OrderBy(post => post.CreatedAt)
            .ToList();

        if (visibleComments.Count == 0)
        {
            return string.Empty;
        }

        var html = new StringBuilder();
        html.Append("<ul class=\"");
        html.Append(blockClass);
        html.Append("__comments\" aria-label=\"Comments\">");

        foreach (var comment in visibleComments)
        {
            html.Append("<li class=\"");
            html.Append(blockClass);
            html.Append("__comment\">");
            html.Append(htmlEncoder.Encode(comment.Body));
            html.Append(labelHtmlRenderer.Render(comment.PostLabels));
            html.Append("</li>");
        }

        html.Append("</ul>");
        return html.ToString();
    }
}
