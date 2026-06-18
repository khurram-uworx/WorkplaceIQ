using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Options;
using WorkplaceIQ.Files;

namespace WorkplaceIQ.AspNet.Files;

public sealed class S3FileObjectStorage : IFileObjectStorage
{
    private readonly FileStorageOptions options;
    private readonly IAmazonS3 client;

    public S3FileObjectStorage(IOptions<FileStorageOptions> options)
    {
        this.options = options.Value;

        var config = new AmazonS3Config
        {
            ServiceURL = this.options.Endpoint,
            ForcePathStyle = true,
            UseHttp = !this.options.UseSsl
        };

        client = new AmazonS3Client(
            new BasicAWSCredentials(this.options.AccessKey, this.options.SecretKey),
            config);
    }

    public string ProviderName => options.Provider;

    public string BucketName => options.BucketName;

    public async Task EnsureBucketAsync(CancellationToken cancellationToken = default)
    {
        var exists = await AmazonS3Util.DoesS3BucketExistV2Async(client, options.BucketName);
        if (exists)
        {
            return;
        }

        await client.PutBucketAsync(new PutBucketRequest
        {
            BucketName = options.BucketName
        }, cancellationToken);
    }

    public async Task<StoredFileObject> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        await client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = options.BucketName,
            Key = objectKey,
            InputStream = content,
            ContentType = contentType,
            AutoCloseStream = false
        }, cancellationToken);

        return new StoredFileObject(
            ProviderName,
            options.BucketName,
            objectKey,
            ChecksumSha256: null);
    }

    public async Task<Stream> OpenReadAsync(
        FileRecord fileRecord,
        CancellationToken cancellationToken = default)
    {
        var response = await client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = fileRecord.BucketName,
            Key = fileRecord.ObjectKey
        }, cancellationToken);

        return response.ResponseStream;
    }

    public Task DeleteAsync(
        FileRecord fileRecord,
        CancellationToken cancellationToken = default)
    {
        return client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = fileRecord.BucketName,
            Key = fileRecord.ObjectKey
        }, cancellationToken);
    }
}
