using System.Security.Claims;
using LF.AppDomain.Models.News.Enums;
using LF.Application.ModelDto.News;
using LF.Application.Services.Admin;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

public sealed class AdminNewsEndpoints : IEndpointGroup
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/news").WithTags("AdminNews").RequireAuthorization("AdminOnly");

        group.MapGet("/", async Task<Ok<PagedAdminNewsResponse>>
            (int? page, int? pageSize, IAdminNewsService newsService, CancellationToken ct) =>
        {
            var effectivePage = page is > 0 ? page.Value : 1;
            var effectivePageSize = pageSize is > 0 ? Math.Min(pageSize.Value, MaxPageSize) : DefaultPageSize;

            var result = await newsService.ListAsync(effectivePage, effectivePageSize, ct);
            return TypedResults.Ok(new PagedAdminNewsResponse(
                [.. result.Items.Select(ToResponse)], result.TotalCount, effectivePage, effectivePageSize));
        });

        group.MapGet("/{id:int}", async Task<Results<Ok<AdminNewsPostResponse>, NotFound>>
            (int id, IAdminNewsService newsService, CancellationToken ct) =>
        {
            var post = await newsService.GetAsync(id, ct);
            return post is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(post));
        });

        group.MapPost("/", async Task<Results<Created<AdminNewsPostResponse>, UnauthorizedHttpResult, ValidationProblem>>
            (SaveNewsPostRequest request, ClaimsPrincipal user, IAdminNewsService newsService, CancellationToken ct) =>
        {
            var adminId = user.GetUserId();
            if (adminId is null) return TypedResults.Unauthorized();

            var validation = new SaveNewsPostRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            try
            {
                var post = await newsService.CreateAsync(ToDto(request), adminId.Value, ct);
                return TypedResults.Created($"/api/admin/news/{post.Id}", ToResponse(post));
            }
            catch (ArgumentException ex)
            {
                return TypedResults.ValidationProblem(ToProblem(ex));
            }
        });

        group.MapPut("/{id:int}", async Task<Results<Ok<AdminNewsPostResponse>, NotFound, ValidationProblem>>
            (int id, SaveNewsPostRequest request, IAdminNewsService newsService, CancellationToken ct) =>
        {
            var validation = new SaveNewsPostRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            try
            {
                var post = await newsService.UpdateAsync(id, ToDto(request), ct);
                return post is null ? TypedResults.NotFound() : TypedResults.Ok(ToResponse(post));
            }
            catch (ArgumentException ex)
            {
                return TypedResults.ValidationProblem(ToProblem(ex));
            }
        });

        group.MapDelete("/{id:int}", async Task<Results<NoContent, NotFound>>
            (int id, IAdminNewsService newsService, CancellationToken ct) =>
        {
            var deleted = await newsService.DeleteAsync(id, ct);
            return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
        });

        group.MapPost("/images", async Task<Results<Ok<UploadNewsImageResponse>, UnauthorizedHttpResult, ValidationProblem>>
            (IFormFile file, ClaimsPrincipal user, IAdminNewsService newsService, CancellationToken ct) =>
        {
            var adminId = user.GetUserId();
            if (adminId is null) return TypedResults.Unauthorized();

            if (file.Length == 0 || file.Length > NewsImageUpload.MaxSizeBytes
                || !NewsImageUpload.AllowedContentTypes.Contains(file.ContentType))
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = ["News images must be PNG, JPEG or WEBP files no larger than 5 MB."],
                });
            }

            await using var stream = file.OpenReadStream();
            var storageObjectId = await newsService.UploadImageAsync(stream, file.ContentType, file.Length, adminId.Value, ct);

            return TypedResults.Ok(new UploadNewsImageResponse(storageObjectId));
        }).DisableAntiforgery();
    }

    private static SaveNewsPostDto ToDto(SaveNewsPostRequest request) => new()
    {
        Title = request.Title,
        Html = request.Html,
        Visibility = Enum.Parse<NewsVisibility>(request.Visibility, ignoreCase: true),
        IsPublished = request.IsPublished,
        ImageStorageObjectIds = request.ImageStorageObjectIds ?? [],
    };

    // The domain names the offending argument; map it back onto the request field the editor shows.
    private static Dictionary<string, string[]> ToProblem(ArgumentException ex)
    {
        var field = ex.ParamName switch
        {
            "title" => "title",
            "html" => "html",
            "visibility" => "visibility",
            _ => "imageStorageObjectIds",
        };

        return new Dictionary<string, string[]> { [field] = [ex.Message] };
    }

    private static AdminNewsPostResponse ToResponse(NewsPostDto dto) => new(
        dto.Id,
        dto.Title,
        dto.Html,
        dto.Visibility.ToString(),
        dto.IsPublished,
        dto.PublishedAt,
        dto.CreatedAt,
        dto.UpdatedAt,
        [.. dto.Images.Select(i => new AdminNewsImageResponse(i.Id, i.StorageObjectId, NewsResponseMapper.ImageUrl(dto.Id, i.Id)))]);
}
