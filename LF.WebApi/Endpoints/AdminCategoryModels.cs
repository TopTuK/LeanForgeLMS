using FluentValidation;

namespace LF.WebApi.Endpoints;

public sealed record CreateCategoryRequest(string Name);

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
