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
}
