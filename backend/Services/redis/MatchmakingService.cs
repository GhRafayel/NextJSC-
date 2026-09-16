using StackExchange.Redis;

namespace Backend.Services;

public class MatchmakingService (IConnectionMultiplexer redis)
{
    private readonly IDatabase _db = redis.GetDatabase();
    private const string ActiveRoomKey = "matchmaking:active-room";

    public async Task<(string RoomId, long PlayerCount)> JoinOrCreateRoom(string userId)
    {
        RedisValue existing = await _db.StringGetAsync(ActiveRoomKey);
        string roomId = existing.IsNullOrEmpty ? Guid.NewGuid().ToString() : existing.ToString();

        if (existing.IsNullOrEmpty)
            await _db.StringSetAsync(ActiveRoomKey, roomId);
        long count = await AddPlayerToRoom(roomId, userId);

        if (count >= 4)
            await _db.KeyDeleteAsync(ActiveRoomKey);
        return (roomId, count);
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
