using System.Security.Claims;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Backend.Hubs;
[Authorize]
public class GameHub(
        GameStateService state, 
        OnlineStateService online,
        MatchmakingService matchmaking,
        RoomCountdownService countdown,
        IHubContext<GameHub> hubContext
) : Hub 
{
    private int? GetUserId()
    {
        string? res = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(res, out var id) ? id : null;
    }
    
    public async Task JoinRoom(string? roomId = null)
    {
        int? userId = GetUserId();
        if (userId is null) return;

        (string RoomId, long PlayerCount) result = roomId is null
                ? await matchmaking.JoinOrCreateRoom(userId.Value.ToString())
                : (roomId, await matchmaking.AddPlayerToRoom(roomId, userId.Value.ToString()));

        await Groups.AddToGroupAsync(Context.ConnectionId, result.RoomId);
        await matchmaking.SetPlayerRoom(Context.ConnectionId, result.RoomId);

        string roomStatus = "WAITING";

        if (result.PlayerCount == 2)
        {
            roomStatus = "STARTING";
            countdown.StartCountdown(result.RoomId, 10, async () =>
            {
                await hubContext.Clients.Group(result.RoomId).SendAsync("room-update", new
                {
                    roomId = result.RoomId,
                    roomStatus = "PLAYING",
                });
            });
        }
        else if (result.PlayerCount >= 4)
        {
            countdown.CancelCountdown(result.RoomId);
            roomStatus = "PLAYING";
        }
        else if (result.PlayerCount == 3)
        {
            roomStatus = "STARTING";
        }

        await Clients.Group(result.RoomId).SendAsync("room-update", new
        {
            players = result.PlayerCount,
            roomId = result.RoomId,
            roomStatus,
        });
    }


    public async Task LeaveRoom ()
    {
        string? roomId = await matchmaking.GetPlayerRoom(Context.ConnectionId);
        if (roomId is null) return;

        int ? userId = GetUserId();
        if (userId is not null)
            await matchmaking.RemovePlayerFromRoom(roomId, userId.Value.ToString());

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        await matchmaking.ClearPlayerRoom(Context.ConnectionId);

        string[] remaining = await matchmaking.GetRoomPlayers(roomId);

        if (remaining.Length == 1)
            countdown.CancelCountdown(roomId);

        await Clients.Group(roomId).SendAsync("room-update", new
        {
            players = remaining.Length,
            roomId,
            roomStatus = "WAITING",
        });
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