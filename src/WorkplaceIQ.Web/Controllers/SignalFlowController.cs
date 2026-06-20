using Microsoft.AspNetCore.Mvc;
using WorkplaceIQ.Web.SignalFlow.Models;
using WorkplaceIQ.Web.SignalFlow.Services;

namespace WorkplaceIQ.Web.Controllers;

[Route("signalflow")]
public sealed class SignalFlowController(
    IWorkplaceIqStore store,
    IFeedbackService feedbackService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var signalCounts = await feedbackService.GetSignalCountsAsync(ct);
        var sidebarItems = new List<SignalCountItem>();
        foreach (var (name, count) in signalCounts)
        {
            var label = await store.GetLabelByNameAsync(name, ct);
            if (label is not null)
                sidebarItems.Add(new SignalCountItem(label.Id, name, count));
        }

        var noiseCount = await feedbackService.GetNoiseCountAsync(ct);
        var bouncedCount = await feedbackService.GetFailedCountAsync(ct);
        var recent = await store.GetRecentClassifiedItemsAsync(20, ct);

        ViewData["SignalCounts"] = sidebarItems;
        ViewData["NoiseCount"] = noiseCount;
        ViewData["BouncedCount"] = bouncedCount;
        ViewData["RecentItems"] = recent;

        return View();
    }

    [HttpGet("signals")]
    public async Task<IActionResult> Signals(CancellationToken ct)
    {
        var groups = await feedbackService.GetSignalsAsync(ct);
        return View(groups);
    }

    [HttpGet("signals/{signalName}")]
    public async Task<IActionResult> Signal(string signalName, int page = 0, CancellationToken ct = default)
    {
        var label = await store.GetLabelByNameAsync(signalName, ct);
        if (label is null)
            return NotFound();

        const int pageSize = 50;
        var items = await store.GetClassifiedItemsByLabelAsync(label.Id, page * pageSize, pageSize, ct);

        ViewData["SignalLabel"] = label;
        ViewData["Page"] = page;
        ViewData["PageSize"] = pageSize;
        return View(items);
    }

    [HttpGet("item/{id:guid}")]
    public async Task<IActionResult> Item(Guid id, CancellationToken ct = default)
    {
        var item = await store.GetClassifiedItemByIdAsync(id, ct);
        if (item is null) return NotFound();

        var similar = await feedbackService.MoreLikeAsync(id, 6, ct);
        ViewData["SimilarItems"] = similar;
        return View(item);
    }

    [HttpGet("noise")]
    public async Task<IActionResult> Noise(CancellationToken ct = default)
    {
        var items = await feedbackService.GetNoiseAsync(ct);
        return View(items);
    }

    [HttpGet("bounced")]
    public async Task<IActionResult> Bounced(CancellationToken ct = default)
    {
        var items = await feedbackService.GetBouncedAsync(ct);
        return View(items);
    }
}
