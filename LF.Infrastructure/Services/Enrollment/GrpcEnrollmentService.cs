using Grpc.Core;
using LF.Application.Common.Exceptions;
using LF.Application.ModelDto.Enrollment;
using LF.Application.Services.Enrollment;
using LF.CourseService;
using Mapster;
using Microsoft.Extensions.Logging;
using AppEnrollmentStatusFilter = LF.Application.ModelDto.Enrollment.EnrollmentStatusFilter;
using RpcEnrollmentStatusFilter = LF.CourseService.EnrollmentStatusFilter;

namespace LF.Infrastructure.Services.Enrollment;

internal sealed class GrpcEnrollmentService(ILogger<GrpcEnrollmentService> logger,
    CourseServiceRpc.CourseServiceRpcClient courseServiceRpcClient) : IGrpcEnrollmentService
{
    private readonly ILogger<GrpcEnrollmentService> _logger = logger;
    private readonly CourseServiceRpc.CourseServiceRpcClient _courseServiceRpcClient = courseServiceRpcClient;

    public async Task<PagedCourseCatalogDto> BrowseCatalogAsync(int page, int pageSize, int actingUserId)
    {
        _logger.LogInformation("GrpcEnrollmentService::BrowseCatalogAsync: called with Page={Page} PageSize={PageSize} ActingUserId={ActingUserId}",
            page, pageSize, actingUserId);

        var request = new ListCatalogRequest { Page = page, PageSize = pageSize, ActingUserId = actingUserId };
        var reply = await _courseServiceRpcClient.ListCatalogAsync(request);

        return new PagedCourseCatalogDto { Items = reply.Items.Adapt<List<CourseCatalogItemDto>>(), TotalCount = reply.TotalCount };
    }

    public async Task<EnrollmentDetailDto> EnrollAsync(int courseId, int actingUserId)
    {
        _logger.LogInformation("GrpcEnrollmentService::EnrollAsync: called with CourseId={CourseId} ActingUserId={ActingUserId}", courseId, actingUserId);

        var request = new EnrollInCourseRequest { CourseId = courseId, ActingUserId = actingUserId };

        try
        {
            var reply = await _courseServiceRpcClient.EnrollInCourseAsync(request);
            return reply.Adapt<EnrollmentDetailDto>();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.FailedPrecondition)
        {
            throw new InvalidOperationException(ex.Status.Detail);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw new InvalidOperationException(ex.Status.Detail);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            throw new SelfEnrollmentException(ex.Status.Detail);
        }
    }

    public async Task<IReadOnlyList<EnrollmentSummaryDto>> ListMyEnrollmentsAsync(int actingUserId, AppEnrollmentStatusFilter status)
    {
        _logger.LogInformation("GrpcEnrollmentService::ListMyEnrollmentsAsync: called with ActingUserId={ActingUserId} Status={Status}", actingUserId, status);

        var request = new ListMyEnrollmentsRequest { ActingUserId = actingUserId, Status = (RpcEnrollmentStatusFilter)(int)status };
        var reply = await _courseServiceRpcClient.ListMyEnrollmentsAsync(request);

        return reply.Items.Adapt<List<EnrollmentSummaryDto>>();
    }

    public async Task<EnrollmentDetailDto?> GetEnrollmentAsync(int id, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("GrpcEnrollmentService::GetEnrollmentAsync: called with Id={Id} ActingUserId={ActingUserId}", id, actingUserId);

        var request = new GetEnrollmentRequest { Id = id, ActingUserId = actingUserId, ActingIsAdmin = isAdmin };
        return await CallOrDefaultAsync(() => _courseServiceRpcClient.GetEnrollmentAsync(request));
    }

    public async Task<EnrollmentDetailDto?> CompleteLessonAsync(int id, int lessonId, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("GrpcEnrollmentService::CompleteLessonAsync: called with Id={Id} LessonId={LessonId} ActingUserId={ActingUserId}",
            id, lessonId, actingUserId);

        var request = new CompleteLessonRequest { Id = id, LessonId = lessonId, ActingUserId = actingUserId, ActingIsAdmin = isAdmin };

        try
        {
            return await CallOrDefaultAsync(() => _courseServiceRpcClient.CompleteLessonAsync(request));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.FailedPrecondition)
        {
            throw new InvalidOperationException(ex.Status.Detail);
        }
    }

    public async Task<CourseCoverDto?> GetCourseCoverAsync(int courseId)
    {
        _logger.LogInformation("GrpcEnrollmentService::GetCourseCoverAsync: called with CourseId={CourseId}", courseId);

        var request = new GetCourseCoverRequest { CourseId = courseId };
        try
        {
            var reply = await _courseServiceRpcClient.GetCourseCoverAsync(request);
            return reply.CoverImageKey is null ? null : new CourseCoverDto { CoverImageKey = reply.CoverImageKey, CoverImageContentType = reply.CoverImageContentType };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    private static async Task<EnrollmentDetailDto?> CallOrDefaultAsync(Func<AsyncUnaryCall<EnrollmentDetailReply>> call)
    {
        try
        {
            var reply = await call();
            return reply.Adapt<EnrollmentDetailDto>();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            throw new EnrollmentAuthorizationException(ex.Status.Detail);
        }
    }
}
