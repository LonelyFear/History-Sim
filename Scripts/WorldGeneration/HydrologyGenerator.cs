using Godot;
using System.Collections.Generic;
public class HydrologyGenerator()
{
    /*
    public void CalculateFlowDirection()
    {
        Dictionary<Vector2I, Vector2I> flowDirMap = new Dictionary<Vector2I, Vector2I>();
        for (int x = 0; x < WorldGenerator.worldSize.X; x++)
        {
            for (int y = 0; y < WorldGenerator.worldSize.Y; y++)
            {
                Vector2I pos = new Vector2I(x, y);
                Vector2I flowDir = new Vector2I(-1, -1);
                float lowestElevation = WorldGenerator.heightmap[x, y] * 1.1f;
                for (int dx = -1; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 2; dy++)
                    {
                        if ((dx != 0 && dy != 0) || (dx == 0 && dy == 0))
                        {
                            continue;
                        }
                        Vector2I next = new Vector2I(Mathf.PosMod(pos.X + dx, worldSize.X), Mathf.PosMod(pos.Y + dy, worldSize.Y));
                        if (WorldGenerator.heightmap[next.X, next.Y] <= lowestElevation)
                        {
                            lowestElevation = WorldGenerator.heightmap[next.X, next.Y];
                            flowDir = next;
                        }
                    }
                }
                flowDirMap.Add(pos, flowDir);
            }
        }
    }

    public void CalculateFlow()
    {
        waterFlow = new float[WorldGenerator.worldSize.X, WorldGenerator.worldSize.Y];
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                if (heightmap[x, y] < 0.6 || humidmap[x, y] < 0.4f)
                {
                    continue;
                }
                waterFlow[x, y] += humidmap[x, y];
                Vector2I pos = new Vector2I(x, y);
                float attempts = 500;
                while (flowDirMap[pos] != new Vector2I(-1, -1) && heightmap[pos.X, pos.Y] >= seaLevel && attempts > 0)
                {
                    attempts--;
                    waterFlow[flowDirMap[pos].X, flowDirMap[pos].Y] += waterFlow[x, y];
                    pos = flowDirMap[pos];
                }
            }
        }
    }
    */
}