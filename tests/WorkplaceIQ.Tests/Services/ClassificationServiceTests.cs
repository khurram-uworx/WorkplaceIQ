using WorkplaceIQ.Content;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Tests.TestDoubles;

namespace WorkplaceIQ.Tests.Services;

public class ClassificationServiceTests
{
    [Test]
    public async Task UpsertClassifiedItemAsync_ClassifySameContentAsDifferentLabels_LastLabelWins()
    {
        var store = new InMemoryWorkplaceIqStore();

        var item = new ContentItem
        {
            Name = "test-item",
            Title = "Test",
            Body = "Test body",
            Discriminator = "RssItem",
            Status = "active"
        };
        await store.CreateItemAsync(item);

        var labelA = new Label { Name = "A", NormalizedName = "a", Slug = "a" };
        var labelB = new Label { Name = "B", NormalizedName = "b", Slug = "b" };
        await store.CreateLabelAsync(labelA);
        await store.CreateLabelAsync(labelB);

        var first = new ClassifiedItem
        {
            ContentId = item.Id,
            LabelId = labelA.Id,
            ClassificationSource = "Test",
            ClassifiedAt = DateTime.UtcNow
        };
        await store.UpsertClassifiedItemAsync(first);

        var second = new ClassifiedItem
        {
            ContentId = item.Id,
            LabelId = labelB.Id,
            ClassificationSource = "Test",
            ClassifiedAt = DateTime.UtcNow
        };
        await store.UpsertClassifiedItemAsync(second);

        var result = await store.GetClassifiedByContentIdAsync(item.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.LabelId, Is.EqualTo(labelB.Id));
    }

    [Test]
    public async Task UpsertClassifiedItemAsync_ClassifyDifferentContent_EachGetsOwnClassification()
    {
        var store = new InMemoryWorkplaceIqStore();

        var itemA = new ContentItem { Name = "a", Title = "A", Discriminator = "RssItem", Status = "active" };
        var itemB = new ContentItem { Name = "b", Title = "B", Discriminator = "RssItem", Status = "active" };
        await store.CreateItemAsync(itemA);
        await store.CreateItemAsync(itemB);

        var label = new Label { Name = "X", NormalizedName = "x", Slug = "x" };
        await store.CreateLabelAsync(label);

        await store.UpsertClassifiedItemAsync(new ClassifiedItem
        {
            ContentId = itemA.Id,
            LabelId = label.Id,
            ClassificationSource = "Test",
            ClassifiedAt = DateTime.UtcNow
        });
        await store.UpsertClassifiedItemAsync(new ClassifiedItem
        {
            ContentId = itemB.Id,
            LabelId = label.Id,
            ClassificationSource = "Test",
            ClassifiedAt = DateTime.UtcNow
        });

        Assert.That(store.ClassifiedItems, Has.Count.EqualTo(2));
        Assert.That(store.ClassifiedItems.Count(c => c.LabelId == label.Id), Is.EqualTo(2));
    }

    [Test]
    public async Task UpsertClassifiedItemAsync_UpdatesPreserveOriginalId()
    {
        var store = new InMemoryWorkplaceIqStore();

        var item = new ContentItem { Name = "test", Title = "Test", Discriminator = "RssItem", Status = "active" };
        await store.CreateItemAsync(item);

        var labelA = new Label { Name = "A", NormalizedName = "a", Slug = "a" };
        var labelB = new Label { Name = "B", NormalizedName = "b", Slug = "b" };
        await store.CreateLabelAsync(labelA);
        await store.CreateLabelAsync(labelB);

        var originalId = Guid.NewGuid();
        await store.UpsertClassifiedItemAsync(new ClassifiedItem
        {
            Id = originalId,
            ContentId = item.Id,
            LabelId = labelA.Id,
            ClassificationSource = "Test",
            ClassifiedAt = DateTime.UtcNow
        });

        await store.UpsertClassifiedItemAsync(new ClassifiedItem
        {
            Id = Guid.NewGuid(),
            ContentId = item.Id,
            LabelId = labelB.Id,
            ClassificationSource = "Test",
            ClassifiedAt = DateTime.UtcNow
        });

        var result = await store.GetClassifiedByContentIdAsync(item.Id);
        Assert.That(result!.Id, Is.EqualTo(originalId));
    }
}
