using System.Security.Claims;
using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Exceptions;
using LF.Application.ModelDto.Course;
using LF.Application.Services.CourseAuthoring;
using LF.WebApi.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LF.WebApi.Endpoints;

public sealed class CourseEndpoints : IEndpointGroup
{
    private const int DefaultPageSize = 20;

    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/courses").WithTags("Courses").RequireAuthorization("CourseCreatorOrAdmin");

        group.MapGet("/categories", async Task<Results<Ok<IReadOnlyList<CategoryResponse>>, UnauthorizedHttpResult>>
            (ClaimsPrincipal user, ICourseAuthoringService courseService, CancellationToken ct) =>
        {
            if (user.GetUserId() is null) return TypedResults.Unauthorized();

            var categories = await courseService.ListCategoriesAsync();
            return TypedResults.Ok<IReadOnlyList<CategoryResponse>>([.. categories.Select(c => new CategoryResponse(c.Id, c.Name))]);
        });

        group.MapGet("/", async Task<Results<Ok<PagedCoursesResponse>, UnauthorizedHttpResult>>
            (int? page, int? pageSize, ClaimsPrincipal user, ICourseAuthoringService courseService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var effectivePage = page is > 0 ? page.Value : 1;
            var effectivePageSize = pageSize is > 0 ? pageSize.Value : DefaultPageSize;
            var isAdmin = user.IsInRole(nameof(UserRole.Admin));

            var result = await courseService.ListCoursesAsync(effectivePage, effectivePageSize, userId.Value, isAdmin);
            return TypedResults.Ok(new PagedCoursesResponse([.. result.Items.Select(ToSummaryResponse)], result.TotalCount, effectivePage, effectivePageSize));
        });

