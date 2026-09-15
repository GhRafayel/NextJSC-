using System.Security.Claims;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Backend.Hubs;
[Authorize]
public class GameHub(GameStateService state, OnlineStateService online) : Hub 
{
    private int? GetUserId()
    {
        string? res = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(res, out var id) ? id : null;
    }
    public async Task JoinGame (string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        await state.AddPlayer(gameId, Context.ConnectionId);
        await Clients.Group(gameId).SendAsync("PlayerJoined", Context.ConnectionId);
    }

    public async Task LeaveGame (string gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
        await state.RemovePlayer(gameId, Context.ConnectionId);
        await Clients.Group(gameId).SendAsync("PlayerLeave",  Context.ConnectionId);
    }

    public override async Task OnConnectedAsync()
    {
        int? clientId = GetUserId();
        if (clientId is not null)
            await online.AddOnlineUser(clientId.Value, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync (Exception? exception)
    {
        int? clientId = GetUserId();
        if (clientId is not null)
            await online.RemOnlineUser(clientId.Value, Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}