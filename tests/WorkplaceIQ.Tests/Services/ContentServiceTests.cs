namespace WorkplaceIQ.Tests.Services;

using WorkplaceIQ.Content;
using WorkplaceIQ.Tests.TestDoubles;

public class ContentServiceTests
{
    [Test]
    public async Task CreateAsync_CreatesPublishedContent()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = new ContentService(store);
        var containerId = Guid.NewGuid();

        var item = await service.CreateAsync(
            containerId,
            " Outage ",
            " generator-3-outage ",
            " Generator 3 outage ",
            " Generator 3 lost power. ",
            authorUserId: " system ",
            metadataJson: """{"durationSeconds":5400}""");

        Assert.That(item.ContainerId, Is.EqualTo(containerId));
        Assert.That(item.ContentType, Is.EqualTo("Outage"));
        Assert.That(item.Name, Is.EqualTo("generator-3-outage"));
        Assert.That(item.Title, Is.EqualTo("Generator 3 outage"));
        Assert.That(item.Body, Is.EqualTo("Generator 3 lost power."));
        Assert.That(item.AuthorUserId, Is.EqualTo("system"));
        Assert.That(item.Status, Is.EqualTo("published"));
        Assert.That(item.PublishedAt, Is.Not.Null);
        Assert.That(store.ContentItems, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetByContainerAsync_ReturnsOnlyContainerContent()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = new ContentService(store);
        var containerId = Guid.NewGuid();
        var otherContainerId = Guid.NewGuid();
        store.ContentItems.Add(new ContentItem { ContainerId = containerId, ContentType = "Outage", Name = "one", Title = "One" });
        store.ContentItems.Add(new ContentItem { ContainerId = otherContainerId, ContentType = "Outage", Name = "two", Title = "Two" });

        var items = await service.GetByContainerAsync(containerId);

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
