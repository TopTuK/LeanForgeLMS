using LF.Application.ModelDto.News;

namespace LF.Application.Services.Admin;

public interface IAdminNewsService
{
    // Drafts included, newest first.
    Task<PagedNewsPostsDto> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<NewsPostDto?> GetAsync(int id, CancellationToken cancellationToken = default);

    // Throws ArgumentException for invalid content or image ids.
    Task<NewsPostDto> CreateAsync(SaveNewsPostDto dto, int actingAdminId, CancellationToken cancellationToken = default);

    // Null when the post doesn't exist. Throws ArgumentException for invalid content or image ids.
    Task<NewsPostDto?> UpdateAsync(int id, SaveNewsPostDto dto, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    // Returns the new storage object id, to be referenced from CreateAsync/UpdateAsync.
    Task<int> UploadImageAsync(Stream content, string contentType, long sizeBytes, int actingAdminId, CancellationToken cancellationToken = default);
}
