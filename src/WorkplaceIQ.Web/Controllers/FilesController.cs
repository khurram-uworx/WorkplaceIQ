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
        IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["FilesMessage"] = "Choose a non-empty file.";
            return RedirectToAction("Index", "Home");
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
        return RedirectToAction("Index", "Home");
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
        return File(stream, file.FileRecord.ContentType, file.FileRecord.FileName);
    }
}
