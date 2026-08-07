namespace LF.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task UploadAsync(string objectKey, Stream content, string contentType, CancellationToken ct = default);

    Task<FileDownloadResult?> DownloadAsync(string objectKey, CancellationToken ct = default);

    Task DeleteAsync(string objectKey, CancellationToken ct = default);
}

public sealed record FileDownloadResult(Stream Content, string ContentType);
