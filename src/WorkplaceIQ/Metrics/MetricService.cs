using System.Diagnostics.Metrics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using WorkplaceIQ;
using WorkplaceIQ.Content;

namespace WorkplaceIQ.Metrics;

public sealed class MetricService : IMetricService, IDisposable
{
    private readonly IWorkplaceIqStore _store;
    private readonly IEnumerable<IMetricProvider> _providers;
    private readonly Meter _meter;
    private readonly ILogger<MetricService> _logger;

    public MetricService(
        IWorkplaceIqStore store,
        IEnumerable<IMetricProvider> providers,
        ILogger<MetricService> logger)
    {
        _store = store;
        _providers = providers;
        _meter = new Meter("WorkplaceIQ", "1.0.0");
        _logger = logger;
    }

    public async Task<MetricResult> ComputeAsync(
        string name,
        Guid? containerId = null,
        CancellationToken cancellationToken = default)
    {
        var definition = await _store.GetMetricDefinitionByNameAsync(name, cancellationToken);

        if (definition is null)
        {
            _logger.LogWarning("Metric definition '{Name}' not found.", name);
            return new MetricResult(0, "count", "0", null);
        }

        var provider = _providers.FirstOrDefault(p => p.Name == name);

        var items = await _store.GetContentByContainerAsync(
            containerId ?? Guid.Empty,
            cancellationToken);

        if (containerId == Guid.Empty || containerId is null)
        {
            items = [];
        }

        var filtered = provider?.Filter is not null
            ? items.AsQueryable().Where(provider.Filter).ToList()
            : items;

        var value = ComputeAggregation(definition, filtered);

        RecordToInstrument(definition, value);

        var displayValue = definition.DisplayUnit is not null
            ? ConvertDisplayUnit(value, definition.Unit, definition.DisplayUnit)
            : null;

        return new MetricResult(
            value,
            definition.Unit,
            displayValue?.ToString("F1"),
            definition.DisplayUnit);
    }

    private static double ComputeAggregation(MetricDefinition definition, IReadOnlyList<ContentItem> items)
    {
        if (items.Count == 0)
        {
            return 0;
        }

        if (definition.Aggregation == "Count")
        {
            return items.Count;
        }

        if (string.IsNullOrWhiteSpace(definition.SourceField))
        {
            return definition.Aggregation == "Count" ? items.Count : 0;
        }

        var values = items
            .Select(item => ExtractFieldValue(item.MetadataJson, definition.SourceField))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        if (values.Count == 0)
        {
            return 0;
        }

        return definition.Aggregation switch
        {
            "Sum" => values.Sum(),
            "Avg" => values.Average(),
            "Min" => values.Min(),
            "Max" => values.Max(),
            "Count" => values.Count,
            _ => 0
        };
    }

    private static double? ExtractFieldValue(string? metadataJson, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(metadataJson);
            if (doc.RootElement.TryGetProperty(fieldName, out var element))
            {
                return element.ValueKind switch
                {
                    JsonValueKind.Number => element.GetDouble(),
                    JsonValueKind.String => double.TryParse(element.GetString(), out var d) ? d : null,
                    _ => null
                };
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    private void RecordToInstrument(MetricDefinition definition, double value)
    {
        try
        {
            switch (definition.InstrumentKind)
            {
                case "Counter":
                    _meter.CreateCounter<double>(definition.Name, definition.Unit, definition.Description)
                        .Add(value);
                    break;

                case "Histogram":
                    _meter.CreateHistogram<double>(definition.Name, definition.Unit, definition.Description)
                        .Record(value);
                    break;

                case "Gauge":
                    _meter.CreateObservableGauge(
                        definition.Name,
                        () => new Measurement<double>(value),
                        definition.Unit,
                        definition.Description);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record metric '{Name}' to instrument.", definition.Name);
        }
    }

    private static double? ConvertDisplayUnit(double value, string fromUnit, string toUnit)
    {
        if (string.IsNullOrWhiteSpace(fromUnit) || string.IsNullOrWhiteSpace(toUnit))
        {
            return null;
        }

        if (fromUnit == "seconds" && toUnit == "hours")
        {
            return value / 3600.0;
        }

        if (fromUnit == "bytes" && toUnit == "mb")
        {
            return value / (1024.0 * 1024.0);
        }

        return null;
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
