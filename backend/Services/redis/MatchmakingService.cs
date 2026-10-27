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

    public async Task<int[][]?> TryGetRoomMap(string roomId)
    {
        RedisValue value = await _db.StringGetAsync($"room:{roomId}:map");
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<int[][]>(value.ToString());
    }

    // creates the room's map/bonuses only if nothing is stored yet (atomic via NotExists) —
    // returns whichever map actually ended up stored, since if another rematching player's
    // call won the race, the caller must use *their* map instead of the one just generated
    // and discarded here
    public async Task<int[][]> EnsureRoomCreated(string roomId, int[][] map, Dictionary<string, string> bonuses)
    {
        bool created = await _db.StringSetAsync($"room:{roomId}:map", JsonSerializer.Serialize(map), when: When.NotExists);
        if (created)
        {
            await _db.StringSetAsync($"room:{roomId}:bonuses", JsonSerializer.Serialize(bonuses));
            return map;
        }
        return (await TryGetRoomMap(roomId))!;
    }

    public async Task<Dictionary<string, string>> GetRoomBonuses(string roomId)
    {
        RedisValue value = await _db.StringGetAsync($"room:{roomId}:bonuses");
        if (value.IsNullOrEmpty) return new Dictionary<string, string>();
        return JsonSerializer.Deserialize<Dictionary<string, string>>(value.ToString())!;
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

    // only clears the "currently open for new joiners" pointer if it still points at this
    // exact room — guards against clobbering a different room that became active in the
    // meantime. Needed anywhere a room stops being a valid target for casual matchmaking
    // (it started playing, or everyone left it) but never hit the 4-player auto-clear.
    public async Task ClearActiveRoomIfMatches(string roomId)
    {
        RedisValue current = await _db.StringGetAsync(ActiveRoomKey);
        if (current == roomId)
            await _db.KeyDeleteAsync(ActiveRoomKey);
    }

}
