namespace WorkplaceIQ.Content;

public interface IContainerService
{
    Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : Container;
    Task<T?> GetByNameAsync<T>(string name, CancellationToken cancellationToken = default) where T : Container;
    Task<T> CreateAsync<T>(T container, CancellationToken cancellationToken = default) where T : Container;
    Task<T> UpdateAsync<T>(T container, CancellationToken cancellationToken = default) where T : Container;
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
