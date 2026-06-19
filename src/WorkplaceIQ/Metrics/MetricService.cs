using Microsoft.Extensions.Logging;
using System.Diagnostics.Metrics;

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
        MetricRequest request,
        CancellationToken cancellationToken = default)
    {
        var series = await ComputeSeriesAsync(request, cancellationToken);

        return series.Count > 0
            ? series[0]
            : MetricResultFactory.Zero(request);
    }

    public async Task<IReadOnlyList<MetricResult>> ComputeSeriesAsync(
        MetricRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return [MetricResultFactory.Zero(request)];
        }

        var provider = _providers.FirstOrDefault(candidate => candidate.Name == request.Name);

        if (provider is null)
        {
            _logger.LogWarning("Metric provider '{Name}' not found.", request.Name);
            return [MetricResultFactory.Zero(request)];
        }

        var results = await provider.ComputeSeriesAsync(request, _store, cancellationToken);

        foreach (var result in results)
        {
            RecordToInstrument(result);
        }

        return results.Count > 0
            ? results
            : [MetricResultFactory.Zero(request)];
    }

    private void RecordToInstrument(MetricResult result)
    {
        try
        {
            var tags = result.Tags
                .Select(tag => new KeyValuePair<string, object?>(tag.Key, tag.Value))
                .ToArray();

            _meter.CreateObservableGauge(
                result.Name,
                () => new Measurement<double>(result.Value, tags),
                result.Unit);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record metric '{Name}' to instrument.", result.Name);
        }
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
