using LF.AppDomain.Entities.User;
using LF.Application.ModelDto.User;
using Mapster;

namespace LF.Application.Common.Mapping;

internal sealed class UserMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GetOrCreateUserDto, DbUser>()
            .Map(dest => dest.LastName, src => src.LastName ?? string.Empty)
            .Ignore(dest => dest.Id, dest => dest.Role, dest => dest.CreatedAt);

        config.NewConfig<DbUser, UserDto>();
    }
}