        group.MapPost("/", async Task<Results<Created<CourseDetailResponse>, UnauthorizedHttpResult, ValidationProblem>>
            (CreateCourseRequest request, ClaimsPrincipal user, ICourseAuthoringService courseService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new CreateCourseRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            var dto = new CreateCourseDto { Title = request.Title, ShortIntroduction = request.ShortIntroduction, Description = request.Description, CategoryId = request.CategoryId };

            try
            {
                var course = await courseService.CreateCourseAsync(dto, userId.Value);
                return TypedResults.Created($"/api/courses/{course.Id}", ToDetailResponse(course));
            }
            catch (ArgumentException)
            {
                return TypedResults.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["categoryId"] = ["Category not found."],
                });
            }
        });

        group.MapGet("/{id:int}", async Task<Results<Ok<CourseDetailResponse>, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int id, ClaimsPrincipal user, ICourseAuthoringService courseService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var isAdmin = user.IsInRole(nameof(UserRole.Admin));

            try
            {
                var course = await courseService.GetCourseAsync(id, userId.Value, isAdmin);
                return course is null ? TypedResults.NotFound() : TypedResults.Ok(ToDetailResponse(course));
            }
            catch (CourseAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        group.MapPost("/{id:int}/chapters", async Task<Results<Ok<CourseDetailResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, ForbidHttpResult>>
            (int id, AddChapterRequest request, ClaimsPrincipal user, ICourseAuthoringService courseService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new AddChapterRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            var isAdmin = user.IsInRole(nameof(UserRole.Admin));

            try
            {
                var course = await courseService.AddChapterAsync(id, request.Title, userId.Value, isAdmin);
                return course is null ? TypedResults.NotFound() : TypedResults.Ok(ToDetailResponse(course));
            }
            catch (CourseAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        group.MapPut("/{id:int}/chapters/{chapterId:int}", async Task<Results<Ok<CourseDetailResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, ForbidHttpResult>>
            (int id, int chapterId, RenameChapterRequest request, ClaimsPrincipal user, ICourseAuthoringService courseService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new RenameChapterRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            var isAdmin = user.IsInRole(nameof(UserRole.Admin));

            try
            {
                var course = await courseService.RenameChapterAsync(id, chapterId, request.Title, userId.Value, isAdmin);
                return course is null ? TypedResults.NotFound() : TypedResults.Ok(ToDetailResponse(course));
            }
            catch (CourseAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        group.MapPost("/{id:int}/chapters/{chapterId:int}/move", async Task<Results<Ok<CourseDetailResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, ForbidHttpResult>>
            (int id, int chapterId, MoveRequest request, ClaimsPrincipal user, ICourseAuthoringService courseService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new MoveRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            var isAdmin = user.IsInRole(nameof(UserRole.Admin));
            var direction = Enum.Parse<MoveDirection>(request.Direction);

            try
            {
                var course = await courseService.MoveChapterAsync(id, chapterId, direction, userId.Value, isAdmin);
                return course is null ? TypedResults.NotFound() : TypedResults.Ok(ToDetailResponse(course));
            }
            catch (CourseAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        group.MapPost("/{id:int}/chapters/{chapterId:int}/lessons", async Task<Results<Ok<CourseDetailResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, ForbidHttpResult>>
            (int id, int chapterId, AddLessonRequest request, ClaimsPrincipal user, ICourseAuthoringService courseService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new AddLessonRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            var isAdmin = user.IsInRole(nameof(UserRole.Admin));
            var dto = new AddLessonDto { Title = request.Title, Content = request.Content, IncludeInPreview = request.IncludeInPreview };

            try
            {
                var course = await courseService.AddLessonAsync(id, chapterId, dto, userId.Value, isAdmin);
                return course is null ? TypedResults.NotFound() : TypedResults.Ok(ToDetailResponse(course));
            }
            catch (CourseAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        group.MapPut("/{id:int}/chapters/{chapterId:int}/lessons/{lessonId:int}", async Task<Results<Ok<CourseDetailResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, ForbidHttpResult>>
            (int id, int chapterId, int lessonId, UpdateLessonRequest request, ClaimsPrincipal user, ICourseAuthoringService courseService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new UpdateLessonRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            var isAdmin = user.IsInRole(nameof(UserRole.Admin));
            var dto = new UpdateLessonDto { Title = request.Title, Content = request.Content, IncludeInPreview = request.IncludeInPreview };

            try
            {
                var course = await courseService.UpdateLessonAsync(id, chapterId, lessonId, dto, userId.Value, isAdmin);
                return course is null ? TypedResults.NotFound() : TypedResults.Ok(ToDetailResponse(course));
            }
            catch (CourseAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        group.MapPost("/{id:int}/chapters/{chapterId:int}/lessons/{lessonId:int}/move", async Task<Results<Ok<CourseDetailResponse>, UnauthorizedHttpResult, NotFound, ValidationProblem, ForbidHttpResult>>
            (int id, int chapterId, int lessonId, MoveRequest request, ClaimsPrincipal user, ICourseAuthoringService courseService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var validation = new MoveRequestValidator().Validate(request);
            if (!validation.IsValid) return TypedResults.ValidationProblem(validation.ToDictionary());

            var isAdmin = user.IsInRole(nameof(UserRole.Admin));
            var direction = Enum.Parse<MoveDirection>(request.Direction);

            try
            {
                var course = await courseService.MoveLessonAsync(id, chapterId, lessonId, direction, userId.Value, isAdmin);
                return course is null ? TypedResults.NotFound() : TypedResults.Ok(ToDetailResponse(course));
            }
            catch (CourseAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        group.MapDelete("/{id:int}/chapters/{chapterId:int}/lessons/{lessonId:int}", async Task<Results<Ok<CourseDetailResponse>, UnauthorizedHttpResult, NotFound, ForbidHttpResult>>
            (int id, int chapterId, int lessonId, ClaimsPrincipal user, ICourseAuthoringService courseService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var isAdmin = user.IsInRole(nameof(UserRole.Admin));

            try
            {
                var course = await courseService.RemoveLessonAsync(id, chapterId, lessonId, userId.Value, isAdmin);
                return course is null ? TypedResults.NotFound() : TypedResults.Ok(ToDetailResponse(course));
            }
            catch (CourseAuthorizationException)
            {
                return TypedResults.Forbid();
            }
        });

        group.MapPost("/{id:int}/publish", async Task<Results<Ok<CourseDetailResponse>, UnauthorizedHttpResult, NotFound, Conflict<string>, ForbidHttpResult>>
            (int id, ClaimsPrincipal user, ICourseAuthoringService courseService, CancellationToken ct) =>
        {
            var userId = user.GetUserId();
            if (userId is null) return TypedResults.Unauthorized();

            var isAdmin = user.IsInRole(nameof(UserRole.Admin));

            try
            {
                var course = await courseService.PublishCourseAsync(id, userId.Value, isAdmin);
                return course is null ? TypedResults.NotFound() : TypedResults.Ok(ToDetailResponse(course));
            }
            catch (CourseAuthorizationException)
            {
                return TypedResults.Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return TypedResults.Conflict(ex.Message);
            }
        });
    }

    private static CourseDetailResponse ToDetailResponse(CourseDetailDto course) => new(
        course.Id,
        course.Title,
        course.ShortIntroduction,
        course.Description,
        course.ImageKey,
        course.IsPublished,
        course.CategoryId,
        course.CategoryName,
        course.CreatedByUserId,
        course.CreatedAt,
        [.. course.Chapters.Select(ToChapterResponse)]);

    private static ChapterResponse ToChapterResponse(ChapterDto chapter) => new(
        chapter.Id,
        chapter.Title,
        chapter.SortOrder,
        [.. chapter.Lessons.Select(l => new LessonResponse(l.Id, l.Title, l.Content, l.IncludeInPreview, l.SortOrder))]);

    private static CourseSummaryResponse ToSummaryResponse(CourseSummaryDto course) => new(
        course.Id,
        course.Title,
        course.ShortIntroduction,
        course.IsPublished,
        course.CategoryId,
        course.CategoryName,
        course.CreatedByUserId,
        course.CreatedAt,
        course.ChapterCount);
}
