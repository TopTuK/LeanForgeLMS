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

        var userDto = await _userService.UpdateUserNameAsync(request.Id, request.Adapt<UpdateUserNameDto>());
        if (userDto is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"User {request.Id} not found"));

        return userDto.Adapt<GetUserReply>();
    }
}
