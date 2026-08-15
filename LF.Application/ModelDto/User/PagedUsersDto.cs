namespace LF.Application.ModelDto.User;

public sealed class PagedUsersDto
{
    public IReadOnlyList<UserDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
}
