namespace WorkplaceIQ.Content;

public sealed class ContainerService(IWorkplaceIqStore store) : IContainerService
{
    public Task<T?> GetByIdAsync<T>(Guid id, CancellationToken cancellationToken = default) where T : Container
        => store.GetContainerByIdAsync<T>(id, cancellationToken);

    public Task<T?> GetByNameAsync<T>(string name, CancellationToken cancellationToken = default) where T : Container
        => store.GetContainerByNameAsync<T>(name, cancellationToken);

    public Task<T> CreateAsync<T>(T container, CancellationToken cancellationToken = default) where T : Container
        => store.CreateContainerAsync(container, cancellationToken);

    public Task<T> UpdateAsync<T>(T container, CancellationToken cancellationToken = default) where T : Container
        => store.UpdateContainerAsync(container, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => store.DeleteContainerAsync(id, cancellationToken);
}
