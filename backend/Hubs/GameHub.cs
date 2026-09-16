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
        GameLoopService gameLoop,
        IHubContext<GameHub> hubContext
) : Hub
{
    public record ChangeDirectionRequest(string Direction, string RoomId);

    public void ChangeDirection(ChangeDirectionRequest request)
    {
        int? userId = GetUserId();
        if (userId is null) return ;
        List<HeroState>? heroes = gameLoop.GetHeroes(request.RoomId);
        HeroState? hero = heroes?.FirstOrDefault(h => h.UserId == userId.Value);
        if (hero is null) return;
        hero.Direction = request.Direction;
    }
    
    private int? GetUserId()
    {
        string? res = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(res, out var id) ? id : null;
    }

    public void PlaceBomb(string roomId)
    {
        int? userId = GetUserId();
        if (userId is null) return;
        gameLoop.TryPlaceBomb(roomId, userId.Value, out _);
    }

    public async Task JoinRoom(string? roomId = null)
    {
        int? userId = GetUserId();
        if (userId is null) return;

        (string RoomId, long PlayerCount, int[][] Map) result = roomId is null
                ? await matchmaking.JoinOrCreateRoom(userId.Value.ToString())
                : (roomId, await matchmaking.AddPlayerToRoom(roomId, userId.Value.ToString()), await matchmaking.GetRoomMap(roomId));

        await Groups.AddToGroupAsync(Context.ConnectionId, result.RoomId);
        await matchmaking.SetPlayerRoom(Context.ConnectionId, result.RoomId);

        string roomStatus = "WAITING";

        if (result.PlayerCount >= 4)
        {
            countdown.CancelCountdown(result.RoomId);
            roomStatus = "PLAYING";
            await StartMatchAndBroadcast(result.RoomId, result.Map, Clients.Group(result.RoomId));
        }
        else if (result.PlayerCount >= 2)
        {
            roomStatus = "STARTING";
            countdown.StartCountdown(result.RoomId, 10, async () =>
            {
                await hubContext.Clients.Group(result.RoomId).SendAsync("room-update", new
                {
                    roomId = result.RoomId,
                    roomStatus = "PLAYING",
                });
                await StartMatchAndBroadcast(result.RoomId, result.Map, hubContext.Clients.Group(result.RoomId));
            });
        }
        await Clients.Group(result.RoomId).SendAsync("room-update", new
        {
            players = result.PlayerCount,
            roomId = result.RoomId,
            roomStatus,
            map = result.Map,
        });
    }

    private async Task StartMatchAndBroadcast(string roomId, int[][] map, IClientProxy clients)
    {
        string[] playerIds = await matchmaking.GetRoomPlayers(roomId);
        List<int> userIds = playerIds.Select(int.Parse).ToList();
        List<HeroState> heroes = gameLoop.StartMatch(roomId, map, userIds);

        await clients.SendAsync("match-state", new
        {
            roomId,
            map,
            heroes,
            bombs = Array.Empty<object>(),
            status = "playing",
            winnerId = (int?)null,
            tick = 0,
        });
        gameLoop.StartTickLoop(roomId);
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