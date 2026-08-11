using FluentValidation;

namespace LF.WebApi.Endpoints;

public sealed record AdminUserResponse(int Id, string Email, string FirstName, string LastName, string Role, DateTime CreatedAt);

public sealed record PagedAdminUserResponse(IReadOnlyList<AdminUserResponse> Items, int TotalCount, int Page, int PageSize);

public sealed record UpdateAdminUserInfoRequest(string FirstName, string? LastName);

public sealed class UpdateAdminUserInfoRequestValidator : AbstractValidator<UpdateAdminUserInfoRequest>
{
    public UpdateAdminUserInfoRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
    }
}

public sealed record UpdateAdminUserRoleRequest(string Role);
