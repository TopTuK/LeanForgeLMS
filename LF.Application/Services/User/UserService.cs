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

        dbUser.UpdateName(dto.FirstName, dto.LastName);
        await _dbContext.SaveChangesAsync();

        return dbUser.Adapt<UserDto>();
    }
}
