using Grpc.Core;
using LF.Application.Common.Exceptions;
using LF.Application.ModelDto.Enrollment;
using LF.Application.ModelDto.Promo;
using LF.Application.Services.Enrollment;
using LF.CourseService;
using Mapster;
using Microsoft.Extensions.Logging;
using AppEnrollmentStatus = LF.AppDomain.Models.Course.Enums.EnrollmentStatus;
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

    public async Task<EnrollmentDetailDto> EnrollAsync(int courseId, int actingUserId, string? promoCode)
    {
        _logger.LogInformation("GrpcEnrollmentService::EnrollAsync: called with CourseId={CourseId} ActingUserId={ActingUserId}", courseId, actingUserId);

        var request = new EnrollInCourseRequest { CourseId = courseId, ActingUserId = actingUserId, PromoCode = promoCode };

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
            // Covers both "you created this course" and "this course does not allow self-enrollment";
            // both surface as HTTP 403 at the endpoint.
            throw new EnrollmentModeException(ex.Status.Detail);
        }
    }

    public async Task<EnrollmentActivationDto> ActivatePaidEnrollmentAsync(int enrollmentId, decimal paidAmount)
    {
        _logger.LogInformation("GrpcEnrollmentService::ActivatePaidEnrollmentAsync: called with EnrollmentId={EnrollmentId} PaidAmount={PaidAmount}",
            enrollmentId, paidAmount);

        var request = new ConfirmEnrollmentPaymentRequest
        {
            EnrollmentId = enrollmentId,
            PaidAmount = paidAmount.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

        try
        {
            var reply = await _courseServiceRpcClient.ConfirmEnrollmentPaymentAsync(request);
            return new EnrollmentActivationDto
            {
                EnrollmentId = reply.EnrollmentId,
                CourseId = reply.CourseId,
                Status = (AppEnrollmentStatus)(int)reply.Status,
                PricePaid = string.IsNullOrEmpty(reply.PricePaid)
                    ? 0m
                    : decimal.Parse(reply.PricePaid, System.Globalization.CultureInfo.InvariantCulture),
            };
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.FailedPrecondition)
        {
            throw new InvalidOperationException(ex.Status.Detail);
        }
    }

    public async Task<PromoCodeValidationDto> ValidatePromoCodeAsync(string code, int courseId, int actingUserId)
    {
        _logger.LogInformation("GrpcEnrollmentService::ValidatePromoCodeAsync: called with CourseId={CourseId} ActingUserId={ActingUserId}", courseId, actingUserId);

        var request = new ValidatePromoCodeRequest { Code = code, CourseId = courseId, ActingUserId = actingUserId };
        var reply = await _courseServiceRpcClient.ValidatePromoCodeAsync(request);
        return reply.Adapt<PromoCodeValidationDto>();
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

    public async Task<QuizSubmissionDto?> SubmitQuizAttemptAsync(int id, int lessonId, int partId, IReadOnlyList<QuizAnswerInputDto> answers, int actingUserId, bool isAdmin)
    {
        _logger.LogInformation("GrpcEnrollmentService::SubmitQuizAttemptAsync: called with Id={Id} LessonId={LessonId} PartId={PartId} ActingUserId={ActingUserId}",
            id, lessonId, partId, actingUserId);

        var request = new SubmitQuizAttemptRequest { Id = id, LessonId = lessonId, PartId = partId, ActingUserId = actingUserId, ActingIsAdmin = isAdmin };
        request.Answers.AddRange(answers.Select(a =>
        {
            var input = new QuizAnswerInput { QuestionId = a.QuestionId };
            input.SelectedOptionIds.AddRange(a.SelectedOptionIds);
            return input;
        }));

        try
        {
            var reply = await _courseServiceRpcClient.SubmitQuizAttemptAsync(request);
            return ToQuizSubmissionDto(reply);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.PermissionDenied)
        {
            throw new EnrollmentAuthorizationException(ex.Status.Detail);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.FailedPrecondition)
        {
            throw new InvalidOperationException(ex.Status.Detail);
        }
    }

    private static QuizSubmissionDto ToQuizSubmissionDto(QuizAttemptResultReply reply) => new()
    {
        Result = new QuizAttemptResultDto
        {
            ScorePercent = reply.ScorePercent,
            Passed = reply.Passed,
            Questions = reply.Questions.Adapt<List<QuizQuestionResultDto>>(),
        },
        Enrollment = reply.Enrollment.Adapt<EnrollmentDetailDto>(),
    };

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

    public async Task<CoursePreviewDto?> GetCoursePreviewAsync(int courseId, int actingUserId)
    {
        _logger.LogInformation("GrpcEnrollmentService::GetCoursePreviewAsync: called with CourseId={CourseId} ActingUserId={ActingUserId}", courseId, actingUserId);

        var request = new GetCoursePreviewRequest { CourseId = courseId, ActingUserId = actingUserId };
        try
        {
            var reply = await _courseServiceRpcClient.GetCoursePreviewAsync(request);
            return reply.Adapt<CoursePreviewDto>();
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
