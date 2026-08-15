using Grpc.Core;
using LF.Application.ModelDto.Authentication;
using LF.Application.ModelDto.User;
using LF.Application.Services.User;
using LF.IdentityService;
using Mapster;
using Microsoft.Extensions.Logging;

namespace LF.Infrastructure.Services.Identity;

internal sealed class GrpcIdentityService(ILogger<GrpcIdentityService> logger,
    UserServiceRpc.UserServiceRpcClient userServiceRpcClient) : IGrpcIdentityService
{
    private readonly ILogger<GrpcIdentityService> _logger = logger;
    private readonly UserServiceRpc.UserServiceRpcClient _userServiceRpcClient = userServiceRpcClient;

    public async Task<UserDto> GetOrCreateUserAsync(UserAuthentificationDto userAuthentification)
    {
        _logger.LogInformation("GrpcIdentityService::GetOrCreateUser: called with userAuthentification: {@userAuthentification}", userAuthentification);

        if (userAuthentification.Email is null)
        {
            throw new ArgumentNullException(nameof(userAuthentification.Email));
        }

        var getUserRequest = userAuthentification.Adapt<GetUserRequest>();
        _logger.LogInformation("GrpcIdentityService::GetOrCreateUser: sending GetUserRequest: {@getUserRequest}", getUserRequest);

        var getUserReply = await _userServiceRpcClient.GetOrCreateUserAsync(getUserRequest);
        _logger.LogInformation("GrpcIdentityService::GetOrCreateUser: received GetUserResponse: {@getUserReply}", getUserReply);

        return getUserReply.Adapt<UserDto>();
    }

    public async Task<UserDto?> GetUserProfileAsync(int userId)
    {
        _logger.LogInformation("GrpcIdentityService::GetUserProfileAsync: called with UserId={usrId}", userId);

        try
        {
            var reply = await _userServiceRpcClient.GetUserProfileAsync(new GetUserProfileRequest { Id = userId });
            return reply.Adapt<UserDto>();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<UserDto?> UpdateUserProfileAsync(int userId, UpdateUserProfileDto dto)
    {
        _logger.LogInformation("GrpcIdentityService::UpdateUserProfileAsync: called with UserId={usrId}", userId);

        var request = new UpdateUserProfileRequest
        {
            Id = userId,
            FirstName = dto.FirstName,
            LastName = dto.LastName ?? string.Empty,
            Description = dto.Description ?? string.Empty,
        };

        try
        {
            var reply = await _userServiceRpcClient.UpdateUserProfileAsync(request);
            return reply.Adapt<UserDto>();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<UserDto?> UpdateUserAvatarAsync(int userId, string? avatarKey)
    {
        _logger.LogInformation("GrpcIdentityService::UpdateUserAvatarAsync: called with UserId={usrId}", userId);

        var request = new UpdateUserAvatarRequest
        {
            Id = userId,
            AvatarKey = avatarKey,
        };

        try
        {
            var reply = await _userServiceRpcClient.UpdateUserAvatarAsync(request);
            return reply.Adapt<UserDto>();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<UserDto> EnsureUserWithRoleAsync(EnsureUserWithRoleDto userRequestDto)
    {
        _logger.LogInformation("GrpcIdentityService::EnsureUserWithRoleAsync: called with Email={Email} Role={Role}",
            userRequestDto.Email, userRequestDto.Role);

        var request = userRequestDto.Adapt<EnsureUserWithRoleRequest>();
        var reply = await _userServiceRpcClient.EnsureUserWithRoleAsync(request);

        return reply.Adapt<UserDto>();
    }

    public async Task<PagedUsersDto> ListUsersAsync(int page, int pageSize, string? search)
    {
        _logger.LogInformation("GrpcIdentityService::ListUsersAsync: called with Page={Page} PageSize={PageSize} Search={Search}", page, pageSize, search);

        var request = new ListUsersRequest { Page = page, PageSize = pageSize, Search = search };
        var reply = await _userServiceRpcClient.ListUsersAsync(request);

        return new PagedUsersDto { Items = reply.Users.Adapt<List<UserDto>>(), TotalCount = reply.TotalCount };
    }

    public async Task<UserDto?> UpdateUserRoleAsync(int userId, UpdateUserRoleDto dto)
    {
        _logger.LogInformation("GrpcIdentityService::UpdateUserRoleAsync: called with UserId={usrId} Role={Role}", userId, dto.Role);

        var request = dto.Adapt<UpdateUserRoleRequest>();
        request.Id = userId;

        try
        {
            var reply = await _userServiceRpcClient.UpdateUserRoleAsync(request);
            return reply.Adapt<UserDto>();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        _logger.LogInformation("GrpcIdentityService::DeleteUserAsync: called with UserId={usrId}", userId);

        try
        {
            var reply = await _userServiceRpcClient.DeleteUserAsync(new DeleteUserRequest { Id = userId });
            return reply.Found;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return false;
        }
    }
}
