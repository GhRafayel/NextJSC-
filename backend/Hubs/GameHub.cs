using System.Security.Claims;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Backend.Hubs;
[Authorize]
public class GameHub(
        GameStateService state,
        OnlineStateService online,
        MatchmakingService matchmaking,
        RoomCountdownService countdown,
        GameLoopService gameLoop,
        IServiceScopeFactory scopeFactory,
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

        // fast-track: react on the very next tick instead of wherever this hero's own move
        // cycle (TicksSinceMove/TicksPerMove) happened to be — that's what made pressing a
        // key feel laggy. Safe to do unconditionally (even for a "same as before" direction,
        // e.g. a quick release-and-repress) because the frontend now filters out OS
        // key-repeat itself (KeyboardControls.ts ignores e.repeat) — only a genuine fresh
        // keydown ever reaches here, so this can no longer be spammed to bypass the speed cap.
        hero.TicksSinceMove = hero.TicksPerMove - 1;
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

        await BroadcastRoomJoin(result.RoomId, result.PlayerCount, result.Map);
    }

    public record RematchRequest(string RoomId);

    public async Task Rematch(RematchRequest request)
    {
        int? userId = GetUserId();
        if (userId is null) return;

        // deterministic room id derived from the just-finished match's room — everyone who
        // presses "try again" for that same match converges on this same new room, without
        // needing to wait for or coordinate with anyone who doesn't rematch (they just fall
        // back to normal "play online" matchmaking separately, landing somewhere else)
        string newRoomId = $"rematch:{request.RoomId}";

        int[][]? existingMap = await matchmaking.TryGetRoomMap(newRoomId);
        int[][] map;
        if (existingMap is null)
        {
            int[][] baseMap = Maps.GetRandom();
            int[][] generated = Maps.GenerateMatchMap(baseMap);
            Dictionary<string, string> bonuses = Maps.PlaceBonuses(generated);
            // race-safe: if another rematching player beat us to creating this room, use
            // their stored map instead of the one we just generated ourselves
            map = await matchmaking.EnsureRoomCreated(newRoomId, generated, bonuses);
        }
        else
        {
            map = existingMap;
        }

        long count = await matchmaking.AddPlayerToRoom(newRoomId, userId.Value.ToString());
        await BroadcastRoomJoin(newRoomId, count, map);
    }

    private async Task BroadcastRoomJoin(string roomId, long playerCount, int[][] map)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await matchmaking.SetPlayerRoom(Context.ConnectionId, roomId);

        string roomStatus = "WAITING";

        if (playerCount >= 4)
        {
            countdown.CancelCountdown(roomId);
            roomStatus = "PLAYING";
            await StartMatchAndBroadcast(roomId, map, Clients.Group(roomId));
        }
        else if (playerCount >= 2)
        {
            roomStatus = "STARTING";
            countdown.StartCountdown(roomId, 10, async () =>
            {
                // once a room actually starts playing it must stop being the room new
                // "play online" clicks get funneled into — otherwise a room that never
                // reached 4 players (the only place this was previously cleared) stays
                // "active" forever, and the next casual joiner lands mid-match instead
                // of getting a fresh room
                await matchmaking.ClearActiveRoomIfMatches(roomId);

                await hubContext.Clients.Group(roomId).SendAsync("room-update", new
                {
                    roomId,
                    roomStatus = "PLAYING",
                });
                await StartMatchAndBroadcast(roomId, map, hubContext.Clients.Group(roomId));
            });
        }

        List<int> roomUserIds = (await matchmaking.GetRoomPlayers(roomId)).Select(int.Parse).ToList();
        Dictionary<int, string> roomColors = await GetColorsAsync(roomUserIds);

        await Clients.Group(roomId).SendAsync("room-update", new
        {
            players = playerCount,
            roomId,
            roomStatus,
            map,
            playerList = roomUserIds.Select(id => new { userId = id, color = roomColors[id] }),
        });
    }

    private async Task<Dictionary<int, string>> GetColorsAsync(IEnumerable<int> userIds)
    {
        var colors = new Dictionary<int, string>();
        using IServiceScope scope = scopeFactory.CreateScope();
        UserService scopedUsers = scope.ServiceProvider.GetRequiredService<UserService>();
        foreach (int id in userIds)
        {
            User? user = await scopedUsers.GetUserById(id);
            colors[id] = user?.Color ?? "#22c55e";
        }
        return colors;
    }

    private async Task StartMatchAndBroadcast(string roomId, int[][] map, IClientProxy clients)
    {
        string[] playerIds = await matchmaking.GetRoomPlayers(roomId);
        List<int> userIds = playerIds.Select(int.Parse).ToList();
        Dictionary<string, string> bonuses = await matchmaking.GetRoomBonuses(roomId);
        Dictionary<int, string> colors = await GetColorsAsync(userIds);

        gameLoop.StartMatch(roomId, map, userIds, bonuses, colors);

        await clients.SendAsync("match-state", gameLoop.BuildMatchStatePayload(roomId));
        gameLoop.StartTickLoop(roomId);
    }

    private static readonly string[] BotColors = ["#ef4444", "#f97316", "#a3e635"];

    public record PlayAiRequest(int Level);

    public async Task PlayAI(PlayAiRequest request)
    {
        int? userId = GetUserId();
        if (userId is null) return;

        int level = Math.Clamp(request.Level, 1, 4);
        int botCount = level <= 3 ? level : 3;

        string roomId = Guid.NewGuid().ToString();
        int[][] baseMap = Maps.GetRandom();
        int[][] map = Maps.GenerateMatchMap(baseMap);
        Dictionary<string, string> bonuses = Maps.PlaceBonuses(map);

        var playerUserIds = new List<int> { userId.Value };
        for (int i = 0; i < botCount; i++)
            playerUserIds.Add(-(i + 1));

        Dictionary<int, string> colors = await GetColorsAsync([userId.Value]);
        for (int i = 0; i < botCount; i++)
            colors[-(i + 1)] = BotColors[i % BotColors.Length];

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await matchmaking.SetPlayerRoom(Context.ConnectionId, roomId);

        gameLoop.StartMatch(roomId, map, playerUserIds, bonuses, colors, level);

        await Clients.Group(roomId).SendAsync("room-update", new
        {
            players = playerUserIds.Count,
            roomId,
            roomStatus = "PLAYING",
            map,
            playerList = playerUserIds.Select(id => new { userId = id, color = colors[id] }),
        });

        await Clients.Group(roomId).SendAsync("match-state", gameLoop.BuildMatchStatePayload(roomId));
        gameLoop.StartTickLoop(roomId);
    }

    public async Task LeaveRoom ()
    {
        string? roomId = await matchmaking.GetPlayerRoom(Context.ConnectionId);
        if (roomId is null) return;

        gameLoop.StopTickLoop(roomId);

        int ? userId = GetUserId();
        if (userId is not null)
            await matchmaking.RemovePlayerFromRoom(roomId, userId.Value.ToString());

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
        await matchmaking.ClearPlayerRoom(Context.ConnectionId);

        string[] remaining = await matchmaking.GetRoomPlayers(roomId);
        List<int> remainingIds = remaining.Select(int.Parse).ToList();

        if (remaining.Length == 1)
            countdown.CancelCountdown(roomId);

        // an abandoned room (everyone left) shouldn't keep being the target new "play
        // online" clicks get funneled into
        if (remaining.Length == 0)
            await matchmaking.ClearActiveRoomIfMatches(roomId);

        Dictionary<int, string> colors = await GetColorsAsync(remainingIds);

        await Clients.Group(roomId).SendAsync("room-update", new
        {
            players = remaining.Length,
            roomId,
            roomStatus = "WAITING",
            playerList = remainingIds.Select(id => new { userId = id, color = colors[id] }),
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