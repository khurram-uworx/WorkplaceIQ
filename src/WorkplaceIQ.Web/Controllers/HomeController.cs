using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WorkplaceIQ.Feeds;
using WorkplaceIQ.Forums;
using WorkplaceIQ.Web.Models;

namespace WorkplaceIQ.Web.Controllers;

public class HomeController(
    IFeedComponentService feedComponentService,
    IForumComponentService forumComponentService) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFeedPost(
        string feedId,
        string title,
        string body,
        string? labels)
    {
        try
        {
            await feedComponentService.CreatePostAsync(feedId, title, body, labels);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(nameof(Index));
        }

        TempData["FeedPostCreated"] = "Feed item added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateForumThread(
        string forumId,
        string title,
        string body,
        string? labels)
    {
        try
        {
            await forumComponentService.CreateThreadAsync(forumId, title, body, labels);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(nameof(Index));
        }

        TempData["ForumThreadCreated"] = "Forum thread added.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
