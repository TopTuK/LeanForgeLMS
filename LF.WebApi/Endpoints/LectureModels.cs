using FluentValidation;
using LF.AppDomain.Entities.Groups;
using LF.Application.ModelDto.Lectures;

namespace LF.WebApi.Endpoints;

// StartsAt should be sent as UTC ("...Z"); the SPA converts from the instructor's local time.
public sealed record LectureRequest(
    string Title,
    string? Description,
    DateTime StartsAt,
    int DurationMinutes,
    string? MeetingUrl,
    IReadOnlyList<int> GroupIds)
{
    public LectureInputDto ToDto() => new()
    {
        Title = Title,
        Description = Description,
        StartsAt = StartsAt,
        DurationMinutes = DurationMinutes,
        MeetingUrl = MeetingUrl,
        GroupIds = GroupIds,
    };
}

public sealed class LectureRequestValidator : AbstractValidator<LectureRequest>
{
    public const int MaxGroups = 100;

    public LectureRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Lecture.MaxTitleLength);
        RuleFor(x => x.Description).MaximumLength(Lecture.MaxDescriptionLength);
        RuleFor(x => x.StartsAt).NotEmpty();
        RuleFor(x => x.DurationMinutes).InclusiveBetween(Lecture.MinDurationMinutes, Lecture.MaxDurationMinutes);
        RuleFor(x => x.MeetingUrl)
            .MaximumLength(Lecture.MaxMeetingUrlLength)
            .Must(BeHttpUrl).WithMessage("Meeting URL must be an absolute http or https link.")
            .When(x => !string.IsNullOrWhiteSpace(x.MeetingUrl));
        RuleFor(x => x.GroupIds).NotEmpty().Must(ids => ids.Count <= MaxGroups)
            .WithMessage($"A lecture can be assigned to at most {MaxGroups} groups.");
        RuleForEach(x => x.GroupIds).GreaterThan(0);
    }

    private static bool BeHttpUrl(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}

public sealed record LectureGroupResponse(int Id, string Name);

public sealed record LectureResponse(
    int Id,
    int CourseId,
    string CourseTitle,
    string Title,
    string? Description,
    DateTime StartsAt,
    int DurationMinutes,
    string? MeetingUrl,
    bool IsCancelled,
    IReadOnlyList<LectureGroupResponse> Groups,
    bool IsTeaching);

public static class LectureResponseMapper
{
    public static LectureResponse ToResponse(LectureDto dto) =>
        new(dto.Id, dto.CourseId, dto.CourseTitle, dto.Title, dto.Description, dto.StartsAt, dto.DurationMinutes,
            dto.MeetingUrl, dto.IsCancelled, [.. dto.Groups.Select(g => new LectureGroupResponse(g.Id, g.Name))], dto.IsTeaching);
}
