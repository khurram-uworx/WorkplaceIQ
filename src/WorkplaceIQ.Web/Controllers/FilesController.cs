using Microsoft.AspNetCore.Mvc;
using WorkplaceIQ.Files;

namespace WorkplaceIQ.Web.Controllers;

[AutoValidateAntiforgeryToken]
public sealed class FilesController(IFileComponentService fileComponentService) : Controller
{
    [HttpPost]
    public async Task<IActionResult> Upload(
        string filesId,
        string? title,
        string? description,
        string? labels,
        string? returnUrl,
        IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["FilesMessage"] = "Choose a non-empty file.";
            return RedirectToLocal(returnUrl);
        }

        await using var stream = file.OpenReadStream();
        await fileComponentService.UploadAsync(new FileUploadRequest(
            filesId,
            file.FileName,
            file.ContentType,
            file.Length,
            stream,
            title,
            description,
            labels));

        TempData["FilesMessage"] = "File uploaded.";
        return RedirectToLocal(returnUrl);
    }

    [HttpGet]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Download(Guid id)
    {
        var file = await fileComponentService.GetFileAsync(id);
        if (file is null)
        {
            return NotFound();
        }

        var stream = await fileComponentService.OpenReadAsync(id);
        return File(stream, file.ContentFile.ContentType, file.ContentFile.FileName);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Documents", "Home");
    }
}
