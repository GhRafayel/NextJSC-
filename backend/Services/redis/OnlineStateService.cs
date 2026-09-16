
using StackExchange.Redis;

namespace Backend.Services;

public class OnlineStateService (IConnectionMultiplexer redis) 
{
    private readonly IDatabase _db = redis.GetDatabase();

    public Task AddOnlineUser (int userId, string connectionId)
    {
        return _db.SetAddAsync($"online:{userId}", connectionId);
    }

    public Task RemOnlineUser (int userId, string connectionId)
    {
        return _db.SetRemoveAsync($"online:{userId}", connectionId);
    }

    public Task <bool> IsUserOnline(int userId)
    {
        return _db.KeyExistsAsync($"online:{userId}");
    }
}