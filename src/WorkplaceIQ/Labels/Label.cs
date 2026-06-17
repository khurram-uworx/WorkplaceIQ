using System.ComponentModel.DataAnnotations;

namespace WorkplaceIQ.Labels;

public sealed class Label
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string NormalizedName { get; set; } = string.Empty;

    [Required]
    [MaxLength(96)]
    public string Slug { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<PostLabel> PostLabels { get; set; } = [];
}
