using Microsoft.AspNetCore.Mvc;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Posts;

namespace WorkplaceIQ.Web.Controllers;

[AutoValidateAntiforgeryToken]
public sealed class ContentController(IWorkplaceIqStore store) : Controller
{
    [HttpPost]
    public async Task<IActionResult> AddComment(
        string itemType,
        Guid itemId,
        string body)
    {
        if (itemType != "content" || string.IsNullOrWhiteSpace(body))
        {
            return RedirectToAction("Index", "Home");
        }

        var item = await store.GetContentByIdAsync(itemId);
        if (item is null)
        {
            return RedirectToAction("Index", "Home");
        }

        await store.CreatePostAsync(
            item.ParentId!.Value,
            "Comment",
            body.Trim(),
            [],
            contentId: item.Id,
            postType: PostTypes.Comment);

        TempData["ItemActionMessage"] = "Comment added.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> AddLabel(
        string itemType,
        Guid itemId,
        string label)
    {
        var parsed = LabelName.ParseList(label).FirstOrDefault();
        if (parsed is null)
        {
            return RedirectToAction("Index", "Home");
        }

        if (itemType == "content")
        {
            await store.AddLabelToContentAsync(itemId, parsed);
        }
        else if (itemType == "post")
        {
            await store.AddLabelToPostAsync(itemId, parsed);
        }

        TempData["ItemActionMessage"] = "Label added.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Edit(
        string itemType,
        Guid itemId,
        string title,
        string? body)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return RedirectToAction("Index", "Home");
        }

        if (itemType == "content")
        {
            var item = await store.GetContentByIdAsync(itemId);
            if (item is not null)
            {
                item.Title = title.Trim();
                item.Body = body?.Trim();
                item.UpdatedAt = DateTimeOffset.UtcNow;
                await store.UpdateContentAsync(item);
            }
        }
        else if (itemType == "post")
        {
            var post = await store.GetPostByIdAsync(itemId);
            if (post is not null)
            {
                post.Title = title.Trim();
                post.Body = body?.Trim() ?? string.Empty;
                await store.UpdatePostAsync(post);
            }
        }

        TempData["ItemActionMessage"] = "Item updated.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Delete(
        string itemType,
        Guid itemId)
    {
        if (itemType == "content")
        {
            await store.DeleteContentAsync(itemId);
        }
        else if (itemType == "post")
        {
            await store.DeletePostAsync(itemId);
        }

        TempData["ItemActionMessage"] = "Item deleted.";
        return RedirectToAction("Index", "Home");
    }
}
