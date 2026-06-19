using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
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

    public IActionResult News()
    {
        return View();
    }

    public IActionResult Incidents()
    {
        return View();
    }

    public IActionResult Discussions()
    {
        return View();
    }

    public IActionResult Documents()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFeedPost(
        string feedId,
        string title,
        string body,
        string? labels,
        string? returnUrl)
    {
        try
        {
            await feedComponentService.CreatePostAsync(feedId, title, body, labels);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return RedirectToLocal(returnUrl, nameof(News));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return RedirectToLocal(returnUrl, nameof(News));
        }

        TempData["FeedPostCreated"] = "Feed item added.";
        return RedirectToLocal(returnUrl, nameof(News));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateForumThread(
        string forumId,
        string title,
        string body,
        string? labels,
        string? returnUrl)
    {
        try
        {
            await forumComponentService.CreateThreadAsync(forumId, title, body, labels);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return RedirectToLocal(returnUrl, nameof(Discussions));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return RedirectToLocal(returnUrl, nameof(Discussions));
        }

        TempData["ForumThreadCreated"] = "Forum thread added.";
        return RedirectToLocal(returnUrl, nameof(Discussions));
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

    private IActionResult RedirectToLocal(string? returnUrl, string defaultAction)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction(defaultAction);
    }
}
