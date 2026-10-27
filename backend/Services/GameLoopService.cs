using System.Collections.Concurrent;
using Backend.Data;
using Backend.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

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
    public int TicksPerMove { get; set; } = 2;
    public int TicksSinceMove { get; set; } = 0;
    public string Color { get; set; } = "#22c55e";
    public int BotSkill { get; set; } = 0;
}

public class BonusState
{
    public int Row { get; set; }
    public int Col { get; set; }
    public required string Kind { get; set; }
}

public record TickResult(List<(int Row, int Col)> Blasts, bool Finished, int? WinnerId);

public class GameLoopService(IHubContext<GameHub> hub, ILogger<GameLoopService> logger, IServiceScopeFactory scopeFactory)
{
    private readonly ConcurrentDictionary<string, List<HeroState>> _matches = new();
    private readonly ConcurrentDictionary<string, int[][]> _maps = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _loops = new();
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _hiddenBonuses = new();
    private readonly ConcurrentDictionary<string, List<BonusState>> _revealedBonuses = new();

    
    private static List<(int Row, int Col)> GetBlastCells(int[][] map, int bombRow, int bombCol, int blastLength)
    {
        var result = new List<(int Row, int Col)> { (bombRow, bombCol) };

        (int dRow, int dCol)[] directions = [(-1, 0), (1, 0), (0, -1), (0, 1)];

        foreach (var (dRow, dCol) in directions)
        {
            for (int step = 1; step <= blastLength; step++)
            {
                int row = bombRow + dRow * step;
                int col = bombCol + dCol * step;

                if (row < 0 || row >= map.Length || col < 0 || col >= map[row].Length || map[row][col] == 1)
                    break;

                if (map[row][col] == 2)
                {
                    result.Add((row, col));
                    break;
                }
                result.Add((row, col));
            }
        }
        return result;
    }

    
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
    
    public List<HeroState> StartMatch(string roomId, int[][] map, IReadOnlyList<int> playerUserIds, Dictionary<string, string> bonuses, Dictionary<int, string> colors, int botLevel = 1)
    {
        var heroes = new List<HeroState>();
        for (int i = 0; i < playerUserIds.Count; i++)
        {
            (int row, int col) = Maps.GetSpawnPosition(i, map);
            int userId = playerUserIds[i];
            heroes.Add(new HeroState
            {
                UserId = userId,
                Row = row,
                Col = col,
                Color = colors.GetValueOrDefault(userId, "#22c55e"),
                BotSkill = userId < 0 ? botLevel : 0,
            });
        }

        _matches[roomId] = heroes;
        _maps[roomId] = map;
        _hiddenBonuses[roomId] = bonuses;
        _revealedBonuses[roomId] = new List<BonusState>();
        _boms[roomId] = new List<BombState>();
        return heroes;
    }

    public List<BonusState> GetRevealedBonuses(string roomId)
    {
        return _revealedBonuses.TryGetValue(roomId, out var bonuses) ? bonuses : new List<BonusState>();
    }

    public object BuildMatchStatePayload(string roomId, string status = "playing", int? winnerId = null, IEnumerable<(int Row, int Col)>? blasts = null)
    {
        int[][] map = _maps.TryGetValue(roomId, out var m) ? m : [];

        return new
        {
            roomId,
            map,
            heroes = (GetHeroes(roomId) ?? new List<HeroState>()).Select(h => new
            {
                userId = h.UserId,
                position = new { row = h.Row, col = h.Col },
                direction = h.Direction,
                alive = h.Alive,
                bombCount = h.BombCount,
                maxBombs = h.MaxBombs,
                blastLength = h.BlastLength,
                color = h.Color,
            }),
            bombs = GetBombs(roomId).Select(b => new
            {
                id = b.Id,
                position = new { row = b.Row, col = b.Col },
                ownerId = b.OwnerId,
                blastLength = b.BlastLength,
                detonatesAt = new DateTimeOffset(b.DetonatesAt).ToUnixTimeMilliseconds(),
            }),
            bonuses = GetRevealedBonuses(roomId).Select(bn => new
            {
                id = $"{bn.Row}-{bn.Col}",
                position = new { row = bn.Row, col = bn.Col },
                kind = bn.Kind,
            }),
            status,
            winnerId,
            tick = 0,
            blasts = (blasts ?? Enumerable.Empty<(int Row, int Col)>()).Select(c => new { row = c.Row, col = c.Col }),
        };
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
                    await Task.Delay(100, cts.Token);
                    TickResult result = await Tick(roomId);

