using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace WorkplaceIQ.Containers;

[Index(nameof(Key), nameof(Type), IsUnique = true)]
public sealed class Container
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(128)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
