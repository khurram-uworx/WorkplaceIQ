namespace WorkplaceIQ.Tests.Entities;

using WorkplaceIQ.Components;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Entities;
using WorkplaceIQ.Tests.TestDoubles;

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
        Assert.That(store.Containers.Single().Type, Is.EqualTo(ContainerTypes.EntityList));
    }

    [Test]
    public async Task CreateEntityAsync_PersistsMetadataAndLabels()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = CreateService(store);
        await store.CreateContainerAsync("Machines", ContainerTypes.EntityList, "Machines");

        var entity = await service.CreateEntityAsync(new EntityCreateRequest(
            "Machines",
            "Machine",
            "press-12",
            "Press 12",
            "Hydraulic press",
            MetadataJson: """{"floor":"A"}""",
            Labels: "Production, Critical"));

        Assert.That(entity.EntityType, Is.EqualTo("Machine"));
        Assert.That(entity.Name, Is.EqualTo("press-12"));
        Assert.That(entity.MetadataJson, Is.EqualTo("""{"floor":"A"}"""));
        Assert.That(entity.EntityLabels.Select(label => label.Label!.Slug), Is.EquivalentTo(new[] { "production", "critical" }));
        Assert.That(store.Labels, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task ResolveEntitiesAsync_ReturnsEntitiesForSelectedList()
    {
        var store = new InMemoryWorkplaceIqStore();
        var service = CreateService(store);
        var machines = await store.CreateContainerAsync("Machines", ContainerTypes.EntityList, "Machines");
        var customers = await store.CreateContainerAsync("Customers", ContainerTypes.EntityList, "Customers");
        await store.CreateEntityAsync(new BusinessEntity { ContainerId = machines.Id, EntityType = "Machine", Name = "press-12", Title = "Press 12" }, []);
        await store.CreateEntityAsync(new BusinessEntity { ContainerId = customers.Id, EntityType = "Customer", Name = "acme", Title = "Acme" }, []);

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
        var container = await store.CreateContainerAsync("Machines", ContainerTypes.EntityList, "Machines");
        var source = await store.CreateEntityAsync(new BusinessEntity { ContainerId = container.Id, EntityType = "Machine", Name = "line-1", Title = "Line 1" }, []);
        var target = await store.CreateEntityAsync(new BusinessEntity { ContainerId = container.Id, EntityType = "Machine", Name = "press-12", Title = "Press 12" }, []);

        var relationship = await service.CreateRelationshipAsync(source.Id, target.Id, "contains", """{"slot":1}""");

        Assert.That(relationship.SourceEntityId, Is.EqualTo(source.Id));
        Assert.That(relationship.TargetEntityId, Is.EqualTo(target.Id));
        Assert.That(relationship.RelationshipType, Is.EqualTo("contains"));
        Assert.That(relationship.MetadataJson, Is.EqualTo("""{"slot":1}"""));
        Assert.That(store.EntityRelationships, Has.Count.EqualTo(1));
    }

    private static EntityComponentService CreateService(InMemoryWorkplaceIqStore store)
    {
        return new EntityComponentService(new ComponentService(store), store);
    }
}
