using System.Linq.Expressions;
using WorkplaceIQ.Content;

namespace WorkplaceIQ.Metrics;

public interface IMetricProvider
{
    string Name { get; }

    Expression<Func<ContentItem, bool>>? Filter { get; }
}
