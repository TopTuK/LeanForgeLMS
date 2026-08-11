using LF.AppDomain.Entities.User;
using LF.AppDomain.Models.User.Enums;
using LF.Application.Common.Interfaces;
using LF.Application.ModelDto.User;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LF.Application.Services.User;

internal sealed class UserService(ILogger<UserService> logger, IAppDbContext dbContext) : IUserService
{
    private readonly ILogger<UserService> _logger = logger;
    private readonly IAppDbContext _dbContext = dbContext;

    public async Task<UserDto> GetOrCreateUserAsync(GetOrCreateUserDto userRequestDto)
    {
        _logger.LogInformation("UserService::GetOrCreateUserAsync: called with Email={Email}", userRequestDto.Email);

        var dbUser = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == userRequestDto.Email);
        if (dbUser is null)
        {
            _logger.LogInformation("UserService::GetOrCreateUserAsync: user with email: {usrEmail} not found, creating new user", userRequestDto.Email);

            dbUser = userRequestDto.Adapt<DbUser>();
            dbUser.Role = UserRole.Student;

            _dbContext.Users.Add(dbUser);
            await _dbContext.SaveChangesAsync();
        }

        _logger.LogInformation("UserService::GetOrCreateUserAsync: User retrieved or created. UserId={usrId}, " +
            "Email={usrEmail}", dbUser.Id, dbUser.Email);

        return dbUser.Adapt<UserDto>();
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        _logger.LogInformation("UserService::GetUserByIdAsync: called with Id={usrId}", id);

        var dbUser = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return dbUser?.Adapt<UserDto>();
    }

    public async Task<UserDto?> UpdateUserNameAsync(int id, UpdateUserNameDto dto)
    {
        _logger.LogInformation("UserService::UpdateUserNameAsync: called with Id={usrId}", id);

        var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (dbUser is null)
        {
            _logger.LogInformation("UserService::UpdateUserNameAsync: user with Id={usrId} not found", id);
            return null;
        }

        if (dbUser.UpdateName(dto.FirstName, dto.LastName))
        {
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            _logger.LogInformation("UserService::UpdateUserNameAsync: no changes for Id={usrId}, skipping save", id);
        }

        return dbUser.Adapt<UserDto>();
    }

    public async Task<UserDto?> UpdateUserAvatarAsync(int id, UpdateUserAvatarDto dto)
    {
        _logger.LogInformation("UserService::UpdateUserAvatarAsync: called with Id={usrId}", id);

        var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (dbUser is null)
        {
            _logger.LogInformation("UserService::UpdateUserAvatarAsync: user with Id={usrId} not found", id);
            return null;
        }

        if (dto.AvatarKey is null)
            dbUser.ClearAvatar();
        else
            dbUser.SetAvatar(dto.AvatarKey);

        await _dbContext.SaveChangesAsync();

        return dbUser.Adapt<UserDto>();
    }

    public async Task<PagedUsersDto> ListUsersAsync(int page, int pageSize, string? search)
    {
        _logger.LogInformation("UserService::ListUsersAsync: called with Page={Page} PageSize={PageSize} Search={Search}", page, pageSize, search);

        var query = _dbContext.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(normalizedSearch) ||
                u.LastName.ToLower().Contains(normalizedSearch) ||
                u.Email.ToLower().Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedUsersDto { Items = users.Adapt<List<UserDto>>(), TotalCount = totalCount };
    }

    public async Task<UserDto?> UpdateUserRoleAsync(int id, UpdateUserRoleDto dto)
    {
        _logger.LogInformation("UserService::UpdateUserRoleAsync: called with Id={usrId} Role={Role}", id, dto.Role);

        var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (dbUser is null)
        {
            _logger.LogInformation("UserService::UpdateUserRoleAsync: user with Id={usrId} not found", id);
            return null;
        }

        if (dbUser.EnsureRole(dto.Role))
        {
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            _logger.LogInformation("UserService::UpdateUserRoleAsync: no changes for Id={usrId}, skipping save", id);
        }

        return dbUser.Adapt<UserDto>();
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        _logger.LogInformation("UserService::DeleteUserAsync: called with Id={usrId}", id);

        var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (dbUser is null)
        {
            _logger.LogInformation("UserService::DeleteUserAsync: user with Id={usrId} not found", id);
            return false;
        }

        _dbContext.Users.Remove(dbUser);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<UserDto> EnsureUserWithRoleAsync(EnsureUserWithRoleDto userRequestDto)
    {
        _logger.LogInformation(
            "UserService::EnsureUserWithRoleAsync: called with Email={Email} Role={Role}",
            userRequestDto.Email,
            userRequestDto.Role);

        var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userRequestDto.Email);
        if (dbUser is null)
        {
            _logger.LogInformation(
                "UserService::EnsureUserWithRoleAsync: user with email {Email} not found, creating with Role={Role}",
                userRequestDto.Email,
                userRequestDto.Role);

            dbUser = userRequestDto.Adapt<DbUser>();

            _dbContext.Users.Add(dbUser);
            await _dbContext.SaveChangesAsync();
        }
        else if (dbUser.EnsureRole(userRequestDto.Role))
        {
            _logger.LogInformation(
                "UserService::EnsureUserWithRoleAsync: updated Role for Email={Email} to {Role}",
                userRequestDto.Email,
                userRequestDto.Role);

            await _dbContext.SaveChangesAsync();
        }

        _logger.LogInformation(
            "UserService::EnsureUserWithRoleAsync: User ensured. UserId={UserId}, Email={Email}, Role={Role}",
            dbUser.Id,
            dbUser.Email,
            dbUser.Role);

        return dbUser.Adapt<UserDto>();
    }
}
