using System.Globalization;
using Grpc.Core;
using LF.Application.Common.Exceptions;
using LF.Application.ModelDto.Course;
using LF.Application.ModelDto.Enrollment;
using LF.Application.ModelDto.Promo;
using LF.Application.Services.Course;
using LF.Application.Services.Enrollment;
using LF.Application.Services.Promo;
using LF.CourseService;
using Mapster;
using AppEnrollmentStatusFilter = LF.Application.ModelDto.Enrollment.EnrollmentStatusFilter;
using AppLessonPartType = LF.AppDomain.Models.Course.Enums.LessonPartType;
using AppMoveDirection = LF.Application.ModelDto.Course.MoveDirection;
using AppQuestionType = LF.AppDomain.Models.Course.Enums.QuestionType;

namespace LF.CourseService.Services;

public class RpcCourseService(
    ILogger<RpcCourseService> logger,
    ICourseService courseService,
    IEnrollmentService enrollmentService,
    IPromoCodeService promoCodeService) : CourseServiceRpc.CourseServiceRpcBase
{
    private readonly ILogger<RpcCourseService> _logger = logger;
    private readonly ICourseService _courseService = courseService;
    private readonly IEnrollmentService _enrollmentService = enrollmentService;
    private readonly IPromoCodeService _promoCodeService = promoCodeService;

    public override async Task<CourseDetailReply> CreateCourse(CreateCourseRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::CreateCourse: called with Title={Title} CreatedByUserId={CreatedByUserId}", request.Title, request.CreatedByUserId);

        var dto = request.Adapt<CreateCourseDto>();

        try
        {
            var course = await _courseService.CreateCourseAsync(dto, request.CreatedByUserId);
            return ToReply(course);
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<CourseDetailReply> GetCourse(GetCourseRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::GetCourse: called with Id={CourseId} ActingUserId={ActingUserId}", request.Id, request.ActingUserId);

        var course = await GuardedAsync(() => _courseService.GetCourseAsync(request.Id, request.ActingUserId, request.ActingIsAdmin));
        return ToReply(course);
    }

    public override async Task<ListCoursesReply> ListCourses(ListCoursesRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::ListCourses: called with Page={Page} PageSize={PageSize} ActingUserId={ActingUserId}",
            request.Page, request.PageSize, request.ActingUserId);

        var paged = await _courseService.ListCoursesAsync(request.Page, request.PageSize, request.ActingUserId, request.ActingIsAdmin);

        var reply = new ListCoursesReply { TotalCount = paged.TotalCount };
        reply.Courses.AddRange(paged.Items.Adapt<List<CourseSummaryReply>>());
        return reply;
    }

    public override async Task<ListCategoriesReply> ListCategories(ListCategoriesRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::ListCategories: called");

        var categories = await _courseService.ListCategoriesAsync();

        var reply = new ListCategoriesReply();
        reply.Categories.AddRange(categories.Adapt<List<CategoryReply>>());
        return reply;
    }

    public override async Task<CategoryReply> CreateCategory(CreateCategoryRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::CreateCategory: called with Name={Name}", request.Name);

        try
        {
            var category = await _courseService.CreateCategoryAsync(request.Name);
            return category.Adapt<CategoryReply>();
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<DeleteCategoryReply> DeleteCategory(DeleteCategoryRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::DeleteCategory: called with Id={CategoryId}", request.Id);

        try
        {
            var deleted = await _courseService.DeleteCategoryAsync(request.Id);
            return new DeleteCategoryReply { Deleted = deleted };
        }
        catch (CategoryProtectedException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    public override async Task<CourseDetailReply> AddChapter(AddChapterRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::AddChapter: called with CourseId={CourseId} ActingUserId={ActingUserId}", request.CourseId, request.ActingUserId);

        var course = await GuardedAsync(() =>
            _courseService.AddChapterAsync(request.CourseId, request.Title, request.ActingUserId, request.ActingIsAdmin));
        return ToReply(course);
    }

    public override async Task<CourseDetailReply> RenameChapter(RenameChapterRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::RenameChapter: called with CourseId={CourseId} ChapterId={ChapterId} ActingUserId={ActingUserId}",
            request.CourseId, request.ChapterId, request.ActingUserId);

        var course = await GuardedAsync(() =>
            _courseService.RenameChapterAsync(request.CourseId, request.ChapterId, request.Title, request.ActingUserId, request.ActingIsAdmin));
        return ToReply(course);
    }

    public override async Task<CourseDetailReply> MoveChapter(MoveChapterRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::MoveChapter: called with CourseId={CourseId} ChapterId={ChapterId} Direction={Direction} ActingUserId={ActingUserId}",
            request.CourseId, request.ChapterId, request.Direction, request.ActingUserId);

        var direction = request.Direction.Adapt<AppMoveDirection>();
        var course = await GuardedAsync(() =>
            _courseService.MoveChapterAsync(request.CourseId, request.ChapterId, direction, request.ActingUserId, request.ActingIsAdmin));
        return ToReply(course);
    }

    public override async Task<CourseDetailReply> AddLesson(AddLessonRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::AddLesson: called with CourseId={CourseId} ChapterId={ChapterId} ActingUserId={ActingUserId}",
            request.CourseId, request.ChapterId, request.ActingUserId);

        var dto = new AddLessonDto { Title = request.Title, Content = request.Content, IncludeInPreview = request.IncludeInPreview };
        var course = await GuardedAsync(() =>
            _courseService.AddLessonAsync(request.CourseId, request.ChapterId, dto, request.ActingUserId, request.ActingIsAdmin));
        return ToReply(course);
    }

    public override async Task<CourseDetailReply> UpdateLesson(UpdateLessonRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::UpdateLesson: called with CourseId={CourseId} ChapterId={ChapterId} LessonId={LessonId} ActingUserId={ActingUserId}",
            request.CourseId, request.ChapterId, request.LessonId, request.ActingUserId);

        var dto = new UpdateLessonDto { Title = request.Title, Content = request.Content, IncludeInPreview = request.IncludeInPreview };
        var course = await GuardedAsync(() =>
            _courseService.UpdateLessonAsync(request.CourseId, request.ChapterId, request.LessonId, dto, request.ActingUserId, request.ActingIsAdmin));
        return ToReply(course);
    }

    public override async Task<CourseDetailReply> MoveLesson(MoveLessonRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::MoveLesson: called with CourseId={CourseId} ChapterId={ChapterId} LessonId={LessonId} Direction={Direction} ActingUserId={ActingUserId}",
            request.CourseId, request.ChapterId, request.LessonId, request.Direction, request.ActingUserId);

        var direction = request.Direction.Adapt<AppMoveDirection>();
        var course = await GuardedAsync(() =>
            _courseService.MoveLessonAsync(request.CourseId, request.ChapterId, request.LessonId, direction, request.ActingUserId, request.ActingIsAdmin));
        return ToReply(course);
    }

    public override async Task<CourseDetailReply> RemoveLesson(RemoveLessonRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::RemoveLesson: called with CourseId={CourseId} ChapterId={ChapterId} LessonId={LessonId} ActingUserId={ActingUserId}",
            request.CourseId, request.ChapterId, request.LessonId, request.ActingUserId);

        var course = await GuardedAsync(() =>
            _courseService.RemoveLessonAsync(request.CourseId, request.ChapterId, request.LessonId, request.ActingUserId, request.ActingIsAdmin));
        return ToReply(course);
    }

    public override async Task<CourseDetailReply> PublishCourse(PublishCourseRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::PublishCourse: called with CourseId={CourseId} ActingUserId={ActingUserId}", request.CourseId, request.ActingUserId);

        var course = await GuardedAsync(() =>
            _courseService.PublishCourseAsync(request.CourseId, request.ActingUserId, request.ActingIsAdmin));
        return ToReply(course);
    }

    public override async Task<CourseDetailReply> ReplaceLessonParts(ReplaceLessonPartsRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::ReplaceLessonParts: called with CourseId={CourseId} ChapterId={ChapterId} LessonId={LessonId} ActingUserId={ActingUserId}",
            request.CourseId, request.ChapterId, request.LessonId, request.ActingUserId);

        var parts = request.Parts.Select(p => new ReplaceLessonPartInputDto
        {
            PartType = p.PartType.Adapt<AppLessonPartType>(),
            Html = p.Html,
            StorageObjectId = p.StorageObjectId,
            QuizPassThresholdPercent = p.QuizPassThresholdPercent,
            QuizQuestions = p.QuizQuestions.Count == 0 ? null : [.. p.QuizQuestions.Select(q => new QuizQuestionInputDto
            {
                Text = q.Text,
                QuestionType = q.QuestionType.Adapt<AppQuestionType>(),
                SortOrder = q.SortOrder,
                Options = [.. q.Options.Select(o => new QuizOptionInputDto
                {
                    Text = o.Text,
                    IsCorrect = o.IsCorrect,
                    SortOrder = o.SortOrder,
                })],
            })],
            Files = p.Files.Count == 0 ? null : [.. p.Files.Select(f => new LessonPartFileInputDto
            {
                FileName = f.FileName,
                StorageObjectId = f.StorageObjectId,
            })],
        }).ToList();

        var course = await GuardedAsync(() =>
            _courseService.ReplaceLessonPartsAsync(request.CourseId, request.ChapterId, request.LessonId, parts, request.ActingUserId, request.ActingIsAdmin));
        return ToReply(course);
    }

    public override async Task<ListCatalogReply> ListCatalog(ListCatalogRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::ListCatalog: called with Page={Page} PageSize={PageSize} ActingUserId={ActingUserId}",
            request.Page, request.PageSize, request.ActingUserId);

        var paged = await _enrollmentService.BrowseCatalogAsync(request.Page, request.PageSize, request.ActingUserId);

        var reply = new ListCatalogReply { TotalCount = paged.TotalCount };
        reply.Items.AddRange(paged.Items.Adapt<List<CourseCatalogItemReply>>());
        return reply;
    }

    public override async Task<EnrollmentDetailReply> EnrollInCourse(EnrollInCourseRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::EnrollInCourse: called with CourseId={CourseId} ActingUserId={ActingUserId}", request.CourseId, request.ActingUserId);

        try
        {
            var enrollment = await _enrollmentService.EnrollAsync(request.CourseId, request.ActingUserId, request.PromoCode);
            return ToEnrollmentReply(enrollment);
        }
        catch (SelfEnrollmentException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (EnrollmentModeException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    public override async Task<EnrollmentSummaryReply> EnrollUser(EnrollUserRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::EnrollUser: called with CourseId={CourseId} TargetUserId={TargetUserId} ActingUserId={ActingUserId}",
            request.CourseId, request.TargetUserId, request.ActingUserId);

        try
        {
            var summary = await _courseService.EnrollUserAsync(request.CourseId, request.TargetUserId, request.ActingUserId, request.ActingIsAdmin);
            return summary is null
                ? throw new RpcException(new Status(StatusCode.NotFound, "Course not found."))
                : summary.Adapt<EnrollmentSummaryReply>();
        }
        catch (CourseAuthorizationException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    public override async Task<PromoCodeValidationReply> ValidatePromoCode(ValidatePromoCodeRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::ValidatePromoCode: called with CourseId={CourseId} ActingUserId={ActingUserId}", request.CourseId, request.ActingUserId);

        var result = await _enrollmentService.ValidatePromoCodeAsync(request.Code, request.CourseId, request.ActingUserId);

        return new PromoCodeValidationReply
        {
            IsValid = result.IsValid,
            Reason = result.Reason,
            OriginalPrice = result.OriginalPrice.ToString(CultureInfo.InvariantCulture),
            DiscountedPrice = result.DiscountedPrice.ToString(CultureInfo.InvariantCulture),
            DiscountAmount = result.DiscountAmount.ToString(CultureInfo.InvariantCulture),
        };
    }

    public override async Task<PromoCodeReply> CreatePromoCode(CreatePromoCodeRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::CreatePromoCode: called with Code={Code} CreatedByUserId={CreatedByUserId}", request.Code, request.CreatedByUserId);

        var dto = new CreatePromoCodeDto
        {
            Code = request.Code,
            DiscountType = (LF.AppDomain.Models.Course.Enums.PromoCodeDiscountType)(int)request.DiscountType,
            DiscountValue = decimal.Parse(request.DiscountValue, CultureInfo.InvariantCulture),
            CourseId = request.CourseId,
            ExpiresAt = request.ExpiresAt?.ToDateTime(),
            MaxRedemptions = request.MaxRedemptions,
        };

        try
        {
            var promoCode = await _promoCodeService.CreatePromoCodeAsync(dto, request.CreatedByUserId);
            return promoCode.Adapt<PromoCodeReply>();
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (FormatException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    public override async Task<ListPromoCodesReply> ListPromoCodes(ListPromoCodesRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::ListPromoCodes: called with Page={Page} PageSize={PageSize}", request.Page, request.PageSize);

        var paged = await _promoCodeService.ListPromoCodesAsync(request.Page, request.PageSize);

        var reply = new ListPromoCodesReply { TotalCount = paged.TotalCount };
        reply.Items.AddRange(paged.Items.Select(p => p.Adapt<PromoCodeReply>()));
        return reply;
    }

    public override async Task<DeactivatePromoCodeReply> DeactivatePromoCode(DeactivatePromoCodeRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::DeactivatePromoCode: called with Id={PromoCodeId}", request.Id);

        var deactivated = await _promoCodeService.DeactivatePromoCodeAsync(request.Id);
        return new DeactivatePromoCodeReply { Deactivated = deactivated };
    }

    public override async Task<ListMyEnrollmentsReply> ListMyEnrollments(ListMyEnrollmentsRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::ListMyEnrollments: called with ActingUserId={ActingUserId} Status={Status}", request.ActingUserId, request.Status);

        var status = (AppEnrollmentStatusFilter)(int)request.Status;
        var items = await _enrollmentService.ListMyEnrollmentsAsync(request.ActingUserId, status);

        var reply = new ListMyEnrollmentsReply();
        reply.Items.AddRange(items.Adapt<List<EnrollmentSummaryReply>>());
        return reply;
    }

    public override async Task<EnrollmentDetailReply> GetEnrollment(GetEnrollmentRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::GetEnrollment: called with Id={Id} ActingUserId={ActingUserId}", request.Id, request.ActingUserId);

        var enrollment = await GuardedEnrollmentAsync(() =>
            _enrollmentService.GetEnrollmentAsync(request.Id, request.ActingUserId, request.ActingIsAdmin));
        return ToEnrollmentReply(enrollment);
    }

    public override async Task<EnrollmentDetailReply> CompleteLesson(CompleteLessonRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::CompleteLesson: called with Id={Id} LessonId={LessonId} ActingUserId={ActingUserId}",
            request.Id, request.LessonId, request.ActingUserId);

        var enrollment = await GuardedEnrollmentAsync(() =>
            _enrollmentService.CompleteLessonAsync(request.Id, request.LessonId, request.ActingUserId, request.ActingIsAdmin));
        return ToEnrollmentReply(enrollment);
    }

    public override async Task<QuizAttemptResultReply> SubmitQuizAttempt(SubmitQuizAttemptRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::SubmitQuizAttempt: called with Id={Id} LessonId={LessonId} PartId={PartId} ActingUserId={ActingUserId}",
            request.Id, request.LessonId, request.PartId, request.ActingUserId);

        var answers = request.Answers.Select(a => new QuizAnswerInputDto
        {
            QuestionId = a.QuestionId,
            SelectedOptionIds = [.. a.SelectedOptionIds],
        }).ToList();

        var submission = await GuardedQuizAsync(() =>
            _enrollmentService.SubmitQuizAttemptAsync(request.Id, request.LessonId, request.PartId, answers, request.ActingUserId, request.ActingIsAdmin));

        var reply = submission.Result.Adapt<QuizAttemptResultReply>();
        reply.Enrollment = ToEnrollmentReply(submission.Enrollment);
        return reply;
    }

    public override async Task<CourseCoverReply> GetCourseCover(GetCourseCoverRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::GetCourseCover: called with CourseId={CourseId}", request.CourseId);

        var cover = await _enrollmentService.GetCourseCoverAsync(request.CourseId);
        if (cover is null)
            throw new RpcException(new Status(StatusCode.NotFound, "Course not found or has no cover image."));

        return new CourseCoverReply { CoverImageKey = cover.CoverImageKey, CoverImageContentType = cover.CoverImageContentType };
    }

    public override async Task<CoursePreviewReply> GetCoursePreview(GetCoursePreviewRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcCourseService::GetCoursePreview: called with CourseId={CourseId} ActingUserId={ActingUserId}", request.CourseId, request.ActingUserId);

        var preview = await _enrollmentService.GetCoursePreviewAsync(request.CourseId, request.ActingUserId);
        if (preview is null)
            throw new RpcException(new Status(StatusCode.NotFound, "Course not found or not published."));

        return ToCoursePreviewReply(preview);
    }

    private static async Task<CourseDetailDto> GuardedAsync(Func<Task<CourseDetailDto?>> operation)
    {
        try
        {
            var course = await operation();
            return course ?? throw new RpcException(new Status(StatusCode.NotFound, "Course, chapter or lesson not found."));
        }
        catch (CourseAuthorizationException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    private static async Task<QuizSubmissionDto> GuardedQuizAsync(Func<Task<QuizSubmissionDto?>> operation)
    {
        try
        {
            var submission = await operation();
            return submission ?? throw new RpcException(new Status(StatusCode.NotFound, "Enrollment, lesson or quiz part not found."));
        }
        catch (EnrollmentAuthorizationException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    private static async Task<EnrollmentDetailDto> GuardedEnrollmentAsync(Func<Task<EnrollmentDetailDto?>> operation)
    {
        try
        {
            var enrollment = await operation();
            return enrollment ?? throw new RpcException(new Status(StatusCode.NotFound, "Enrollment or course not found."));
        }
        catch (EnrollmentAuthorizationException ex)
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, ex.Message));
        }
        catch (QuizCompletionRequiredException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
    }

    // Same nested-repeated-field caveat as Chapters/Lessons/Parts (see ToReply) one level
    // deeper — LessonPartReply.QuizQuestions[].Options[] needs the same manual treatment.
    private static LessonPartReply ToLessonPartReply(LessonPartDto dto)
    {
        var reply = dto.Adapt<LessonPartReply>();
        reply.QuizQuestions.Clear();
        reply.QuizQuestions.AddRange(dto.QuizQuestions.Select(q =>
        {
            var question = q.Adapt<QuizQuestionReply>();
            question.Options.Clear();
            question.Options.AddRange(q.Options.Adapt<List<QuizOptionReply>>());
            return question;
        }));

        reply.Files.Clear();
        reply.Files.AddRange(dto.Files.Select(f => f.Adapt<LessonPartFileReply>()));

        return reply;
    }

    // Mapster's top-level Adapt<T>() isn't trusted here to populate nested `repeated` fields
    // correctly (see ToReply(CourseDetailDto) for the same reasoning) — build the reply
    // scalar-first, then fill Chapters/Lessons manually.
    private static EnrollmentDetailReply ToEnrollmentReply(EnrollmentDetailDto dto)
    {
        var reply = dto.Adapt<EnrollmentDetailReply>();
        reply.Chapters.Clear();

        foreach (var chapterDto in dto.Chapters)
        {
            var chapterReply = chapterDto.Adapt<EnrollmentChapterReply>();
            chapterReply.Lessons.Clear();

            foreach (var lessonDto in chapterDto.Lessons)
            {
                var lessonReply = lessonDto.Adapt<EnrollmentLessonReply>();
                lessonReply.Parts.Clear();
                lessonReply.Parts.AddRange(lessonDto.Parts.Select(ToLessonPartReply));
                chapterReply.Lessons.Add(lessonReply);
            }

            reply.Chapters.Add(chapterReply);
        }

        return reply;
    }

    // Same nested-repeated-field caveat as ToReply/ToEnrollmentReply — build scalar-first,
    // then fill Chapters/Lessons/Parts manually.
    private static CoursePreviewReply ToCoursePreviewReply(CoursePreviewDto dto)
    {
        var reply = dto.Adapt<CoursePreviewReply>();
        reply.Chapters.Clear();

        foreach (var chapterDto in dto.Chapters)
        {
            var chapterReply = chapterDto.Adapt<CoursePreviewChapterReply>();
            chapterReply.Lessons.Clear();

            foreach (var lessonDto in chapterDto.Lessons)
            {
                var lessonReply = lessonDto.Adapt<CoursePreviewLessonReply>();
                lessonReply.Parts.Clear();
                lessonReply.Parts.AddRange(lessonDto.Parts.Select(ToLessonPartReply));
                chapterReply.Lessons.Add(lessonReply);
            }

            reply.Chapters.Add(chapterReply);
        }

        return reply;
    }

    // Mapster's top-level Adapt<T>() isn't trusted here to populate nested `repeated` fields
    // correctly (see ListCourses/ListCategories using explicit AddRange for the same reason) —
    // build the reply scalar-first, then fill Chapters/Lessons manually.
    private static CourseDetailReply ToReply(CourseDetailDto dto)
    {
        var reply = dto.Adapt<CourseDetailReply>();
        reply.Chapters.Clear();

        foreach (var chapterDto in dto.Chapters)
        {
            var chapterReply = chapterDto.Adapt<ChapterReply>();
            chapterReply.Lessons.Clear();

            foreach (var lessonDto in chapterDto.Lessons)
            {
                var lessonReply = lessonDto.Adapt<LessonReply>();
                lessonReply.Parts.Clear();
                lessonReply.Parts.AddRange(lessonDto.Parts.Select(ToLessonPartReply));
                chapterReply.Lessons.Add(lessonReply);
            }

            reply.Chapters.Add(chapterReply);
        }

        return reply;
    }
}
