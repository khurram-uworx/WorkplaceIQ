using WorkplaceIQ.Content;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Tests.TestDoubles;

namespace WorkplaceIQ.Tests.Services;

/// <summary>
/// Tests for the classification invariant: one ClassifiedItem per ContentId, last label wins.
/// These tests serve as the regression safety net for ADR 02 (unified polymorphic content model).
/// When Content is refactored into Container/ContentItem, these tests must still pass with the
/// updated store implementation — they validate the behavioral contract, not the entity shape.
/// </summary>
public class ClassificationServiceTests
{
    [Test]
    public async Task UpsertClassifiedItemAsync_ClassifySameContentAsDifferentLabels_LastLabelWins()
    {
        var store = new InMemoryWorkplaceIqStore();

        var content = new Content.Content
        {
            Name = "test-item",
            Title = "Test",
            Body = "Test body",
            ContentType = "RssItem",
            Status = "active"
        };
        await store.CreateContentAsync(content);

        var labelA = new Label { Name = "A", NormalizedName = "a", Slug = "a" };
        var labelB = new Label { Name = "B", NormalizedName = "b", Slug = "b" };
        await store.CreateLabelAsync(labelA);
        await store.CreateLabelAsync(labelB);

        var first = new ClassifiedItem
        {
            ContentId = content.Id,
            LabelId = labelA.Id,
            ClassificationSource = "Test",
            ClassifiedAt = DateTime.UtcNow
        };
        await store.UpsertClassifiedItemAsync(first);

        var second = new ClassifiedItem
        {
            ContentId = content.Id,
            LabelId = labelB.Id,
            ClassificationSource = "Test",
            ClassifiedAt = DateTime.UtcNow
        };
        await store.UpsertClassifiedItemAsync(second);

        var result = await store.GetClassifiedByContentIdAsync(content.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.LabelId, Is.EqualTo(labelB.Id));
    }

    [Test]
    public async Task UpsertClassifiedItemAsync_ClassifyDifferentContent_EachGetsOwnClassification()
    {
        var store = new InMemoryWorkplaceIqStore();

        var contentA = new Content.Content { Name = "a", Title = "A", ContentType = "RssItem", Status = "active" };
        var contentB = new Content.Content { Name = "b", Title = "B", ContentType = "RssItem", Status = "active" };
        await store.CreateContentAsync(contentA);
        await store.CreateContentAsync(contentB);

        var label = new Label { Name = "X", NormalizedName = "x", Slug = "x" };
        await store.CreateLabelAsync(label);

        await store.UpsertClassifiedItemAsync(new ClassifiedItem
        {
            ContentId = contentA.Id, LabelId = label.Id,
            ClassificationSource = "Test", ClassifiedAt = DateTime.UtcNow
        });
        await store.UpsertClassifiedItemAsync(new ClassifiedItem
        {
            ContentId = contentB.Id, LabelId = label.Id,
            ClassificationSource = "Test", ClassifiedAt = DateTime.UtcNow
        });

        Assert.That(store.ClassifiedItems, Has.Count.EqualTo(2));
        Assert.That(store.ClassifiedItems.Count(c => c.LabelId == label.Id), Is.EqualTo(2));
    }

    [Test]
    public async Task UpsertClassifiedItemAsync_UpdatesPreserveOriginalId()
    {
        var store = new InMemoryWorkplaceIqStore();

        var content = new Content.Content { Name = "test", Title = "Test", ContentType = "RssItem", Status = "active" };
        await store.CreateContentAsync(content);

        var labelA = new Label { Name = "A", NormalizedName = "a", Slug = "a" };
        var labelB = new Label { Name = "B", NormalizedName = "b", Slug = "b" };
        await store.CreateLabelAsync(labelA);
        await store.CreateLabelAsync(labelB);

        var originalId = Guid.NewGuid();
        await store.UpsertClassifiedItemAsync(new ClassifiedItem
        {
            Id = originalId,
            ContentId = content.Id,
            LabelId = labelA.Id,
            ClassificationSource = "Test",
            ClassifiedAt = DateTime.UtcNow
        });

        await store.UpsertClassifiedItemAsync(new ClassifiedItem
        {
            Id = Guid.NewGuid(),
            ContentId = content.Id,
            LabelId = labelB.Id,
            ClassificationSource = "Test",
            ClassifiedAt = DateTime.UtcNow
        });

        var result = await store.GetClassifiedByContentIdAsync(content.Id);
        Assert.That(result!.Id, Is.EqualTo(originalId));
    }
}
