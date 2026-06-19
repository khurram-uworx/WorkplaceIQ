namespace WorkplaceIQ.Metrics;

public abstract class MetricProviderBase
{
    protected static async Task<IReadOnlyList<(Content.Content Container, IReadOnlyList<Content.Content> Items)>> GetContainerItemsAsync(
        MetricRequest request,
        IWorkplaceIqStore store,
        CancellationToken cancellationToken)
    {
        var containers = request.ContainerId.HasValue
            ? await GetSingleContainerAsync(request, store, cancellationToken)
            : await store.GetContentByTypeAsync(request.ContainerType ?? string.Empty, cancellationToken);

        var results = new List<(Content.Content Container, IReadOnlyList<Content.Content> Items)>();

        foreach (var container in containers)
        {
            var items = await store.GetChildrenAsync(container.Id, cancellationToken: cancellationToken);
            results.Add((container, FilterItems(request, items).ToList()));
        }

        return results;
    }

    protected static IReadOnlyDictionary<string, object?> CreateTags(
        MetricRequest request,
        Content.Content container)
    {
        var tags = new Dictionary<string, object?>
        {
            ["container.id"] = container.Id.ToString(),
            ["container.name"] = container.Name,
            ["container.type"] = container.ContentType
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

    private static async Task<IReadOnlyList<Content.Content>> GetSingleContainerAsync(
        MetricRequest request,
        IWorkplaceIqStore store,
        CancellationToken cancellationToken)
    {
        var containers = await store.GetContentByTypeAsync(request.ContainerType ?? string.Empty, cancellationToken);
        return containers
            .Where(container => container.Id == request.ContainerId)
            .ToList();
    }

    private static IEnumerable<Content.Content> FilterItems(
        MetricRequest request,
        IReadOnlyList<Content.Content> items)
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
