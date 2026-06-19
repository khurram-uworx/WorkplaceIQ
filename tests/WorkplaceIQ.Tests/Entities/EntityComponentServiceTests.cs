using WorkplaceIQ.Components;
using WorkplaceIQ.Content;
using WorkplaceIQ.Entities;
using WorkplaceIQ.Tests.TestDoubles;

namespace WorkplaceIQ.Tests.Entities;

public class EntityComponentServiceTests
{
    [Test]
    public async Task ResolveEntitiesAsync_AutoProvisionsEntityListInDevelopment()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = CreateService(store);

        var result = await service.ResolveEntitiesAsync(new EntityComponentRequest(
            "Machines",
            "Machines",
            "Machine",
            true));

        Assert.That(result.Created, Is.True);
        Assert.That(result.DisplayTitle, Is.EqualTo("Machines"));
        Assert.That(result.EntityType, Is.EqualTo("Machine"));
        Assert.That(store.Contents.Single().ContentType, Is.EqualTo(ContentTypes.EntityContainer));
    }

    [Test]
    public async Task CreateEntityAsync_PersistsMetadataAndLabels()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = CreateService(store);
        await store.CreateContentAsync(new Content.Content
        {
            Name = "Machines",
            ContentType = ContentTypes.EntityContainer,
            Title = "Machines"
        });

        var entity = await service.CreateEntityAsync(new EntityCreateRequest(
            "Machines",
            "Machine",
            "press-12",
            "Press 12",
            "Hydraulic press",
            MetadataJson: """{"floor":"A"}""",
            Labels: "Production, Critical"));

        Assert.That(entity.ContentType, Is.EqualTo("Machine"));
        Assert.That(entity.Name, Is.EqualTo("press-12"));
        Assert.That(entity.MetadataJson, Is.EqualTo("""{"floor":"A"}"""));
        Assert.That(entity.ContentLabels.Select(label => label.Label!.Slug), Is.EquivalentTo(new[] { "production", "critical" }));
        Assert.That(store.Labels, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ResolveEntitiesAsync_ReturnsEntitiesForSelectedList()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = CreateService(store);
        var machines = await store.CreateContentAsync(new Content.Content
        {
            Name = "Machines",
            ContentType = ContentTypes.EntityContainer,
            Title = "Machines"
        });
        var customers = await store.CreateContentAsync(new Content.Content
        {
            Name = "Customers",
            ContentType = ContentTypes.EntityContainer,
            Title = "Customers"
        });
        await store.CreateContentAsync(new Content.Content
        {
            ParentId = machines.Id,
            ContentType = "Machine",
            Name = "press-12",
            Title = "Press 12"
        });
        await store.CreateContentAsync(new Content.Content
        {
            ParentId = customers.Id,
            ContentType = "Customer",
            Name = "acme",
            Title = "Acme"
        });

        var result = await service.ResolveEntitiesAsync(new EntityComponentRequest(
            "Machines",
            "Machines",
            "Machine",
            true));

        Assert.That(result.Entities.Select(entity => entity.Title), Is.EquivalentTo(new[] { "Press 12" }));
    }

    [Test]
    public async Task CreateRelationshipAsync_CreatesExplicitRelationshipBetweenEntities()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = CreateService(store);
        var container = await store.CreateContentAsync(new Content.Content
        {
            Name = "Machines",
            ContentType = ContentTypes.EntityContainer,
            Title = "Machines"
        });
        var source = await store.CreateContentAsync(new Content.Content
        {
            ParentId = container.Id,
            ContentType = "Machine",
            Name = "line-1",
            Title = "Line 1"
        });
        var target = await store.CreateContentAsync(new Content.Content
        {
            ParentId = container.Id,
            ContentType = "Machine",
            Name = "press-12",
            Title = "Press 12"
        });

        var relationship = await service.CreateRelationshipAsync(source.Id, target.Id, "contains", """{"slot":1}""");

        Assert.That(relationship.SourceContentId, Is.EqualTo(source.Id));
        Assert.That(relationship.TargetContentId, Is.EqualTo(target.Id));
        Assert.That(relationship.RelationshipType, Is.EqualTo("contains"));
        Assert.That(relationship.MetadataJson, Is.EqualTo("""{"slot":1}"""));
        Assert.That(store.ContentRelationships, Has.Count.EqualTo(1));
    }

    private static EntityComponentService CreateService(InMemoryWorkplaceIqStore store)
    {
        return new EntityComponentService(new ComponentService(store), store);
    }
}
