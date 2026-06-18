using System.Linq.Expressions;
using WorkplaceIQ.Content;
using WorkplaceIQ.Metrics;

namespace WorkplaceIQ.Web.Metrics;

public sealed class OutageCountLast7DaysProvider : IMetricProvider
{
    public string Name => "OutageCountLast7Days";

    public Expression<Func<ContentItem, bool>>? Filter =>
        item => item.ContentType == "Outage"
            && item.CreatedAt >= new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-7), TimeSpan.Zero);
}

public sealed class TotalOutageTimeLast7DaysProvider : IMetricProvider
{
    public string Name => "TotalOutageTimeLast7Days";

    public Expression<Func<ContentItem, bool>>? Filter =>
        item => item.ContentType == "Outage"
            && item.CreatedAt >= new DateTimeOffset(DateTime.UtcNow.Date.AddDays(-7), TimeSpan.Zero);
}
