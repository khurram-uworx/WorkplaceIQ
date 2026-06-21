using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WorkplaceIQ.Files;

public sealed class FileRecord
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ContentId { get; set; }

    [ForeignKey(nameof(ContentId))]
    public Content.Content? Content { get; set; }

    [Required]
    [MaxLength(512)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long SizeBytes { get; set; }

    [MaxLength(128)]
    public string? ChecksumSha256 { get; set; }

    [Required]
    [MaxLength(64)]
    public string StorageProvider { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string BucketName { get; set; } = string.Empty;

    [Required]
    [MaxLength(1024)]
    public string ObjectKey { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
