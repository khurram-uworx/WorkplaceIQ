using WorkplaceIQ.Content;
using WorkplaceIQ.Tests.TestDoubles;

namespace WorkplaceIQ.Tests.Services;

public class ContentServiceTests
{
    [Test]
    public async Task CreateAsync_CreatesPublishedContent()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = new ContentService(store);
        var parentId = Guid.NewGuid();

        var item = await service.CreateAsync(
            parentId,
            " Outage ",
            " generator-3-outage ",
            " Generator 3 outage ",
            " Generator 3 lost power. ",
            authorUserId: " system ",
            metadataJson: """{"durationSeconds":5400}""");

        Assert.That(item.ParentId, Is.EqualTo(parentId));
        Assert.That(item.ContentType, Is.EqualTo("Outage"));
        Assert.That(item.Name, Is.EqualTo("generator-3-outage"));
        Assert.That(item.Title, Is.EqualTo("Generator 3 outage"));
        Assert.That(item.Body, Is.EqualTo("Generator 3 lost power."));
        Assert.That(item.AuthorUserId, Is.EqualTo("system"));
        Assert.That(item.PublishedAt, Is.Not.Null);
        Assert.That(store.Contents, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetByParentAsync_ReturnsOnlyParentContent()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = new ContentService(store);
        var parentId = Guid.NewGuid();
        var otherParentId = Guid.NewGuid();
        store.Contents.Add(new Content.Content { ParentId = parentId, ContentType = "Outage", Name = "one", Title = "One" });
        store.Contents.Add(new Content.Content { ParentId = otherParentId, ContentType = "Outage", Name = "two", Title = "Two" });

        var items = await service.GetByParentAsync(parentId);

        Assert.That(items.Select(item => item.Title), Is.EquivalentTo(new[] { "One" }));
    }

    [Test]
    public void UpdateAsync_RejectsMissingContent()
    {
        var service = new ContentService(new InMemoryWorkplaceIqStore());

        var exception = Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(Guid.NewGuid(), title: "Updated"));

        Assert.That(exception!.Message, Does.Contain("not found"));
    }
}
