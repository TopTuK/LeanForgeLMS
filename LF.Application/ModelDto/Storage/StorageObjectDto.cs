using LF.AppDomain.Models.Storage.Enums;

namespace LF.Application.ModelDto.Storage;

public sealed class StorageObjectDto
{
    public int Id { get; init; }
    public StorageObjectType ObjectType { get; init; }
    public string ObjectKey { get; init; } = null!;
    public string ContentType { get; init; } = null!;
    public long SizeBytes { get; init; }
    public int CreatedByUserId { get; init; }
    public DateTime CreatedAt { get; init; }
}
