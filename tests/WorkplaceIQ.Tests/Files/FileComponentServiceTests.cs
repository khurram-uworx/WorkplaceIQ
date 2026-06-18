namespace WorkplaceIQ.Tests.Files;

using System.Text;
using WorkplaceIQ.Components;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Files;
using WorkplaceIQ.Tests.TestDoubles;

public class FileComponentServiceTests
{
    [Test]
    public async Task ResolveFilesAsync_AutoProvisionsFilesContainerInDevelopment()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = CreateService(store, new InMemoryFileObjectStorage());

        var result = await service.ResolveFilesAsync(new FileComponentRequest(
            "HRDocs",
            "HR Documents",
            true));

        Assert.That(result.Created, Is.True);
        Assert.That(result.DisplayTitle, Is.EqualTo("HR Documents"));
        Assert.That(store.Containers.Single().Key, Is.EqualTo("HRDocs"));
        Assert.That(store.Containers.Single().Type, Is.EqualTo(ContainerTypes.Files));
    }

    [Test]
    public async Task UploadAsync_CreatesFileContentAndStoresObject()
    {
        var store = new InMemoryWorkplaceIqStore();
        var storage = new InMemoryFileObjectStorage();
        var service = CreateService(store, storage);
        await store.CreateContainerAsync("HRDocs", ContainerTypes.Files, "HR Documents");
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("policy"));

        var file = await service.UploadAsync(new FileUploadRequest(
            "HRDocs",
            "Leave Policy.pdf",
            "application/pdf",
            content.Length,
            content,
            Title: "Leave Policy",
            Description: "Annual leave rules",
            Labels: "HR, Policy"));

        Assert.That(storage.BucketEnsured, Is.True);
        Assert.That(file.ContentItem.ContentType, Is.EqualTo(FileContentTypes.File));
        Assert.That(file.ContentItem.Title, Is.EqualTo("Leave Policy"));
        Assert.That(file.ContentItem.Body, Is.EqualTo("Annual leave rules"));
        Assert.That(file.FileRecord.FileName, Is.EqualTo("Leave Policy.pdf"));
        Assert.That(file.FileRecord.BucketName, Is.EqualTo("test-files"));
        Assert.That(file.FileRecord.ObjectKey, Does.Contain("containers/HRDocs/content/"));
        Assert.That(storage.Objects[file.FileRecord.ObjectKey], Is.EqualTo(Encoding.UTF8.GetBytes("policy")));
        Assert.That(file.ContentItem.ContentLabels.Select(label => label.Label!.Slug), Is.EquivalentTo(new[] { "hr", "policy" }));
    }

    [Test]
    public async Task ResolveFilesAsync_ReturnsOnlyFilesForSelectedContainer()
    {
        var store = new InMemoryWorkplaceIqStore();
        var storage = new InMemoryFileObjectStorage();
        var service = CreateService(store, storage);
        await store.CreateContainerAsync("HRDocs", ContainerTypes.Files, "HR Documents");
        await store.CreateContainerAsync("ITDocs", ContainerTypes.Files, "IT Documents");

        await UploadTextAsync(service, "HRDocs", "Leave.pdf");
        await UploadTextAsync(service, "ITDocs", "Vpn.pdf");

        var result = await service.ResolveFilesAsync(new FileComponentRequest("HRDocs", "HR Documents", true));

        Assert.That(result.Files.Select(file => file.FileRecord.FileName), Is.EquivalentTo(new[] { "Leave.pdf" }));
    }

    [Test]
    public async Task OpenReadAsync_ReturnsStoredObjectStream()
    {
        var store = new InMemoryWorkplaceIqStore();
        var storage = new InMemoryFileObjectStorage();
        var service = CreateService(store, storage);
        await store.CreateContainerAsync("HRDocs", ContainerTypes.Files, "HR Documents");
        var uploaded = await UploadTextAsync(service, "HRDocs", "Leave.pdf", "stored");

        await using var stream = await service.OpenReadAsync(uploaded.ContentItem.Id);
        using var reader = new StreamReader(stream);

        Assert.That(await reader.ReadToEndAsync(), Is.EqualTo("stored"));
    }

    private static async Task<FileObject> UploadTextAsync(
        FileComponentService service,
        string filesId,
        string fileName,
        string text = "content")
    {
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes(text));
        return await service.UploadAsync(new FileUploadRequest(
            filesId,
            fileName,
            "application/pdf",
            content.Length,
            content));
    }

    private static FileComponentService CreateService(
        InMemoryWorkplaceIqStore store,
        InMemoryFileObjectStorage storage)
    {
        return new FileComponentService(new ComponentService(store), store, storage);
    }
}
