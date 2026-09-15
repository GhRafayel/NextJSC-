using StackExchange.Redis;

namespace Backend.Services;

public class GameStateService (IConnectionMultiplexer redis)
{
    private readonly IDatabase _db = redis.GetDatabase();

    public Task AddPlayer(string gameId, string connectionsId)
    {
        return _db.SetAddAsync(gameId, connectionsId);
    }

    public Task RemovePlayer ( string gameId, string connectionsId)
    {
        return _db.SetRemoveAsync(gameId, connectionsId);
    }

    public async Task <string[]> GetPlayers(string gameId)
    {
        RedisValue[] values = await _db.SetMembersAsync(gameId);
        return [.. values.Select(v => v.ToString())];
    }

    
}
