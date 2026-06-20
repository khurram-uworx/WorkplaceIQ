using WorkplaceIQ.Content;
using WorkplaceIQ.Web.SignalFlow.Models;

namespace WorkplaceIQ.Web.SignalFlow.Services;

public interface IFeedbackService
{
    Task<List<SignalGroup>> GetSignalsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ClassifiedItem>> GetRecentItemsAsync(int limit = 20, CancellationToken ct = default);
    Task<List<ClassifiedItem>> GetNoiseAsync(CancellationToken ct = default);
    Task<List<ClassifiedItem>> GetBouncedAsync(CancellationToken ct = default);
    Task<ClassifiedItem?> GetItemDetailsAsync(Guid itemId, CancellationToken ct = default);
    Task<bool> ReclassifyAsync(Guid itemId, string newSignal, bool isNoise, CancellationToken ct = default);
    Task<bool> DeleteItemAsync(Guid itemId, CancellationToken ct = default);
    Task<List<(ClassifiedItem Item, double Score)>> MoreLikeAsync(Guid classifiedId, int top = 6, CancellationToken ct = default);
    Task<Dictionary<string, int>> GetSignalCountsAsync(CancellationToken ct = default);
    Task<int> GetNoiseCountAsync(CancellationToken ct = default);
    Task<int> GetFailedCountAsync(CancellationToken ct = default);
    Task<bool> MarkNotNoiseAsync(Guid classifiedId, CancellationToken ct = default);
    Task<(bool Success, ClassifiedItem? Item)> RetryFailedAsync(Guid classifiedId, CancellationToken ct = default);
}
