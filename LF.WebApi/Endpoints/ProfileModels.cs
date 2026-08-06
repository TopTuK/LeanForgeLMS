using FluentValidation;

namespace LF.WebApi.Endpoints;

public sealed record ProfileResponse(string FirstName, string LastName, string Email);

public sealed record UpdateProfileRequest(string FirstName, string? LastName);

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
    }
}
