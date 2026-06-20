using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using WorkplaceIQ.Web.SignalFlow.Models;

namespace WorkplaceIQ.Web.SignalFlow.Services;

public static class RssFetcher
{
    static string ComputeRssSha(string feedUrl, string feedName, string title, DateTimeOffset published)
    {
        var input = $"{feedUrl}|{feedName}|{title}|{published.Ticks}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    public static async IAsyncEnumerable<RssItem> FetchFeedAsync(
        string feedUrl,
        string feedName,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        using var stream = await httpClient.GetStreamAsync(feedUrl, ct);
        using var reader = XmlReader.Create(stream);
        var feed = SyndicationFeed.Load(reader);

        foreach (var item in feed.Items)
        {
            ct.ThrowIfCancellationRequested();

            var title = item.Title?.Text ?? "(no title)";
            var published = item.PublishDate != DateTimeOffset.MinValue
                ? item.PublishDate
                : item.LastUpdatedTime;

            yield return new RssItem
            {
                FeedUrl = feedUrl,
                FeedName = feedName,
                Title = title,
                Summary = item.Summary?.Text ?? string.Empty,
                Link = item.Links.FirstOrDefault()?.Uri?.ToString() ?? string.Empty,
                Published = published,
                ContentHash = ComputeRssSha(feedUrl, feedName, title, published)
            };
        }
    }
}
