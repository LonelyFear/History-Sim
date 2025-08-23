using Godot;
using System.Collections.Generic;
public class HydrologyGenerator()
{
    Dictionary<Vector2I, Vector2I> flowDirMap;
    float[,] waterFlow;
    public void CalculateFlowDirection(WorldGenerator world)
    {
        flowDirMap = new Dictionary<Vector2I, Vector2I>();
        for (int x = 0; x < world.WorldSize.X; x++)
        {
            for (int y = 0; y < world.WorldSize.Y; y++)
            {
                Vector2I pos = new Vector2I(x, y);
                Vector2I flowDir = new Vector2I(-1, -1);
                float lowestElevation = Mathf.Inf;
                for (int dx = -1; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 2; dy++)
                    {
                        if ((dx != 0 && dy != 0) || (dx == 0 && dy == 0))
                        {
                            continue;
                        }
                        Vector2I next = new Vector2I(Mathf.PosMod(pos.X + dx, world.WorldSize.X), Mathf.PosMod(pos.Y + dy, world.WorldSize.Y));
                        Vector2I nextNext = new Vector2I(-1, -1);
                        if (flowDirMap.ContainsKey(next))
                        {
                            nextNext = new Vector2I(Mathf.PosMod(next.X + flowDirMap[next].X, world.WorldSize.X), Mathf.PosMod(next.Y + flowDirMap[next].Y, world.WorldSize.Y));
                        }

                        if (world.HeightMap[next.X, next.Y] <= lowestElevation && nextNext != pos)
                        {
                            lowestElevation = world.HeightMap[next.X, next.Y];
                            flowDir = next;
                        }
                    }
                }
                flowDirMap.Add(pos, flowDir);
            }
        }
    }

    public void CalculateFlow(WorldGenerator world)
    {
        waterFlow = new float[world.WorldSize.X, world.WorldSize.Y];
        for (int x = 0; x < world.WorldSize.X; x++)
        {
            for (int y = 0; y < world.WorldSize.Y; y++)
            {
                if (world.HeightMap[x, y] < 0.7f || world.RainfallMap[x, y] < 0.4f)
                {
                    continue;
                }
                waterFlow[x, y] += world.RainfallMap[x, y];
                Vector2I pos = new Vector2I(x, y);
                float attempts = 500;
                while (flowDirMap[pos] != new Vector2I(-1, -1) && world.HeightMap[pos.X, pos.Y] >= world.SeaLevel && attempts > 0)
                {
                    attempts--;
                    waterFlow[flowDirMap[pos].X, flowDirMap[pos].Y] += waterFlow[x, y];
                    pos = flowDirMap[pos];
                }
            }
        }
    }

    public float[,] GenerateHydrologyMap(WorldGenerator world)
    {
        CalculateFlowDirection(world);
        CalculateFlow(world);
        return waterFlow;
    }
}