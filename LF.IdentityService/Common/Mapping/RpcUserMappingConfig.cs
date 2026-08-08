using Google.Protobuf.WellKnownTypes;
using LF.Application.ModelDto.User;
using LF.IdentityService;
using Mapster;
using DomainUserRole = LF.AppDomain.Models.User.Enums.UserRole;

namespace Lf.IdentityService.Common.Mapping;

internal sealed class RpcUserMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GetUserRequest, GetOrCreateUserDto>();
        config.NewConfig<UpdateUserProfileRequest, UpdateUserNameDto>();
        config.NewConfig<UpdateUserAvatarRequest, UpdateUserAvatarDto>();

        // domain UserRole and proto UserRole share numeric values but not member names, so name-based mapping can't be trusted.
        config.NewConfig<EnsureUserWithRoleRequest, EnsureUserWithRoleDto>()
            .Map(dest => dest.Role, src => (DomainUserRole)(int)src.Role);

        config.NewConfig<UserDto, GetUserReply>()
            .Map(dest => dest.Role, src => (UserRole)(int)src.Role)
            .Map(dest => dest.CreatedAt, src => Timestamp.FromDateTime(src.CreatedAt.ToUniversalTime()));
    }
}
