using LF.AppDomain.Entities.News;
using LF.AppDomain.Entities.Storage;
using LF.AppDomain.Models.Storage.Enums;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.News;
using LF.Application.Services.News;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.Admin;

// Runs inside LF.WebApi, the only host with MinIO — news posts and their image blobs are managed
// in one process, the same reasoning that keeps StorageObjects out of the gRPC services.
internal sealed class AdminNewsService(
    ILogger<AdminNewsService> logger,
    IAppDbContext dbContext,
    IHtmlSanitizer htmlSanitizer,
    [FromKeyedServices("storage")] IFileStorageService fileStorageService,
    TimeProvider timeProvider) : IAdminNewsService
{
    // News images get their own key prefix so a post can only ever reference images uploaded for
    // news: attaching a course cover or lesson media would expose it publicly and, on delete,
    // remove a blob the course still points to.
    internal const string ImageKeyPrefix = "news/";

    private static readonly IReadOnlyDictionary<string, string> ImageExtensionsByContentType = new Dictionary<string, string>
    {
        ["image/png"] = ".png",
        ["image/jpeg"] = ".jpg",
        ["image/webp"] = ".webp",
    };

    private readonly ILogger<AdminNewsService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;
    private readonly IHtmlSanitizer _htmlSanitizer = htmlSanitizer;
    private readonly IFileStorageService _fileStorageService = fileStorageService;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<PagedNewsPostsDto> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AdminNewsService::ListAsync: called with Page={Page} PageSize={PageSize}", page, pageSize);

        var query = _dbContext.NewsPosts.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(NewsPostProjection.ToDto)
            .ToListAsync(cancellationToken);

        return new PagedNewsPostsDto { Items = items, TotalCount = totalCount };
    }

    public async Task<NewsPostDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AdminNewsService::GetAsync: called with Id={NewsPostId}", id);

        return await _dbContext.NewsPosts
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(NewsPostProjection.ToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<NewsPostDto> CreateAsync(SaveNewsPostDto dto, int actingAdminId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AdminNewsService::CreateAsync: called with ActingAdminId={AdminId} Visibility={Visibility} IsPublished={IsPublished} ImageCount={ImageCount}",
            actingAdminId, dto.Visibility, dto.IsPublished, dto.ImageStorageObjectIds.Count);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var post = NewsPost.Create(dto.Title, _htmlSanitizer.Sanitize(dto.Html), dto.Visibility, actingAdminId, now);

        post.ReplaceImages(await LoadImagesAsync(dto.ImageStorageObjectIds, newsPostId: null, cancellationToken));
        ApplyPublishState(post, dto.IsPublished, now);

        _dbContext.NewsPosts.Add(post);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NewsPostProjection.ToDtoCompiled(post);
    }

    public async Task<NewsPostDto?> UpdateAsync(int id, SaveNewsPostDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AdminNewsService::UpdateAsync: called with Id={NewsPostId} Visibility={Visibility} IsPublished={IsPublished} ImageCount={ImageCount}",
            id, dto.Visibility, dto.IsPublished, dto.ImageStorageObjectIds.Count);

        var post = await LoadPostWithImagesAsync(id, cancellationToken);
        if (post is null)
            return null;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        post.Edit(dto.Title, _htmlSanitizer.Sanitize(dto.Html), dto.Visibility, now);

        var previousImages = post.Images.Select(i => i.StorageObject).ToList();
        var removedIds = post.ReplaceImages(await LoadImagesAsync(dto.ImageStorageObjectIds, id, cancellationToken));
        ApplyPublishState(post, dto.IsPublished, now);

        var removedObjects = previousImages.Where(o => removedIds.Contains(o.Id)).ToList();
        _dbContext.StorageObjects.RemoveRange(removedObjects);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await DeleteBlobsAsync(removedObjects);

        return NewsPostProjection.ToDtoCompiled(post);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AdminNewsService::DeleteAsync: called with Id={NewsPostId}", id);

        var post = await LoadPostWithImagesAsync(id, cancellationToken);
        if (post is null)
            return false;

        var storageObjects = post.Images.Select(i => i.StorageObject).ToList();

        _dbContext.NewsPosts.Remove(post);
        _dbContext.StorageObjects.RemoveRange(storageObjects);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await DeleteBlobsAsync(storageObjects);
        return true;
    }

    public async Task<int> UploadImageAsync(Stream content, string contentType, long sizeBytes, int actingAdminId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AdminNewsService::UploadImageAsync: called with ContentType={ContentType} SizeBytes={SizeBytes} ActingAdminId={AdminId}",
            contentType, sizeBytes, actingAdminId);

        var extension = ImageExtensionsByContentType.GetValueOrDefault(contentType, string.Empty);
        var objectKey = $"{ImageKeyPrefix}{Guid.NewGuid():N}{extension}";

        await _fileStorageService.UploadAsync(objectKey, content, contentType, cancellationToken);

        var storageObject = StorageObject.Create(
            StorageObjectType.Image, objectKey, contentType, sizeBytes, actingAdminId, _timeProvider.GetUtcNow().UtcDateTime);

        try
        {
            _dbContext.StorageObjects.Add(storageObject);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _fileStorageService.DeleteAsync(objectKey, cancellationToken);
            throw;
        }

        return storageObject.Id;
    }

    private async Task<NewsPost?> LoadPostWithImagesAsync(int id, CancellationToken cancellationToken) =>
        await _dbContext.NewsPosts
            .Include(p => p.Images)
            .ThenInclude(i => i.StorageObject)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    // Resolves the requested ids in their requested order. Only news-prefixed images that are
    // unattached or already belong to this post qualify — sharing an image between posts would
    // make deleting either one fail on the Restrict FK.
    private async Task<IReadOnlyList<StorageObject>> LoadImagesAsync(
        IReadOnlyList<int> storageObjectIds, int? newsPostId, CancellationToken cancellationToken)
    {
        if (storageObjectIds.Count == 0)
            return [];

        var distinctIds = storageObjectIds.Distinct().ToList();

        var found = await _dbContext.StorageObjects
            .Where(s => distinctIds.Contains(s.Id) && s.ObjectKey.StartsWith(ImageKeyPrefix))
            .ToListAsync(cancellationToken);

        var byId = found.ToDictionary(s => s.Id);
        var unknownIds = distinctIds.Where(id => !byId.ContainsKey(id)).ToList();
        if (unknownIds.Count > 0)
            throw new ArgumentException($"Unknown news image id(s): {string.Join(", ", unknownIds)}.", nameof(storageObjectIds));

        var attachedElsewhere = await _dbContext.NewsPosts
            .Where(p => p.Id != newsPostId)
            .SelectMany(p => p.Images)
            .AnyAsync(i => distinctIds.Contains(i.StorageObjectId), cancellationToken);
        if (attachedElsewhere)
            throw new ArgumentException("An image is already attached to another news post.", nameof(storageObjectIds));

        return [.. storageObjectIds.Select(id => byId[id])];
    }

    private static void ApplyPublishState(NewsPost post, bool isPublished, DateTime now)
    {
        if (isPublished)
            post.Publish(now);
        else
            post.Unpublish();
    }

    // The rows are already gone by the time we get here, so a storage failure must never surface
    // as a failed save — a leaked blob is recoverable, a misleading error is not. The catch stays
    // broad because the concrete MinIO exception types live in LF.Infrastructure and cannot be
    // named from here; cancellation is deliberately excluded so a shutdown still unwinds.
    private async Task DeleteBlobsAsync(IReadOnlyList<StorageObject> storageObjects)
    {
        foreach (var storageObject in storageObjects)
        {
            try
            {
                await _fileStorageService.DeleteAsync(storageObject.ObjectKey);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "AdminNewsService: failed to delete storage object {ObjectKey}", storageObject.ObjectKey);
            }
        }
    }
}
