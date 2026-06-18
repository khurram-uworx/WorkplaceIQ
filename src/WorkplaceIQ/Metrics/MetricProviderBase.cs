using WorkplaceIQ.Containers;
using WorkplaceIQ.Content;

namespace WorkplaceIQ.Metrics;

public abstract class MetricProviderBase
{
    protected static async Task<IReadOnlyList<(Container Container, IReadOnlyList<ContentItem> Items)>> GetContainerItemsAsync(
        MetricRequest request,
        IWorkplaceIqStore store,
        CancellationToken cancellationToken)
    {
        var containers = request.ContainerId.HasValue
            ? await GetSingleContainerAsync(request, store, cancellationToken)
            : await store.GetContainersAsync(request.ContainerType, cancellationToken);

        var results = new List<(Container Container, IReadOnlyList<ContentItem> Items)>();

        foreach (var container in containers)
        {
            var items = await store.GetContentByContainerAsync(container.Id, cancellationToken);
            results.Add((container, FilterItems(request, items).ToList()));
        }

        return results;
    }

    protected static IReadOnlyDictionary<string, object?> CreateTags(
        MetricRequest request,
        Container container)
    {
        var tags = new Dictionary<string, object?>
        {
            ["container.id"] = container.Id.ToString(),
            ["container.key"] = container.Key,
            ["container.type"] = container.Type
        };

        if (!string.IsNullOrWhiteSpace(request.ContentType))
        {
            tags["content.type"] = request.ContentType;
        }

        if (!string.IsNullOrWhiteSpace(request.SourceField))
        {
            tags["metadata.field"] = request.SourceField;
        }

        if (!string.IsNullOrWhiteSpace(request.Window))
        {
            tags["window"] = request.Window;
        }

        return tags;
    }

    protected static string? FormatDisplayValue(double value, string unit, string? displayUnit)
    {
        var converted = ConvertDisplayUnit(value, unit, displayUnit);
        return converted?.ToString("F1");
    }

    protected static double? ConvertDisplayUnit(double value, string unit, string? displayUnit)
    {
        if (string.IsNullOrWhiteSpace(displayUnit))
        {
            return null;
        }

        if (unit == "seconds" && displayUnit == "hours")
        {
            return value / 3600.0;
        }

        if (unit == "bytes" && displayUnit == "mb")
        {
            return value / (1024.0 * 1024.0);
        }

        return null;
    }

    private static async Task<IReadOnlyList<Container>> GetSingleContainerAsync(
        MetricRequest request,
        IWorkplaceIqStore store,
        CancellationToken cancellationToken)
    {
        var containers = await store.GetContainersAsync(request.ContainerType, cancellationToken);
        return containers
            .Where(container => container.Id == request.ContainerId)
            .ToList();
    }

    private static IEnumerable<ContentItem> FilterItems(
        MetricRequest request,
        IReadOnlyList<ContentItem> items)
    {
        var filtered = items.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.ContentType))
        {
            filtered = filtered.Where(item => item.ContentType == request.ContentType);
        }

        var start = GetWindowStart(request.Window);
        if (start is not null)
        {
            filtered = filtered.Where(item => item.CreatedAt >= start);
        }

        return filtered;
    }

    private static DateTimeOffset? GetWindowStart(string? window)
    {
        return window switch
        {
            "last_7_days" => new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-7), TimeSpan.Zero),
            "last_30_days" => new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-30), TimeSpan.Zero),
            _ => null
        };
    }
}
