using LF.Application.ModelDto.Authentication;
using LF.Application.ModelDto.User;
using LF.IdentityService;
using Mapster;
using DomainUserRole = LF.AppDomain.Models.User.Enums.UserRole;

namespace LF.Infrastructure.Common.Mapping;

internal sealed class GrpcUserMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // proto FirstName isn't StringValue-wrapped (unlike LastName), so it's a non-nullable string on the wire.
        config.NewConfig<UserAuthentificationDto, GetUserRequest>()
            .Map(dest => dest.FirstName, src => src.FirstName ?? string.Empty);

        // domain UserRole and proto UserRole share numeric values but not member names, so name-based mapping can't be trusted.
        config.NewConfig<GetUserReply, UserDto>()
            .Map(dest => dest.Role, src => (DomainUserRole)(int)src.Role)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt.ToDateTime());
    }
}
