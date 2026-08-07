using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace LF.Infrastructure.Services.Storage;

internal sealed class MinioBucketInitializer(
    ILogger<MinioBucketInitializer> logger,
    IMinioClient minioClient,
    IOptions<MinioStorageOptions> options) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var bucketName = options.Value.AvatarsBucketName;

        var exists = await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName), cancellationToken);
        if (exists)
            return;

        logger.LogInformation("MinioBucketInitializer::StartAsync: creating bucket {BucketName}", bucketName);
        await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
