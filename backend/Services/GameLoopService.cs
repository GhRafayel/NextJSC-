using System.Collections.Concurrent;
using Backend.Data;
using Backend.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Backend.Services;

public class BombState
{
    public required string      Id          { get; set; }
    public required DateTime    DetonatesAt { get; set; }
    public required int         OwnerId     { get; set; }
    public          int         Row         { get; set; }
    public          int         Col         { get; set; }
    public          int         BlastLength { get; set; }
}
public class HeroState
{
    public required int UserId { get; set; }
    public int Row { get; set; }
    public int Col { get; set; }
    public string? Direction { get; set; }
    public bool Alive { get; set; } = true;
    public int BombCount { get; set; } = 3;
    public int MaxBombs { get; set; } = 3;
    public int BlastLength { get; set; } = 3;
}

public class GameLoopService(IHubContext<GameHub> hub)
{
    private readonly ConcurrentDictionary<string, List<HeroState>> _matches = new();
    private readonly ConcurrentDictionary<string, int[][]> _maps = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _loops = new();
    
    private readonly ConcurrentDictionary<string, List<BombState>> _boms = new();

    public bool TryPlaceBomb(string roomId, int userId, out BombState? bomb)
    {
        bomb = null;
        if (!_matches.TryGetValue(roomId, out var heroes)) return false;
        
        HeroState? hero = heroes.FirstOrDefault(h => h.UserId == userId);
        if (hero is null || !hero.Alive || hero.BombCount <= 0) return false;

        bomb = new BombState
        {
            Id = Guid.NewGuid().ToString(),
            OwnerId = userId,
            Row = hero.Row,
            Col = hero.Col,
            BlastLength = hero.BlastLength,
            DetonatesAt = DateTime.UtcNow.AddSeconds(3)
        };
        hero.BombCount--;
        List<BombState> bombs = _boms.GetOrAdd(roomId, _ => new List<BombState>());
        bombs.Add(bomb);
        return true;
    }
    
    public List<HeroState> StartMatch(string roomId, int[][] map, IReadOnlyList<int> playerUserIds)
    {
        var heroes = new List<HeroState>();
        for (int i = 0; i < playerUserIds.Count; i++)
        {
            (int row, int col) = Maps.GetSpawnPosition(i, map);
            heroes.Add(new HeroState { UserId = playerUserIds[i], Row = row, Col = col });
        }

        _matches[roomId] = heroes;
        _maps[roomId] = map;
        return heroes;
    }
    
    public void StartTickLoop(string roomId)
    {
        if (_loops.ContainsKey(roomId)) return;

        var cts = new CancellationTokenSource();
        _loops[roomId] = cts;

        _ = Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(200, cts.Token);
                    Tick(roomId);

                    await hub.Clients.Group(roomId).SendAsync("match-state", new
                    {
                        roomId,
                        map = _maps[roomId],
                        heroes = _matches[roomId],
                        bombs = Array.Empty<object>(),
                        status = "playing",
                        winnerId = (int?)null,
                        tick = 0,
                    });
                }
            }
            catch (TaskCanceledException) { }
        });
    }

    public void StopTickLoop(string roomId)
    {
        if (_loops.TryRemove(roomId, out CancellationTokenSource? cts))
            cts.Cancel();
    }

    public List<HeroState>? GetHeroes(string roomId)
    {
        return _matches.TryGetValue(roomId, out var heroes) ? heroes : null;
    }

    private void Tick(string roomId)
    {
        if (!_matches.TryGetValue(roomId, out var heroes)) return;
        if (!_maps.TryGetValue(roomId, out var map)) return;

        foreach (var hero in heroes)
        {
            if (!hero.Alive || hero.Direction is null) continue;

            (int row, int col) = hero.Direction switch
            {
                "UP" => (hero.Row - 1, hero.Col),
                "DOWN" => (hero.Row + 1, hero.Col),
                "LEFT" => (hero.Row, hero.Col - 1),
                "RIGHT" => (hero.Row, hero.Col + 1),
                _ => (hero.Row, hero.Col),
            };

            if (IsBlocked(map, row, col)) continue;
            hero.Row = row;
            hero.Col = col;
        }
    }

    private static bool IsBlocked(int[][] map, int row, int col)
    {
        if (row < 0 || row >= map.Length || col < 0 || col >= map[0].Length) return true;
        return map[row][col] == 1 || map[row][col] == 2;
    }

}
