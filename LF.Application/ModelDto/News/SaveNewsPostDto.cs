using LF.AppDomain.Models.News.Enums;

namespace LF.Application.ModelDto.News;

public sealed class SaveNewsPostDto
{
    public string Title { get; init; } = null!;
    public string Html { get; init; } = null!;
    public NewsVisibility Visibility { get; init; }
    public bool IsPublished { get; init; }

    // Ordered: the gallery renders in this order.
    public IReadOnlyList<int> ImageStorageObjectIds { get; init; } = [];
}
