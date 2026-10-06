using LF.WebApi.Hubs;

namespace LF.WebApi.Endpoints;

// Maps the real-time hub through the same discovery mechanism as the REST groups, so Program.cs
// only registers the SignalR services.
public sealed class GroupChatHubEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        // A WebSocket outlives the request that authenticated it, so without this a connection keeps
        // its identity (and the admin flag cached at JoinGroup) after the session token expires. The
        // client's automatic reconnect then re-authenticates, and fails once the session is gone.
        app.MapHub<GroupChatHub>(GroupChatHub.Path, options => options.CloseOnAuthenticationExpiration = true)
            .RequireAuthorization();
    }
}
