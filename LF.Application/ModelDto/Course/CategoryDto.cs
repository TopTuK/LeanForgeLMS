namespace LF.Application.ModelDto.Course;

public sealed class CategoryDto
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public bool IsDefault { get; init; }
}
