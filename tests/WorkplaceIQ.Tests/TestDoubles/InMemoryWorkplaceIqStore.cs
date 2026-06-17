namespace WorkplaceIQ.Tests.TestDoubles;

using WorkplaceIQ;
using WorkplaceIQ.Containers;
using WorkplaceIQ.Labels;
using WorkplaceIQ.Posts;

internal sealed class InMemoryWorkplaceIqStore : IWorkplaceIqStore
{
    public List<Container> Containers { get; } = [];

    public List<Post> Posts { get; } = [];

    public List<Label> Labels { get; } = [];

    public Task<Container?> GetContainerByKeyAsync(
        string key,
        string type,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Containers.FirstOrDefault(container =>
            container.Key == key && container.Type == type));
    }

    public Task<Container> CreateContainerAsync(
        string key,
        string type,
        string title,
        CancellationToken cancellationToken = default)
    {
        var container = new Container
        {
            Key = key,
            Type = type,
            Title = title
        };

        Containers.Add(container);

        return Task.FromResult(container);
    }

    public Task<IReadOnlyList<Post>> GetPostsAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Post>>(
            Posts.Where(post => post.ContainerId == containerId).ToList());
    }

    public Task<Post> CreatePostAsync(
        Guid containerId,
        string title,
        string body,
        IReadOnlyList<LabelName> labels,
        CancellationToken cancellationToken = default)
    {
        var post = new Post
        {
            ContainerId = containerId,
            Title = title,
            Body = body
        };

        foreach (var labelName in labels)
        {
            var label = Labels.FirstOrDefault(candidate =>
                candidate.NormalizedName == labelName.NormalizedName);

            if (label is null)
            {
                label = new Label
                {
                    Name = labelName.Name,
                    NormalizedName = labelName.NormalizedName,
                    Slug = labelName.Slug
                };
                Labels.Add(label);
            }

            post.PostLabels.Add(new PostLabel
            {
                Post = post,
                PostId = post.Id,
                Label = label,
                LabelId = label.Id
            });
        }

        Posts.Add(post);

        return Task.FromResult(post);
    }
}
