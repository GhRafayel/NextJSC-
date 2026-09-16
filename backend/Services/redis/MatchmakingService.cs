using Backend.Data;
using StackExchange.Redis;
using System.Text.Json;

namespace Backend.Services;

public class MatchmakingService (IConnectionMultiplexer redis)
{
    private readonly IDatabase _db = redis.GetDatabase();
    private const string ActiveRoomKey = "matchmaking:active-room";

    public async Task<(string RoomId, long PlayerCount, int[][] Map)> JoinOrCreateRoom(string userId)
    {
        RedisValue existing = await _db.StringGetAsync(ActiveRoomKey);
        string roomId;
        int[][] map;

        if (existing.IsNullOrEmpty)
        {
            roomId = Guid.NewGuid().ToString();
            int[][] baseMap = Maps.GetRandom();
            map = Maps.GenerateMatchMap(baseMap);
            Dictionary<string, string> bonuses = Maps.PlaceBonuses(map);

            await _db.StringSetAsync(ActiveRoomKey, roomId);
            await _db.StringSetAsync($"room:{roomId}:map", JsonSerializer.Serialize(map));
            await _db.StringSetAsync($"room:{roomId}:bonuses", JsonSerializer.Serialize(bonuses));
        }
        else
        {
            roomId = existing.ToString();
            map = await GetRoomMap(roomId);
        }

        long count = await AddPlayerToRoom(roomId, userId);

        if (count >= 4)
            await _db.KeyDeleteAsync(ActiveRoomKey);

        return (roomId, count, map);
    }

    public async Task<int[][]> GetRoomMap(string roomId)
    {
        RedisValue value = await _db.StringGetAsync($"room:{roomId}:map");
        return JsonSerializer.Deserialize<int[][]>(value.ToString())!;
    }
    
    public Task<long> AddPlayerToRoom(string roomId, string userId)
    {
        return _db.ListRightPushAsync($"room:{roomId}:players", userId);
    }
    
    public async Task<string[]> GetRoomPlayers(string roomId)
    {
        RedisValue[] values = await _db.ListRangeAsync($"room:{roomId}:players");
        return [.. values.Select(v => v.ToString())];
    }

    public Task RemovePlayerFromRoom(string roomId, string userId)
    {
        return _db.ListRemoveAsync($"room:{roomId}:players", userId);
    }

    public Task SetPlayerRoom(string connectionId, string roomId)
    {
        return _db.StringSetAsync($"connectionId:{connectionId}:room", roomId);
    }

    public async Task<string?> GetPlayerRoom(string connectionId)
    {
        RedisValue values = await _db.StringGetAsync($"connectionId:{connectionId}:room");
        return values.IsNullOrEmpty ? null : values.ToString();
    }

    public Task ClearPlayerRoom(string connectionId)
    {
        return _db.KeyDeleteAsync($"connectionId:{connectionId}:room");
    }

}
