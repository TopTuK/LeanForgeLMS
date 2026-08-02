using Google.Protobuf.WellKnownTypes;
using LF.Application.ModelDto.User;
using LF.IdentityService;
using Mapster;

namespace Lf.IdentityService.Common.Mapping;

internal sealed class RpcUserMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GetUserRequest, GetOrCreateUserDto>();

        // domain UserRole and proto UserRole share numeric values but not member names, so name-based mapping can't be trusted.
        config.NewConfig<UserDto, GetUserReply>()
            .Map(dest => dest.Role, src => (UserRole)(int)src.Role)
            .Map(dest => dest.CreatedAt, src => Timestamp.FromDateTime(src.CreatedAt.ToUniversalTime()));
    }
}
