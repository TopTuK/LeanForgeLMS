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

            dbUser = new DbUser
            {
                Email = userRequestDto.Email,
                FirstName = userRequestDto.FirstName,
                LastName = userRequestDto.LastName ?? string.Empty,
                Role = userRequestDto.Role,
            };

            _dbContext.Users.Add(dbUser);
            await _dbContext.SaveChangesAsync();
        }
        else if (dbUser.Role != userRequestDto.Role)
        {
            _logger.LogInformation(
                "UserService::EnsureUserWithRoleAsync: updating Role for Email={Email} from {OldRole} to {NewRole}",
                userRequestDto.Email,
                dbUser.Role,
                userRequestDto.Role);

            dbUser.Role = userRequestDto.Role;
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

