using LF.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace LF.Infrastructure.Services.Storage;

internal sealed class MinioFileStorageService(
    ILogger<MinioFileStorageService> logger,
    IMinioClient minioClient,
    string bucketName) : IFileStorageService
{
    private readonly string _bucketName = bucketName;

    public async Task UploadAsync(string objectKey, Stream content, string contentType, CancellationToken ct = default)
    {
        var args = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectKey)
            .WithStreamData(content)
            .WithObjectSize(content.Length)
            .WithContentType(contentType);

        await minioClient.PutObjectAsync(args, ct);
    }

    public async Task<FileDownloadResult?> DownloadAsync(string objectKey, CancellationToken ct = default)
    {
        try
        {
            var statArgs = new StatObjectArgs().WithBucket(_bucketName).WithObject(objectKey);
            var stat = await minioClient.StatObjectAsync(statArgs, ct);

            var buffer = new MemoryStream();
            var getArgs = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectKey)
                .WithCallbackStream(async (stream, innerCt) => await stream.CopyToAsync(buffer, innerCt));

            await minioClient.GetObjectAsync(getArgs, ct);
            buffer.Position = 0;

            return new FileDownloadResult(buffer, stat.ContentType);
        }
        catch (ObjectNotFoundException)
        {
            return null;
        }
        catch (ErrorResponseException ex) when (ex.Response.Code == "NoSuchKey")
        {
            return null;
        }
    }

    public async Task DeleteAsync(string objectKey, CancellationToken ct = default)
    {
        logger.LogInformation("MinioFileStorageService::DeleteAsync: removing object {ObjectKey}", objectKey);

        var args = new RemoveObjectArgs().WithBucket(_bucketName).WithObject(objectKey);
        await minioClient.RemoveObjectAsync(args, ct);
    }
}
