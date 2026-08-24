using Grpc.Core;
using LF.Application.Common.Exceptions;
using LF.Application.ModelDto.Course;
using LF.Application.Services.Course;
using LF.CourseService;
using Mapster;
using Microsoft.Extensions.Logging;
using AppMoveDirection = LF.Application.ModelDto.Course.MoveDirection;
using RpcMoveDirection = LF.CourseService.MoveDirection;

namespace LF.Infrastructure.Services.Course;

internal sealed class GrpcCourseService(ILogger<GrpcCourseService> logger,
    CourseServiceRpc.CourseServiceRpcClient courseServiceRpcClient) : IGrpcCourseService
{
    private readonly ILogger<GrpcCourseService> _logger = logger;
    private readonly CourseServiceRpc.CourseServiceRpcClient _courseServiceRpcClient = courseServiceRpcClient;

    public async Task<CourseDetailDto> CreateCourseAsync(CreateCourseDto dto, int createdByUserId)
    {
        _logger.LogInformation("GrpcCourseService::CreateCourseAsync: called with Title={Title} CreatedByUserId={CreatedByUserId}", dto.Title, createdByUserId);

        var request = dto.Adapt<CreateCourseRequest>();
        request.CreatedByUserId = createdByUserId;

        try
        {
            var reply = await _courseServiceRpcClient.CreateCourseAsync(request);
            return reply.Adapt<CourseDetailDto>();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
        {
            throw new ArgumentException(ex.Status.Detail);
        }
    }

    public async Task<CourseDetailDto?> GetCourseAsync(int id, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("GrpcCourseService::GetCourseAsync: called with Id={CourseId} ActingUserId={ActingUserId}", id, actingUserId);

        var request = new GetCourseRequest { Id = id, ActingUserId = actingUserId, ActingIsAdmin = isAdmin };
        return await CallOrDefaultAsync(() => _courseServiceRpcClient.GetCourseAsync(request));
    }

    public async Task<PagedCoursesDto> ListCoursesAsync(int page, int pageSize, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("GrpcCourseService::ListCoursesAsync: called with Page={Page} PageSize={PageSize} ActingUserId={ActingUserId}", page, pageSize, actingUserId);

        var request = new ListCoursesRequest { Page = page, PageSize = pageSize, ActingUserId = actingUserId, ActingIsAdmin = isAdmin };
        var reply = await _courseServiceRpcClient.ListCoursesAsync(request);

        return new PagedCoursesDto { Items = reply.Courses.Adapt<List<CourseSummaryDto>>(), TotalCount = reply.TotalCount };
    }

    public async Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync()
    {
        _logger.LogInformation("GrpcCourseService::ListCategoriesAsync: called");

        var reply = await _courseServiceRpcClient.ListCategoriesAsync(new ListCategoriesRequest());
        return reply.Categories.Adapt<List<CategoryDto>>();
    }

    public async Task<CategoryDto> CreateCategoryAsync(string name)
    {
        _logger.LogInformation("GrpcCourseService::CreateCategoryAsync: called with Name={Name}", name);

        try
        {
            var reply = await _courseServiceRpcClient.CreateCategoryAsync(new CreateCategoryRequest { Name = name });
            return reply.Adapt<CategoryDto>();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
        {
            throw new ArgumentException(ex.Status.Detail);
        }
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        _logger.LogInformation("GrpcCourseService::DeleteCategoryAsync: called with Id={CategoryId}", id);

        try
        {
            var reply = await _courseServiceRpcClient.DeleteCategoryAsync(new DeleteCategoryRequest { Id = id });
            return reply.Deleted;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.FailedPrecondition)
        {
            throw new InvalidOperationException(ex.Status.Detail);
        }
    }

    public async Task<CourseDetailDto?> AddChapterAsync(int courseId, string title, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("GrpcCourseService::AddChapterAsync: called with CourseId={CourseId} ActingUserId={ActingUserId}", courseId, actingUserId);

        var request = new AddChapterRequest { CourseId = courseId, Title = title, ActingUserId = actingUserId, ActingIsAdmin = isAdmin };
        return await CallOrDefaultAsync(() => _courseServiceRpcClient.AddChapterAsync(request));
    }

    public async Task<CourseDetailDto?> RenameChapterAsync(int courseId, int chapterId, string title, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("GrpcCourseService::RenameChapterAsync: called with CourseId={CourseId} ChapterId={ChapterId} ActingUserId={ActingUserId}",
            courseId, chapterId, actingUserId);

        var request = new RenameChapterRequest { CourseId = courseId, ChapterId = chapterId, Title = title, ActingUserId = actingUserId, ActingIsAdmin = isAdmin };
        return await CallOrDefaultAsync(() => _courseServiceRpcClient.RenameChapterAsync(request));
    }

    public async Task<CourseDetailDto?> MoveChapterAsync(int courseId, int chapterId, AppMoveDirection direction, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("GrpcCourseService::MoveChapterAsync: called with CourseId={CourseId} ChapterId={ChapterId} Direction={Direction} ActingUserId={ActingUserId}",
            courseId, chapterId, direction, actingUserId);

        var request = new MoveChapterRequest
        {
            CourseId = courseId,
            ChapterId = chapterId,
            Direction = direction.Adapt<RpcMoveDirection>(),
            ActingUserId = actingUserId,
            ActingIsAdmin = isAdmin,
        };
        return await CallOrDefaultAsync(() => _courseServiceRpcClient.MoveChapterAsync(request));
    }

    public async Task<CourseDetailDto?> AddLessonAsync(int courseId, int chapterId, AddLessonDto dto, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("GrpcCourseService::AddLessonAsync: called with CourseId={CourseId} ChapterId={ChapterId} ActingUserId={ActingUserId}",
            courseId, chapterId, actingUserId);

        var request = new AddLessonRequest
        {
            CourseId = courseId,
            ChapterId = chapterId,
            Title = dto.Title,
            Content = dto.Content ?? string.Empty,
            IncludeInPreview = dto.IncludeInPreview,
            ActingUserId = actingUserId,
            ActingIsAdmin = isAdmin,
        };
        return await CallOrDefaultAsync(() => _courseServiceRpcClient.AddLessonAsync(request));
    }

    public async Task<CourseDetailDto?> UpdateLessonAsync(int courseId, int chapterId, int lessonId, UpdateLessonDto dto, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("GrpcCourseService::UpdateLessonAsync: called with CourseId={CourseId} ChapterId={ChapterId} LessonId={LessonId} ActingUserId={ActingUserId}",
            courseId, chapterId, lessonId, actingUserId);

        var request = new UpdateLessonRequest
        {
            CourseId = courseId,
            ChapterId = chapterId,
            LessonId = lessonId,
            Title = dto.Title,
            Content = dto.Content ?? string.Empty,
            IncludeInPreview = dto.IncludeInPreview,
            ActingUserId = actingUserId,
            ActingIsAdmin = isAdmin,
        };
        return await CallOrDefaultAsync(() => _courseServiceRpcClient.UpdateLessonAsync(request));
    }

    public async Task<CourseDetailDto?> MoveLessonAsync(int courseId, int chapterId, int lessonId, AppMoveDirection direction, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("GrpcCourseService::MoveLessonAsync: called with CourseId={CourseId} ChapterId={ChapterId} LessonId={LessonId} Direction={Direction} ActingUserId={ActingUserId}",
            courseId, chapterId, lessonId, direction, actingUserId);

        var request = new MoveLessonRequest
        {
            CourseId = courseId,
            ChapterId = chapterId,
            LessonId = lessonId,
            Direction = direction.Adapt<RpcMoveDirection>(),
            ActingUserId = actingUserId,
            ActingIsAdmin = isAdmin,
        };
        return await CallOrDefaultAsync(() => _courseServiceRpcClient.MoveLessonAsync(request));
    }

    public async Task<CourseDetailDto?> RemoveLessonAsync(int courseId, int chapterId, int lessonId, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("GrpcCourseService::RemoveLessonAsync: called with CourseId={CourseId} ChapterId={ChapterId} LessonId={LessonId} ActingUserId={ActingUserId}",
            courseId, chapterId, lessonId, actingUserId);

        var request = new RemoveLessonRequest { CourseId = courseId, ChapterId = chapterId, LessonId = lessonId, ActingUserId = actingUserId, ActingIsAdmin = isAdmin };
        return await CallOrDefaultAsync(() => _courseServiceRpcClient.RemoveLessonAsync(request));
    }

    public async Task<CourseDetailDto?> PublishCourseAsync(int courseId, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("GrpcCourseService::PublishCourseAsync: called with CourseId={CourseId} ActingUserId={ActingUserId}", courseId, actingUserId);

        var request = new PublishCourseRequest { CourseId = courseId, ActingUserId = actingUserId, ActingIsAdmin = isAdmin };
        return await CallOrDefaultAsync(() => _courseServiceRpcClient.PublishCourseAsync(request));
    }

    public async Task<CourseDetailDto?> ReplaceLessonPartsAsync(
        int courseId, int chapterId, int lessonId, IReadOnlyList<ReplaceLessonPartInputDto> parts, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("GrpcCourseService::ReplaceLessonPartsAsync: called with CourseId={CourseId} ChapterId={ChapterId} LessonId={LessonId} ActingUserId={ActingUserId}",
            courseId, chapterId, lessonId, actingUserId);

        var request = new ReplaceLessonPartsRequest
        {
            CourseId = courseId,
            ChapterId = chapterId,
            LessonId = lessonId,
            ActingUserId = actingUserId,
            ActingIsAdmin = isAdmin,
        };
        request.Parts.AddRange(parts.Select(ToLessonPartInput));

        return await CallOrDefaultAsync(() => _courseServiceRpcClient.ReplaceLessonPartsAsync(request));
    }

    // Mapster's top-level Adapt<T>() isn't trusted here to populate nested `repeated` fields
    // correctly (same reasoning as RpcCourseService.ToReply/ToEnrollmentReply on the server side) —
    // build the message scalar-first, then fill QuizQuestions/Options manually. Without this, quiz
    // parts round-trip with an empty QuizQuestions list, which the domain then rejects as invalid.
    private static LessonPartInput ToLessonPartInput(ReplaceLessonPartInputDto dto)
    {
        var input = dto.Adapt<LessonPartInput>();
        input.QuizQuestions.Clear();

        if (dto.QuizQuestions is not null)
        {
            input.QuizQuestions.AddRange(dto.QuizQuestions.Select(q =>
            {
                var question = q.Adapt<QuizQuestionInput>();
                question.Options.Clear();
                question.Options.AddRange(q.Options.Adapt<List<QuizOptionInput>>());
                return question;
            }));
        }

        input.Files.Clear();
        if (dto.Files is not null)
            input.Files.AddRange(dto.Files.Select(f => f.Adapt<LessonPartFileInput>()));

        return input;
    }

    private static async Task<CourseDetailDto?> CallOrDefaultAsync(Func<AsyncUnaryCall<CourseDetailReply>> call)
    {
        try
        {
            var reply = await call();
            return reply.Adapt<CourseDetailDto>();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            throw new CourseAuthorizationException(ex.Status.Detail);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.FailedPrecondition)
        {
            throw new InvalidOperationException(ex.Status.Detail);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
        {
            throw new ArgumentException(ex.Status.Detail);
        }
    }
}
