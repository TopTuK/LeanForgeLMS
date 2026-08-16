namespace LF.Infrastructure.Services.Storage;

internal sealed class MinioStorageOptions
{
    public string AvatarsBucketName { get; set; } = "avatars";
    public string StorageBucketName { get; set; } = "storage";
}
