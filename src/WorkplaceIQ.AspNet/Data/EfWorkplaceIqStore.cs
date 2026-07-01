using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using WorkplaceIQ.Content;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Metrics;

namespace WorkplaceIQ.AspNet.Data;

public sealed class EfWorkplaceIqStore(IDbContextFactory<WorkplaceIqDbContext> dbContextFactory) : IWorkplaceIqStore
{
    // ── Container CRUD ──────────────────────────────────────────────

    public async Task<T?> GetContainerByIdAsync<T>(Guid id, CancellationToken cancellationToken = default)
        where T : Container
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Set<T>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<T?> GetContainerByNameAsync<T>(string name, CancellationToken cancellationToken = default)
        where T : Container
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Set<T>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<T>> GetContainersByTypeAsync<T>(CancellationToken cancellationToken = default)
        where T : Container
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Set<T>()
            .AsNoTracking()
            .Where(c => c.Status != "archived")
            .OrderBy(c => c.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<T> CreateContainerAsync<T>(T container, CancellationToken cancellationToken = default)
        where T : Container
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.Set<T>().Add(container);
        await db.SaveChangesAsync(cancellationToken);
        return container;
    }

    public async Task<T> UpdateContainerAsync<T>(T container, CancellationToken cancellationToken = default)
        where T : Container
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.Set<T>().Update(container);
        await db.SaveChangesAsync(cancellationToken);
        return container;
    }

    public async Task<IReadOnlyList<Container>> GetAllContainersAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Contents
            .OfType<Container>()
            .AsNoTracking()
            .Where(c => c.Status != "archived")
            .OrderBy(c => c.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteContainerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var container = await db.Contents.OfType<Container>()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (container is null) return;

        container.Status = "archived";
        container.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    // ── ContentItem CRUD ────────────────────────────────────────────

    public async Task<ContentItem?> GetItemByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.ContentItems
            .AsNoTracking()
            .Include(ci => ci.Labels)
                .ThenInclude(l => l.Label)
            .FirstOrDefaultAsync(ci => ci.Name == name, cancellationToken);
    }

    public async Task<ContentItem?> GetItemByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.ContentItems
            .AsNoTracking()
            .Include(ci => ci.Labels)
                .ThenInclude(l => l.Label)
            .FirstOrDefaultAsync(ci => ci.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ContentItem>> GetItemsByContainerAsync(
        Guid containerId, string? discriminator = null, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.ContentItems
            .AsNoTracking()
            .Include(ci => ci.Labels)
                .ThenInclude(l => l.Label)
            .Where(ci => ci.ContainerId == containerId)
            .Where(ci => ci.Status != "archived");

        if (!string.IsNullOrWhiteSpace(discriminator))
            query = query.Where(ci => ci.Discriminator == discriminator);

        return await query
            .OrderByDescending(ci => ci.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ContentItem> CreateItemAsync(ContentItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.ContentItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<ContentItem> UpdateItemAsync(ContentItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.ContentItems.Update(item);
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task DeleteItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.ContentItems.FirstOrDefaultAsync(ci => ci.Id == id, cancellationToken);
        if (item is null) return;

        item.Status = "archived";
        item.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    // ── File CRUD ───────────────────────────────────────────────────

    public async Task<ContentFile?> GetContentFileByItemIdAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.ContentFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == itemId, cancellationToken);
    }

    public async Task<ContentFile> CreateContentFileAsync(ContentFile file, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.ContentFiles.Add(file);
        await db.SaveChangesAsync(cancellationToken);
        return file;
    }

    // ── Classification ──────────────────────────────────────────────

    private IQueryable<ClassifiedItem> ClassifiedItemQuery(WorkplaceIqDbContext db)
        => db.ClassifiedItems
            .AsNoTrackingWithIdentityResolution()
            .Include(item => item.RssItem)
            .Include(item => item.SignalLabel);

    public async Task<ClassifiedItem?> GetClassifiedItemByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ClassifiedItemQuery(db)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<ClassifiedItem?> GetClassifiedByContentIdAsync(Guid contentId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ClassifiedItemQuery(db)
            .FirstOrDefaultAsync(item => item.ContentId == contentId, cancellationToken);
    }

    public async Task<IReadOnlyList<ClassifiedItem>> GetClassifiedItemsByLabelAsync(
        Guid labelId, int offset = 0, int limit = 50, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ClassifiedItemQuery(db)
            .Where(item => item.LabelId == labelId)
            .OrderByDescending(item => item.ClassifiedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClassifiedItem>> GetRecentClassifiedItemsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await ClassifiedItemQuery(db)
            .Where(item => !item.IsNoise)
            .OrderByDescending(item => item.ClassifiedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<ClassifiedItem> UpsertClassifiedItemAsync(ClassifiedItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.ClassifiedItems
            .FirstOrDefaultAsync(ci => ci.ContentId == item.ContentId, cancellationToken);

        if (existing is not null)
        {
            existing.LabelId = item.LabelId;
            existing.Reasoning = item.Reasoning;
            existing.IsNoise = item.IsNoise;
            existing.AttemptCount = item.AttemptCount;
            existing.HallucinatedSignal = item.HallucinatedSignal;
            existing.Embedding = item.Embedding;
            existing.ClassificationSource = item.ClassificationSource;
            existing.ClassifiedAt = item.ClassifiedAt;
            await db.SaveChangesAsync(cancellationToken);
            return existing;
        }

        db.ClassifiedItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<ClassifiedItem> UpdateClassifiedItemAsync(ClassifiedItem item, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.ClassifiedItems.Update(item);
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<Dictionary<Guid, int>> GetSignalCountsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.ClassifiedItems
            .GroupBy(item => item.LabelId)
            .Select(group => new { LabelId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(g => g.LabelId, g => g.Count, cancellationToken);
    }

    public async Task DeleteClassifiedItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.ClassifiedItems.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (item is not null)
        {
            db.ClassifiedItems.Remove(item);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async IAsyncEnumerable<ContentItem> GetUnclassifiedItemsAsync(int limit, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var items = await db.ContentItems
            .AsNoTracking()
            .Include(ci => ci.Labels)
                .ThenInclude(l => l.Label)
            .Where(ci => ci.Status != "archived")
            .Where(ci => !db.ClassifiedItems.Any(cfi => cfi.ContentId == ci.Id))
            .OrderByDescending(ci => ci.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
            yield return item;
    }

    // ── Labels ──────────────────────────────────────────────────────

    public async Task<Label?> GetLabelByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Labels
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.NormalizedName == name.ToUpperInvariant(), cancellationToken);
    }

    public async Task<Label> CreateLabelAsync(Label label, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.Labels.Add(label);
        await db.SaveChangesAsync(cancellationToken);
        return label;
    }

    public async Task AddLabelToContentAsync(Guid contentId, LabelName label, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var labelEntity = await GetOrCreateLabelAsync(db, label, cancellationToken);
        var exists = await db.ContentLabels.AnyAsync(
            cl => cl.ContentId == contentId && cl.LabelId == labelEntity.Id, cancellationToken);

        if (!exists)
        {
            db.ContentLabels.Add(new ContentLabel { ContentId = contentId, LabelId = labelEntity.Id });
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task AddLabelToItemAsync(Guid itemId, LabelName label, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var labelEntity = await GetOrCreateLabelAsync(db, label, cancellationToken);
        var exists = await db.ContentItemLabels.AnyAsync(
            cil => cil.ContentItemId == itemId && cil.LabelId == labelEntity.Id, cancellationToken);

        if (!exists)
        {
            db.ContentItemLabels.Add(new ContentItemLabel { ContentItemId = itemId, LabelId = labelEntity.Id });
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task<Label> GetOrCreateLabelAsync(
        WorkplaceIqDbContext db, LabelName labelName, CancellationToken cancellationToken)
    {
        var label = await db.Labels.FirstOrDefaultAsync(
            candidate => candidate.NormalizedName == labelName.NormalizedName, cancellationToken);
        if (label is not null) return label;

        label = new Label
        {
            Name = labelName.Name,
            NormalizedName = labelName.NormalizedName,
            Slug = labelName.Slug,
            CreatedAt = DateTime.UtcNow
        };
        db.Labels.Add(label);
        await db.SaveChangesAsync(cancellationToken);
        return label;
    }

    // ── Relationships ───────────────────────────────────────────────

    public async Task<ContentRelationship> CreateContentRelationshipAsync(ContentRelationship relationship, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.ContentRelationships.Add(relationship);
        await db.SaveChangesAsync(cancellationToken);
        return relationship;
    }

    // ── Metrics ─────────────────────────────────────────────────────

    public async Task<MetricDefinition?> GetMetricDefinitionByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.MetricDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Name == name, cancellationToken);
    }
}
