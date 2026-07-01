using Microsoft.AspNetCore.Mvc;
using WorkplaceIQ.Content;
using WorkplaceIQ.Labels;

namespace WorkplaceIQ.Web.Controllers;

[AutoValidateAntiforgeryToken]
public sealed class ContentController(IWorkplaceIqStore store) : Controller
{
    [HttpPost]
    public async Task<IActionResult> AddComment(
        string itemType,
        Guid itemId,
        string body,
        string? returnUrl)
    {
        if (itemType != "content" || string.IsNullOrWhiteSpace(body))
        {
            return RedirectToLocal(returnUrl);
        }

        var item = await store.GetItemByIdAsync(itemId);
        if (item is null)
        {
            return RedirectToLocal(returnUrl);
        }

        var now = DateTime.UtcNow;
        var comment = new ContentItem
        {
            ContainerId = item.ContainerId,
            Discriminator = "comment",
            Title = "Comment",
            Body = body.Trim(),
            Status = "active",
            CreatedAt = now,
            ModifiedAt = now
        };
        await store.CreateItemAsync(comment);

        TempData["ItemActionMessage"] = "Comment added.";
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> AddLabel(
        string itemType,
        Guid itemId,
        string label,
        string? returnUrl)
    {
        var parsed = LabelName.ParseList(label).FirstOrDefault();
        if (parsed is null)
        {
            return RedirectToLocal(returnUrl);
        }

        if (itemType == "content")
        {
            await store.AddLabelToContentAsync(itemId, parsed);
        }
        else if (itemType == "post")
        {
            await store.AddLabelToItemAsync(itemId, parsed);
        }

        TempData["ItemActionMessage"] = "Label added.";
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(
        string itemType,
        Guid itemId,
        string title,
        string? body,
        string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return RedirectToLocal(returnUrl);
        }

        if (itemType == "content")
        {
            var item = await store.GetItemByIdAsync(itemId);
            if (item is not null)
            {
                item.Title = title.Trim();
                item.Body = body?.Trim();
                item.ModifiedAt = DateTime.UtcNow;
                await store.UpdateItemAsync(item);
            }
        }
        else if (itemType == "post")
        {
            var item = await store.GetItemByIdAsync(itemId);
            if (item is not null)
            {
                item.Title = title.Trim();
                item.Body = body?.Trim();
                item.ModifiedAt = DateTime.UtcNow;
                await store.UpdateItemAsync(item);
            }
        }

        TempData["ItemActionMessage"] = "Item updated.";
        return RedirectToLocal(returnUrl);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(
        string itemType,
        Guid itemId,
        string? returnUrl)
    {
        await store.DeleteItemAsync(itemId);

        TempData["ItemActionMessage"] = "Item deleted.";
        return RedirectToLocal(returnUrl);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }
}
