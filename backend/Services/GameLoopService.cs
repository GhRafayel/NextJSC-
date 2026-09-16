using System.Collections.Concurrent;
using Backend.Data;

namespace Backend.Services;

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

public class GameLoopService
{
    private readonly ConcurrentDictionary<string, List<HeroState>> _matches = new();

    public List<HeroState> StartMatch(string roomId, int[][] map, IReadOnlyList<int> playerUserIds)
    {
        var heroes = new List<HeroState>();
        for (int i = 0; i < playerUserIds.Count; i++)
        {
            (int row, int col) = Maps.GetSpawnPosition(i, map);
            heroes.Add(new HeroState { UserId = playerUserIds[i], Row = row, Col = col });
        }

        _matches[roomId] = heroes;
        return heroes;
    }

    public List<HeroState>? GetHeroes(string roomId)
    {
        return _matches.TryGetValue(roomId, out var heroes) ? heroes : null;
    }
}
