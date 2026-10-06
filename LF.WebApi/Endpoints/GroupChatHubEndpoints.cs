using LF.WebApi.Hubs;

namespace LF.WebApi.Endpoints;

// Maps the real-time hub through the same discovery mechanism as the REST groups, so Program.cs
// only registers the SignalR services.
public sealed class GroupChatHubEndpoints : IEndpointGroup
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapHub<GroupChatHub>(GroupChatHub.Path).RequireAuthorization();
    }
}
