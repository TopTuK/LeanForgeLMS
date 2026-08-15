using Grpc.Core;
using LF.Application.ModelDto.User;
using LF.Application.Services.User;
using LF.IdentityService;
using Mapster;

namespace Lf.IdentityService.Services;

public class RpcUserService(ILogger<RpcUserService> logger, IUserService userService) : UserServiceRpc.UserServiceRpcBase
{
    private readonly ILogger<RpcUserService> _logger = logger;
    private readonly IUserService _userService = userService;

    public override async Task<GetUserReply> GetOrCreateUser(GetUserRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcUserService::GetOrCreateUser: called with Email={usrEmail} " +
            "FirstName={usrFirstName} LastName={usrLastName}", request.Email, request.FirstName, request.LastName);

        var userDto = await _userService.GetOrCreateUserAsync(request.Adapt<GetOrCreateUserDto>());

        _logger.LogInformation("RpcUserService::GetOrCreateUser: user created with Id={usrId} Email={usrEmail} " +
            "FirstName={usrFirstName} LastName={usrLastName} Role={usrRole} CreatedAt={usrCreatedAt}",
            userDto.Id, userDto.Email, userDto.FirstName, userDto.LastName, userDto.Role, userDto.CreatedAt);

        return userDto.Adapt<GetUserReply>();
    }

    public override async Task<GetUserReply> GetUserProfile(GetUserProfileRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcUserService::GetUserProfile: called with Id={usrId}", request.Id);

        var userDto = await _userService.GetUserByIdAsync(request.Id);
        if (userDto is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"User {request.Id} not found"));

        return userDto.Adapt<GetUserReply>();
    }

    public override async Task<GetUserReply> UpdateUserProfile(UpdateUserProfileRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcUserService::UpdateUserProfile: called with Id={usrId} FirstName={usrFirstName} LastName={usrLastName}",
            request.Id, request.FirstName, request.LastName);

        var userDto = await _userService.UpdateUserNameAsync(request.Id, request.Adapt<UpdateUserProfileDto>());
        if (userDto is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"User {request.Id} not found"));

        return userDto.Adapt<GetUserReply>();
    }

    public override async Task<GetUserReply> UpdateUserAvatar(UpdateUserAvatarRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcUserService::UpdateUserAvatar: called with Id={usrId}", request.Id);

        var userDto = await _userService.UpdateUserAvatarAsync(request.Id, request.Adapt<UpdateUserAvatarDto>());
        if (userDto is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"User {request.Id} not found"));

        return userDto.Adapt<GetUserReply>();
    }

    public override async Task<GetUserReply> EnsureUserWithRole(EnsureUserWithRoleRequest request, ServerCallContext context)
    {
        _logger.LogInformation(
            "RpcUserService::EnsureUserWithRole: called with Email={usrEmail} FirstName={usrFirstName} Role={usrRole}",
            request.Email, request.FirstName, request.Role);

        var userDto = await _userService.EnsureUserWithRoleAsync(request.Adapt<EnsureUserWithRoleDto>());

        _logger.LogInformation(
            "RpcUserService::EnsureUserWithRole: user ensured Id={usrId} Email={usrEmail} Role={usrRole}",
            userDto.Id, userDto.Email, userDto.Role);

        return userDto.Adapt<GetUserReply>();
    }

    public override async Task<ListUsersReply> ListUsers(ListUsersRequest request, ServerCallContext context)
    {
        _logger.LogInformation(
            "RpcUserService::ListUsers: called with Page={Page} PageSize={PageSize} Search={Search}",
            request.Page, request.PageSize, request.Search);

        var paged = await _userService.ListUsersAsync(request.Page, request.PageSize, request.Search);

        var reply = new ListUsersReply { TotalCount = paged.TotalCount };
        reply.Users.AddRange(paged.Items.Adapt<List<GetUserReply>>());

        return reply;
    }

    public override async Task<GetUserReply> UpdateUserRole(UpdateUserRoleRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcUserService::UpdateUserRole: called with Id={usrId} Role={usrRole}", request.Id, request.Role);

        var userDto = await _userService.UpdateUserRoleAsync(request.Id, request.Adapt<UpdateUserRoleDto>());
        if (userDto is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"User {request.Id} not found"));

        return userDto.Adapt<GetUserReply>();
    }

    public override async Task<DeleteUserReply> DeleteUser(DeleteUserRequest request, ServerCallContext context)
    {
        _logger.LogInformation("RpcUserService::DeleteUser: called with Id={usrId}", request.Id);

        var found = await _userService.DeleteUserAsync(request.Id);
        if (!found)
            throw new RpcException(new Status(StatusCode.NotFound, $"User {request.Id} not found"));

        return new DeleteUserReply { Found = found };
    }
}
