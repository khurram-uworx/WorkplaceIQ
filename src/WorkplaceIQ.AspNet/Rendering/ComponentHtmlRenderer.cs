using System.Text;
using System.Text.Encodings.Web;
using WorkplaceIQ.Content;
using WorkplaceIQ.Entities;
using WorkplaceIQ.Files;
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

    public string RenderFiles(
        string displayTitle,
        IReadOnlyList<FileObject> files,
        ComponentInteractionOptions interactions)
    {
        var html = new StringBuilder();
        html.Append("<header class=\"iq-files__header\"><h2 class=\"iq-files__title\">");
        html.Append(htmlEncoder.Encode(displayTitle));
        html.Append("</h2></header>");

        if (files.Count == 0)
        {
            html.Append("<p class=\"iq-files__empty\">No files yet.</p>");
            return html.ToString();
        }

        html.Append("<ul class=\"iq-files__items\">");

        foreach (var file in files)
        {
            var content = file.ContentItem;
            var record = file.FileRecord;
            html.Append("<li class=\"iq-files__item\" data-iq-item-type=\"content\" data-iq-item-id=\"");
            html.Append(content.Id);
            html.Append("\"><article class=\"iq-files__row\"><div class=\"iq-files__icon\" aria-hidden=\"true\">");
            html.Append(RenderFileIcon(record.FileName));
            html.Append("</div><div class=\"iq-files__main\"><h3 class=\"iq-files__item-title\">");
            html.Append(htmlEncoder.Encode(content.Title));
            html.Append("</h3>");

            if (!string.IsNullOrWhiteSpace(content.Body))
            {
                html.Append("<p class=\"iq-files__item-body\">");
                html.Append(htmlEncoder.Encode(content.Body));
                html.Append("</p>");
            }

            html.Append("<p class=\"iq-files__meta\">");
            html.Append(htmlEncoder.Encode(GetFileExtension(record.FileName)));
            html.Append(" · ");
            html.Append(htmlEncoder.Encode(FormatSize(record.SizeBytes)));
            html.Append(" · Updated ");
            html.Append(htmlEncoder.Encode(content.UpdatedAt.ToString("MMM d, yyyy")));
            html.Append("</p>");

            html.Append(labelHtmlRenderer.RenderContentLabels(content.ContentLabels));
            html.Append(RenderComments("iq-files", content.Posts));
            html.Append(RenderFileActions(content, interactions));
            html.Append("</div></article></li>");
        }

        html.Append("</ul>");
        return html.ToString();
    }

    public string RenderEntities(
        string displayTitle,
        string entityType,
        IReadOnlyList<BusinessEntity> entities)
    {
        var html = new StringBuilder();
        html.Append("<header class=\"iq-entity-list__header\"><h2 class=\"iq-entity-list__title\">");
        html.Append(htmlEncoder.Encode(displayTitle));
        html.Append("</h2></header>");

        if (entities.Count == 0)
        {
            html.Append("<p class=\"iq-entity-list__empty\">No ");
            html.Append(htmlEncoder.Encode(entityType.ToLowerInvariant()));
            html.Append(" entities yet.</p>");
            return html.ToString();
        }

        html.Append("<ul class=\"iq-entity-list__items\">");

        foreach (var entity in entities)
        {
            html.Append("<li class=\"iq-entity-list__item\" data-iq-item-type=\"entity\" data-iq-item-id=\"");
            html.Append(entity.Id);
            html.Append("\">");
            html.Append(RenderEntity(entity));
            html.Append("</li>");
        }

        html.Append("</ul>");
        return html.ToString();
    }

    public string RenderEntity(BusinessEntity entity)
    {
        var html = new StringBuilder();
        html.Append("<article class=\"iq-entity\"><div class=\"iq-entity__main\"><p class=\"iq-entity__type\">");
        html.Append(htmlEncoder.Encode(entity.EntityType));
        html.Append("</p><h3 class=\"iq-entity__title\">");
        html.Append(htmlEncoder.Encode(entity.Title));
        html.Append("</h3>");

        if (!string.IsNullOrWhiteSpace(entity.Description))
        {
            html.Append("<p class=\"iq-entity__description\">");
            html.Append(htmlEncoder.Encode(entity.Description));
            html.Append("</p>");
        }

        html.Append("<p class=\"iq-entity__meta\">");
        html.Append(htmlEncoder.Encode(entity.Status));

        if (!string.IsNullOrWhiteSpace(entity.MetadataJson))
        {
            html.Append(" · Metadata");
        }

        html.Append("</p>");
        html.Append(labelHtmlRenderer.RenderEntityLabels(entity.EntityLabels));

        var relationships = entity.SourceRelationships
            .Where(relationship => relationship.TargetEntity is not null)
            .OrderBy(relationship => relationship.RelationshipType)
            .ThenBy(relationship => relationship.TargetEntity!.Title)
            .ToList();

        if (relationships.Count > 0)
        {
            html.Append("<ul class=\"iq-entity__relationships\" aria-label=\"Relationships\">");

            foreach (var relationship in relationships)
            {
                html.Append("<li class=\"iq-entity__relationship\"><span>");
                html.Append(htmlEncoder.Encode(relationship.RelationshipType));
                html.Append("</span> ");
                html.Append(htmlEncoder.Encode(relationship.TargetEntity!.Title));
                html.Append("</li>");
            }

            html.Append("</ul>");
        }

        html.Append("</div></article>");
        return html.ToString();
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
            AppendActionButton(html, blockClass, "comment", "Comment", itemType, itemId, title, body);
        }

        if (interactions.AllowLabel)
        {
            AppendActionButton(html, blockClass, "label", "Label", itemType, itemId, title, body);
        }

        if (interactions.AllowEdit)
        {
            AppendActionButton(html, blockClass, "edit", "Edit", itemType, itemId, title, body);
        }

        if (interactions.AllowDelete)
        {
            AppendActionButton(html, blockClass, "delete", "Delete", itemType, itemId, title, body);
        }

        html.Append("</div>");
        return html.ToString();
    }

    private string RenderFileActions(
        ContentItem item,
        ComponentInteractionOptions interactions)
    {
        var html = new StringBuilder();
        html.Append("<div class=\"iq-files__item-actions\">");
        AppendFileDownloadButton(html, item.Id, item.Title);

        if (interactions.AllowComment)
        {
            AppendActionButton(html, "iq-files", "comment", "Comment", "content", item.Id, item.Title, item.Body);
        }

        if (interactions.AllowLabel)
        {
            AppendActionButton(html, "iq-files", "label", "Label", "content", item.Id, item.Title, item.Body);
        }

        if (interactions.AllowEdit)
        {
            AppendActionButton(html, "iq-files", "edit", "Edit", "content", item.Id, item.Title, item.Body);
        }

        if (interactions.AllowDelete)
        {
            AppendActionButton(html, "iq-files", "delete", "Delete", "content", item.Id, item.Title, item.Body);
        }

        html.Append("</div>");
        return html.ToString();
    }

    private void AppendFileDownloadButton(
        StringBuilder html,
        Guid itemId,
        string title)
    {
        html.Append("<a class=\"iq-files__item-action\" href=\"/Files/Download/");
        html.Append(itemId);
        html.Append("\" aria-label=\"Download ");
        html.Append(htmlEncoder.Encode(title));
        html.Append("\" title=\"Download\">");
        html.Append(RenderIcon("download"));
        html.Append("<span class=\"visually-hidden\">Download</span></a>");
    }

    private void AppendActionButton(
        StringBuilder html,
        string blockClass,
        string action,
        string label,
        string itemType,
        Guid itemId,
        string title,
        string? body)
    {
        html.Append("<button type=\"button\" class=\"");
        html.Append(blockClass);
        html.Append("__item-action\" data-iq-action=\"");
        html.Append(action);
        html.Append("\" data-iq-item-type=\"");
        html.Append(itemType);
        html.Append("\" data-iq-item-id=\"");
        html.Append(itemId);
        html.Append("\" data-iq-item-title=\"");
        html.Append(htmlEncoder.Encode(title));
        html.Append("\" data-iq-item-body=\"");
        html.Append(htmlEncoder.Encode(body ?? string.Empty));
        html.Append("\" aria-label=\"");
        html.Append(label);
        html.Append("\" title=\"");
        html.Append(label);
        html.Append("\">");
        html.Append(RenderIcon(action));
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
            "label" => "<path d=\"M3 11.5V4h7.5L21 14.5 14.5 21 4 10.5Z\"/><path d=\"M7.5 7.5h.01\"/>",
            "edit" => "<path d=\"M12 20h9\"/><path d=\"M16.5 3.5a2.1 2.1 0 0 1 3 3L7 19l-4 1 1-4Z\"/>",
            "delete" => "<path d=\"M4 7h16\"/><path d=\"M10 11v6\"/><path d=\"M14 11v6\"/><path d=\"M6 7l1 14h10l1-14\"/><path d=\"M9 7V4h6v3\"/>",
            "download" => "<path d=\"M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4\"/><path d=\"M7 10l5 5 5-5\"/><path d=\"M12 15V3\"/>",
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

    private static string GetFileExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return string.IsNullOrWhiteSpace(extension)
            ? "FILE"
            : extension.TrimStart('.').ToUpperInvariant();
    }

    private static string FormatSize(long sizeBytes)
    {
        if (sizeBytes < 1024)
        {
            return $"{sizeBytes} B";
        }

        if (sizeBytes < 1024 * 1024)
        {
            return $"{sizeBytes / 1024d:0.#} KB";
        }

        return $"{sizeBytes / 1024d / 1024d:0.#} MB";
    }

    private string RenderFileIcon(string fileName)
    {
        var extension = GetFileExtension(fileName);
        return $"<span class=\"iq-files__file-ext\">{htmlEncoder.Encode(extension)}</span>";
    }
}
