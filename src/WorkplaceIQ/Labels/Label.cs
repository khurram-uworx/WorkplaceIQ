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

    [MaxLength(7)]
    public string? Color { get; set; }

    [MaxLength(256)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ContentLabel> ContentLabels { get; set; } = [];
    public ICollection<ContentItemLabel> ContentItemLabels { get; set; } = [];
}