                    string status = result.Finished ? "finished" : "playing";
                    await hub.Clients.Group(roomId).SendAsync("match-state", BuildMatchStatePayload(roomId, status, result.WinnerId, result.Blasts));

                    if (result.Finished)
                    {
                        StopTickLoop(roomId);
                        break;
                    }
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "GameLoopService tick loop failed for room {RoomId}", roomId);
            }
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

    public List<BombState> GetBombs(string roomId)
    {
        return _boms.TryGetValue(roomId, out var bombs) ? bombs : new List<BombState>();
    }

    private async Task<TickResult> Tick(string roomId)
    {
        var blastCells = new List<(int Row, int Col)>();
        var noResult = new TickResult(blastCells, false, null);

        if (!_matches.TryGetValue(roomId, out var heroes)) return noResult;
        if (!_maps.TryGetValue(roomId, out var map)) return noResult;

        _boms.TryGetValue(roomId, out List<BombState>? bombsForAi);
        RunBotAI(roomId, map, heroes, bombsForAi ?? []);

        _boms.TryGetValue(roomId, out List<BombState>? bombs);

        if (bombs is not null)
        {
            var toExplode = new Queue<BombState>(bombs.Where(b => b.DetonatesAt <= DateTime.UtcNow));
            var alreadyExploded = new HashSet<string>();

            while (toExplode.Count > 0)
            {
                BombState bomb = toExplode.Dequeue();
                if (!alreadyExploded.Add(bomb.Id)) continue;

                List<(int Row, int Col)> cells = GetBlastCells(map, bomb.Row, bomb.Col, bomb.BlastLength);
                blastCells.AddRange(cells);

                foreach ((int row, int col) in cells)
                {
                    if (map[row][col] != 2) continue;
                    map[row][col] = 0;
                    RevealBonus(roomId, row, col);
                }

                foreach (HeroState hero in heroes)
                {
                    if (hero.Alive && cells.Contains((hero.Row, hero.Col)))
                        hero.Alive = false;
                }

                HeroState? owner = heroes.FirstOrDefault(h => h.UserId == bomb.OwnerId);
                if (owner is not null)
                    owner.BombCount = Math.Min(owner.MaxBombs, owner.BombCount + 1);

                // chain reaction: any other still-armed bomb caught in this blast detonates immediately too
                foreach (BombState other in bombs)
                {
                    if (!alreadyExploded.Contains(other.Id) && cells.Contains((other.Row, other.Col)))
                        toExplode.Enqueue(other);
                }
            }

            bombs.RemoveAll(b => alreadyExploded.Contains(b.Id));
        }

        var bombCells = (bombs ?? []).Select(b => (b.Row, b.Col)).ToHashSet();

        foreach (var hero in heroes)
        {
            if (!hero.Alive || hero.Direction is null) continue;

            hero.TicksSinceMove++;
            if (hero.TicksSinceMove < hero.TicksPerMove) continue;
            hero.TicksSinceMove = 0;

            (int row, int col) = hero.Direction switch
            {
                "UP" => (hero.Row - 1, hero.Col),
                "DOWN" => (hero.Row + 1, hero.Col),
                "LEFT" => (hero.Row, hero.Col - 1),
                "RIGHT" => (hero.Row, hero.Col + 1),
                _ => (hero.Row, hero.Col),
            };

            if (IsBlocked(map, row, col, bombCells)) continue;
            hero.Row = row;
            hero.Col = col;
        }

        if (_revealedBonuses.TryGetValue(roomId, out var revealed))
        {
            foreach (HeroState hero in heroes)
            {
                if (!hero.Alive) continue;
                BonusState? picked = revealed.FirstOrDefault(b => b.Row == hero.Row && b.Col == hero.Col);
                if (picked is null) continue;

                if (picked.Kind == "BLAST_LENGTH")
                {
                    hero.BlastLength++;
                    hero.TicksPerMove = Math.Max(1, hero.TicksPerMove - 1);
                }
                else if (picked.Kind == "BOMB_COUNT")
                {
                    hero.MaxBombs++;
                    hero.BombCount++;
                }

                revealed.Remove(picked);
            }
        }

        List<HeroState> stillAlive = heroes.Where(h => h.Alive).ToList();
        if (stillAlive.Count <= 1 && heroes.Count > 1)
        {
            int? winnerId = stillAlive.Count == 1 ? stillAlive[0].UserId : null;
            await FinishMatchAsync(heroes, winnerId);
            return new TickResult(blastCells, true, winnerId);
        }

        return new TickResult(blastCells, false, null);
    }

    private async Task FinishMatchAsync(List<HeroState> heroes, int? winnerId)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        UserService users = scope.ServiceProvider.GetRequiredService<UserService>();

        foreach (HeroState hero in heroes)
        {
            if (hero.UserId < 0) continue; // bots have no user record
            await users.ApplyMatchResult(hero.UserId, hero.UserId == winnerId);
        }
    }

    private static readonly (int DRow, int DCol, string Name)[] BotDirections =
        [(-1, 0, "UP"), (1, 0, "DOWN"), (0, -1, "LEFT"), (0, 1, "RIGHT")];

    // real shortest-path first-step, not just "which neighbor reduces raw distance" — a purely
    // greedy Manhattan-distance walker gets stuck rattling against a pillar/wall face forever
    // whenever the direct line to the target is blocked and reaching it actually requires a
    // detour (temporarily moving further away), which is exactly what this checkerboard-pillar
    // map does to every path. 15x15 is tiny, so a fresh BFS per bot per decision is essentially free.
    // BFS reachability check: from bombPos, is there any cell outside the hypothetical blast
    // that can actually be walked to within a small move budget? A 1-tile-away check is
    // meaningless here since every open adjacent cell is inherently inside a fresh bomb's own
    // blast (blastLength >= 1) — this instead asks "can I get clear before the 3s fuse ends."
    private static bool CanEscapeIfBombedHere(int[][] map, HashSet<(int Row, int Col)> bombCells, (int Row, int Col) bombPos, int blastLength)
    {
        HashSet<(int Row, int Col)> blastSet = GetBlastCells(map, bombPos.Row, bombPos.Col, blastLength).ToHashSet();
        var visited = new HashSet<(int Row, int Col)> { bombPos };
        var queue = new Queue<((int Row, int Col) Pos, int Dist)>();
        queue.Enqueue((bombPos, 0));
        int maxSteps = blastLength + 3;

        while (queue.Count > 0)
        {
            var (pos, dist) = queue.Dequeue();
            if (dist > 0 && !blastSet.Contains(pos)) return true;
            if (dist >= maxSteps) continue;

            foreach (var d in BotDirections)
            {
                var next = (Row: pos.Row + d.DRow, Col: pos.Col + d.DCol);
                if (next.Row < 0 || next.Row >= map.Length || next.Col < 0 || next.Col >= map[0].Length) continue;
                if (map[next.Row][next.Col] == 1 || map[next.Row][next.Col] == 2) continue;
                if (bombCells.Contains(next)) continue;
                if (!visited.Add(next)) continue;
                queue.Enqueue((next, dist + 1));
            }
        }

        return false;
    }

    private static string? FindFirstStepTowards(int[][] map, HashSet<(int Row, int Col)> bombCells, (int Row, int Col) from, (int Row, int Col) to)
    {
        if (from == to) return null;

        var visited = new HashSet<(int Row, int Col)> { from };
        var queue = new Queue<((int Row, int Col) Pos, string FirstStep)>();

        foreach (var d in BotDirections)
        {
            var next = (Row: from.Row + d.DRow, Col: from.Col + d.DCol);
            if (next.Row < 0 || next.Row >= map.Length || next.Col < 0 || next.Col >= map[0].Length) continue;
            if (map[next.Row][next.Col] == 1 || map[next.Row][next.Col] == 2 || bombCells.Contains(next)) continue;
            if (!visited.Add(next)) continue;

            if (next == to) return d.Name;
            queue.Enqueue((next, d.Name));
        }

        while (queue.Count > 0)
        {
            var (pos, firstStep) = queue.Dequeue();
            foreach (var d in BotDirections)
            {
                var next = (Row: pos.Row + d.DRow, Col: pos.Col + d.DCol);
                if (next.Row < 0 || next.Row >= map.Length || next.Col < 0 || next.Col >= map[0].Length) continue;
                if (map[next.Row][next.Col] == 1 || map[next.Row][next.Col] == 2 || bombCells.Contains(next)) continue;
                if (!visited.Add(next)) continue;

                if (next == to) return firstStep;
                queue.Enqueue((next, firstStep));
            }
        }

        return null; // target unreachable through open floor right now
    }

    // BFS to the nearest cell outside dangerCells, returning the first step of that path — a
    // 1-step lookahead ("is my immediate neighbor currently marked dangerous?") can't tell a
    // dead end from a corridor that reaches safety a few tiles further on, and picking randomly
    // among "equally dangerous right now" neighbors is how a bot walked itself into a fully
    // enclosed pocket (wall on 3 sides, its own bomb on the 4th) instead of the corridor that
    // actually led clear.
    private static string? FindFirstStepToSafety(int[][] map, HashSet<(int Row, int Col)> bombCells, (int Row, int Col) from, HashSet<(int Row, int Col)> dangerCells)
    {
        var visited = new HashSet<(int Row, int Col)> { from };
        var queue = new Queue<((int Row, int Col) Pos, string FirstStep)>();

        foreach (var d in BotDirections)
        {
            var next = (Row: from.Row + d.DRow, Col: from.Col + d.DCol);
            if (next.Row < 0 || next.Row >= map.Length || next.Col < 0 || next.Col >= map[0].Length) continue;
            if (map[next.Row][next.Col] == 1 || map[next.Row][next.Col] == 2 || bombCells.Contains(next)) continue;
            if (!visited.Add(next)) continue;

            if (!dangerCells.Contains(next)) return d.Name;
            queue.Enqueue((next, d.Name));
        }

        while (queue.Count > 0)
        {
            var (pos, firstStep) = queue.Dequeue();
            foreach (var d in BotDirections)
            {
                var next = (Row: pos.Row + d.DRow, Col: pos.Col + d.DCol);
                if (next.Row < 0 || next.Row >= map.Length || next.Col < 0 || next.Col >= map[0].Length) continue;
                if (map[next.Row][next.Col] == 1 || map[next.Row][next.Col] == 2 || bombCells.Contains(next)) continue;
                if (!visited.Add(next)) continue;

                if (!dangerCells.Contains(next)) return firstStep;
                queue.Enqueue((next, firstStep));
            }
        }

        return null; // no reachable safe cell right now — genuinely cornered
    }

    private void RunBotAI(string roomId, int[][] map, List<HeroState> heroes, List<BombState> bombs)
    {
        var bombCells = bombs.Select(b => (b.Row, b.Col)).ToHashSet();
        var dangerCells = new HashSet<(int Row, int Col)>();
        foreach (BombState bomb in bombs)
            foreach ((int Row, int Col) cell in GetBlastCells(map, bomb.Row, bomb.Col, bomb.BlastLength))
                dangerCells.Add(cell);

        bool CanStep(int r, int c) =>
            r >= 0 && r < map.Length && c >= 0 && c < map[0].Length &&
            map[r][c] != 1 && map[r][c] != 2 && !bombCells.Contains((r, c));

        foreach (HeroState bot in heroes)
        {
            if (bot.UserId >= 0 || !bot.Alive) continue;

            var candidates = BotDirections
                .Select(d => (d.Name, r: bot.Row + d.DRow, c: bot.Col + d.DCol))
                .Where(x => CanStep(x.r, x.c))
                .ToList();

            // only re-decide direction on the tick that will actually move the bot — deciding
            // every 100ms tick while only moving every TicksPerMove ticks made the facing
            // direction visibly flicker between moves, looking twitchy/erratic rather than human.
            // The very first decision must not wait for that, though — TicksSinceMove only ever
            // advances once the bot already has a direction (see the movement loop below), so
            // gating this on TicksSinceMove alone would leave a fresh bot's direction null forever.
            bool aboutToMove = bot.Direction is null || bot.TicksSinceMove >= bot.TicksPerMove - 1;

            if (aboutToMove)
            {
                bool standingInDanger = dangerCells.Contains((bot.Row, bot.Col));

                if (standingInDanger)
                {
                    // find an actual path clear of the blast, not just "is my immediate
                    // neighbor currently flagged dangerous" — every neighbor of a bot standing
                    // right next to a bomb usually *is* still inside the blast pattern, so a
                    // 1-step check can't tell a genuine dead end from a corridor that reaches
                    // safety a few tiles further on
                    string? escapeStep = FindFirstStepToSafety(map, bombCells, (bot.Row, bot.Col), dangerCells);

                    if (escapeStep is not null && candidates.Any(x => x.Name == escapeStep))
                    {
                        bot.Direction = escapeStep;
                    }
                    else if (candidates.Count > 0)
                    {
                        // no clear path to safety found (or its first step is blocked this
                        // instant) — still commit to one direction rather than re-rolling every
                        // tick, since flip-flopping between two equally-doomed tiles is worse
                        // than just picking one and trying
                        bot.Direction = candidates.Any(x => x.Name == bot.Direction)
                            ? bot.Direction
                            : candidates[Random.Shared.Next(candidates.Count)].Name;
                    }
                }
                else
                {
                    // chase the nearest revealed bonus, or else the nearest enemy hero —
                    // without a target bots just wander randomly and never actually hunt or collect
                    (int Row, int Col)? target = null;
                    if (_revealedBonuses.TryGetValue(roomId, out var visibleBonuses) && visibleBonuses.Count > 0)
                    {
                        BonusState nearestBonus = visibleBonuses
                            .OrderBy(b => Math.Abs(b.Row - bot.Row) + Math.Abs(b.Col - bot.Col))
                            .First();
                        target = (nearestBonus.Row, nearestBonus.Col);
                    }
                    else
                    {
                        // slight bias toward the human player: on the symmetric corner-spawn
                        // map, all 4 heroes start roughly equidistant from each other, so a
                        // pure nearest-enemy rule left bots fighting each other almost
                        // exclusively and never actually threatening the human
                        HeroState? nearestEnemy = heroes
                            .Where(h => h.Alive && h.UserId != bot.UserId)
                            .OrderBy(h => (Math.Abs(h.Row - bot.Row) + Math.Abs(h.Col - bot.Col)) - (h.UserId >= 0 ? 3 : 0))
                            .FirstOrDefault();
                        if (nearestEnemy is not null)
                            target = (nearestEnemy.Row, nearestEnemy.Col);
                    }

                    bool continuingIntoDanger = bot.Direction is not null &&
                        candidates.Any(x => x.Name == bot.Direction && dangerCells.Contains((x.r, x.c)));

                    // with an active target, re-aim every move (still looks purposeful, not
                    // twitchy, since the chosen direction rarely flips once actually heading
                    // toward a fixed point) — without one, only reconsider occasionally so
                    // aimless wandering doesn't look erratic either
                    bool shouldReconsider = bot.Direction is null
                        || target is not null
                        || Random.Shared.NextDouble() < 0.08
                        || !candidates.Any(x => x.Name == bot.Direction)
                        || continuingIntoDanger;

                    if (shouldReconsider && candidates.Count > 0)
                    {
                        var preferred = candidates.Where(x => !dangerCells.Contains((x.r, x.c))).ToList();
                        var pool = preferred.Count > 0 ? preferred : candidates;

                        double greediness = bot.BotSkill switch { 1 => 0.5, 2 => 0.65, 3 => 0.8, _ => 0.92 };

                        string? pathStep = target is not null
                            ? FindFirstStepTowards(map, bombCells, (bot.Row, bot.Col), target.Value)
                            : null;
                        bool pathStepIsSafe = pathStep is not null && pool.Any(x => x.Name == pathStep);

                        if (pathStepIsSafe && Random.Shared.NextDouble() < greediness)
                        {
                            bot.Direction = pathStep;
                        }
                        else if (target is not null)
                        {
                            // no safe pathfound step (unreachable right now, or its first step
                            // walks through current danger) — fall back to "reduce raw distance"
                            int bestDist = pool.Min(x => Math.Abs(x.r - target.Value.Row) + Math.Abs(x.c - target.Value.Col));
                            var best = pool.Where(x => Math.Abs(x.r - target.Value.Row) + Math.Abs(x.c - target.Value.Col) == bestDist).ToList();
                            // prefer to keep going the same way if it's still one of the equally-
                            // good options — otherwise a diagonal approach flip-flops axis every
                            // single step (right, up, right, up...) which reads as erratic
                            bot.Direction = best.Any(x => x.Name == bot.Direction)
                                ? bot.Direction
                                : best[Random.Shared.Next(best.Count)].Name;
                        }
                        else
                        {
                            bot.Direction = pool[Random.Shared.Next(pool.Count)].Name;
                        }
                    }
                }
            }

            // don't drop a second bomb while already fleeing one
            if (dangerCells.Contains((bot.Row, bot.Col))) continue;

            // can the bot actually reach somewhere safe within the 3s fuse before this new bomb
            // goes off? Checking only the immediate neighbor is useless here — every open
            // adjacent cell is *always* inside a fresh bomb's own blast (blastLength >= 1), so
            // that check could never pass. What matters is whether a safe cell is reachable at
            // all within a realistic number of moves (a real BFS, not a 1-step lookahead).
            if (!CanEscapeIfBombedHere(map, bombCells, (bot.Row, bot.Col), bot.BlastLength)) continue;

            // don't stack up more than 2 of the bot's own bombs at once — keeps its own blast-danger footprint smaller
            int ownActiveBombs = bombs.Count(b => b.OwnerId == bot.UserId);
            if (ownActiveBombs >= 2) continue;

            bool nearDestructibleWall = BotDirections.Any(d =>
            {
                int r = bot.Row + d.DRow, c = bot.Col + d.DCol;
                return r >= 0 && r < map.Length && c >= 0 && c < map[0].Length && map[r][c] == 2;
            });
            // exact row/column alignment is a near-guaranteed hit; being simply close by is a
            // looser but still real threat (especially once the bot's own hunting closes the
            // rest of the gap next move) — checkerboard-pillar maps make perfect alignment rare,
            // so requiring it outright left bots almost never actually attacking anyone
            bool alignedWithEnemy = heroes.Any(h => h.Alive && h.UserId != bot.UserId &&
                ((h.Row == bot.Row && Math.Abs(h.Col - bot.Col) <= bot.BlastLength) ||
                 (h.Col == bot.Col && Math.Abs(h.Row - bot.Row) <= bot.BlastLength)));
            bool closeToEnemy = heroes.Any(h => h.Alive && h.UserId != bot.UserId &&
                Math.Abs(h.Row - bot.Row) + Math.Abs(h.Col - bot.Col) <= bot.BlastLength + 1);
            bool nearEnemy = alignedWithEnemy || closeToEnemy;

            // decisive when there's an actual reason to bomb (break into a bonus, or catch an
            // enemy in range); otherwise stay conservative so bots don't carpet the map for no reason
            double bombChance;
            if (nearEnemy)
                bombChance = bot.BotSkill switch { 1 => 0.15, 2 => 0.25, 3 => 0.4, _ => 0.6 };
            else if (nearDestructibleWall)
                bombChance = bot.BotSkill switch { 1 => 0.1, 2 => 0.18, 3 => 0.28, _ => 0.4 };
            else
                bombChance = bot.BotSkill switch { 1 => 0.003, 2 => 0.005, 3 => 0.008, _ => 0.012 };

            if (Random.Shared.NextDouble() < bombChance)
                TryPlaceBomb(roomId, bot.UserId, out _);
        }
    }

    private void RevealBonus(string roomId, int row, int col)
    {
        if (!_hiddenBonuses.TryGetValue(roomId, out var hidden)) return;
        string key = $"{row},{col}";
        if (!hidden.TryGetValue(key, out string? kind)) return;

        hidden.Remove(key);
        List<BonusState> revealed = _revealedBonuses.GetOrAdd(roomId, _ => new List<BonusState>());
        revealed.Add(new BonusState { Row = row, Col = col, Kind = kind });
    }

    private static bool IsBlocked(int[][] map, int row, int col, HashSet<(int Row, int Col)> bombCells)
    {
        if (row < 0 || row >= map.Length || col < 0 || col >= map[0].Length) return true;
        if (map[row][col] == 1 || map[row][col] == 2) return true;
        return bombCells.Contains((row, col));
    }

}
