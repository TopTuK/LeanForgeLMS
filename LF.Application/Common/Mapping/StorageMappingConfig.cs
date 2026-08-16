using LF.AppDomain.Entities.Storage;
using LF.Application.ModelDto.Storage;
using Mapster;

namespace LF.Application.Common.Mapping;

internal sealed class StorageMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<StorageObject, StorageObjectDto>();
    }
}
