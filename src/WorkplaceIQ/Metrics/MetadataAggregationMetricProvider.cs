using System.Text.Json;

namespace WorkplaceIQ.Metrics;

public sealed class MetadataAggregationMetricProvider(
    string name,
    Func<IReadOnlyList<double>, double> aggregate) : MetricProviderBase, IMetricProvider
{
    public string Name { get; } = name;

    public async Task<IReadOnlyList<MetricResult>> ComputeSeriesAsync(
        MetricRequest request,
        IWorkplaceIqStore store,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SourceField))
        {
            return [];
        }

        var containerItems = await GetContainerItemsAsync(request, store, cancellationToken);
        var unit = request.Unit ?? "count";

        return containerItems
            .Select(entry =>
            {
                var values = entry.Items
                    .Select(item => ExtractFieldValue(item, request.SourceField))
                    .Where(value => value.HasValue)
                    .Select(value => value!.Value)
                    .ToList();

                var value = values.Count == 0 ? 0 : aggregate(values);

                return new MetricResult(
                    Name,
                    value,
                    unit,
                    FormatDisplayValue(value, unit, request.DisplayUnit),
                    request.DisplayUnit,
                    CreateTags(request, entry.Container));
            })
            .ToList();
    }

    private static double? ExtractFieldValue(Content.Content item, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(item.MetadataJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(item.MetadataJson);
            if (!doc.RootElement.TryGetProperty(fieldName, out var element))
            {
                return null;
            }

            return element.ValueKind switch
            {
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.String => double.TryParse(element.GetString(), out var value) ? value : null,
                _ => null
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
