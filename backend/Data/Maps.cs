namespace Backend.Data;

public static class Maps
{
    public static readonly int[][] Classic =
    [
        [1,1,1,1,1,1,1,1,1,1,1,1,1,1,1],
        [1,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
        [1,0,1,0,1,0,1,0,1,0,1,0,1,0,1],
        [1,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
        [1,0,1,0,1,0,1,0,1,0,1,0,1,0,1],
        [1,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
        [1,0,1,0,1,0,1,0,1,0,1,0,1,0,1],
        [1,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
        [1,0,1,0,1,0,1,0,1,0,1,0,1,0,1],
        [1,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
        [1,0,1,0,1,0,1,0,1,0,1,0,1,0,1],
        [1,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
        [1,0,1,0,1,0,1,0,1,0,1,0,1,0,1],
        [1,0,0,0,0,0,0,0,0,0,0,0,0,0,1],
        [1,1,1,1,1,1,1,1,1,1,1,1,1,1,1],
    ];


    private static bool IsNearSpawn(int row, int col, (int Row, int Col)[] spawns)
    {
        foreach (var s in spawns)
        {
            if (Math.Abs(row - s.Row) <= 1 && Math.Abs(col - s.Col) <= 1)
                return true;
        }
        return false;
    }
    public static readonly int[][][] All = [Classic];

    public static int[][] GetRandom()
    {
        return All[Random.Shared.Next(All.Length)];
    }

    public static int[][] Get(int index) => All[index];
    public static (int Row, int Col) GetSpawnPosition(int index, int[][] map)
    {
        int lastRow = map.Length - 2;
        int lastCol = map[0].Length - 2;

        return index switch
        {
            0 => (1, 1),
            1 => (1, lastCol),
            2 => (lastRow, 1),
            3 => (lastRow, lastCol),
            _ => (1, 1),
        };
    }
    public static int[][] GenerateMatchMap(int[][] baseMap)
    {
        int rows = baseMap.Length;
        int cols = baseMap[0].Length;
        int[][] map = new int[rows][];
        for (int r = 0; r < rows; r++)
            map[r] = (int[])baseMap[r].Clone();

        var spawns = new (int Row, int Col)[]
        {
            GetSpawnPosition(0, baseMap),
            GetSpawnPosition(1, baseMap),
            GetSpawnPosition(2, baseMap),
            GetSpawnPosition(3, baseMap),
        };

        for (int r = 1; r < rows - 1; r++)
        {
            for (int c = 1; c < cols - 1; c++)
            {
                if (map[r][c] != 0) continue;
                if (IsNearSpawn(r, c, spawns)) continue;
                if (Random.Shared.NextDouble() < 0.4)
                    map[r][c] = 2;
            }
        }

        return map;
    }

    public static Dictionary<string, string> PlaceBonuses(int[][] map, double chance = 0.2)
    {
        var bonuses = new Dictionary<string, string>();
        for (int r = 0; r < map.Length; r++)
        {
            for (int c = 0; c < map[r].Length; c++)
            {
                if (map[r][c] != 2) continue;
                if (Random.Shared.NextDouble() >= chance) continue;

                string kind = Random.Shared.Next(2) == 0 ? "BLAST_LENGTH" : "BOMB_COUNT";
                bonuses[$"{r},{c}"] = kind;
            } 
        }
        return bonuses;
    }
}
 