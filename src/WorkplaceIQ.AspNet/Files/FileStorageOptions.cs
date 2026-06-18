namespace WorkplaceIQ.AspNet.Files;

public sealed class FileStorageOptions
{
    public string Provider { get; set; } = "MinIO";

    public string Endpoint { get; set; } = "http://localhost:9000";

    public string BucketName { get; set; } = "workplaceiq-files";

    public string AccessKey { get; set; } = "workplaceiq";

    public string SecretKey { get; set; } = "workplaceiq-secret";

    public bool UseSsl { get; set; }
}
