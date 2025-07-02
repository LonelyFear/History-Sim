using Godot;
using System.Collections.Generic;
public class HydrologyGenerator()
{
    Dictionary<Vector2I, Vector2I> flowDirMap;
    float[,] waterFlow;
    public void CalculateFlowDirection()
    {
        flowDirMap = new Dictionary<Vector2I, Vector2I>();
        for (int x = 0; x < WorldGenerator.WorldSize.X; x++)
        {
            for (int y = 0; y < WorldGenerator.WorldSize.Y; y++)
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
                        Vector2I next = new Vector2I(Mathf.PosMod(pos.X + dx, WorldGenerator.WorldSize.X), Mathf.PosMod(pos.Y + dy, WorldGenerator.WorldSize.Y));
                        Vector2I nextNext = new Vector2I(-1, -1);
                        if (flowDirMap.ContainsKey(next))
                        {
                            nextNext = new Vector2I(Mathf.PosMod(next.X + flowDirMap[next].X, WorldGenerator.WorldSize.X), Mathf.PosMod(next.Y + flowDirMap[next].Y, WorldGenerator.WorldSize.Y));
                        }

                        if (WorldGenerator.HeightMap[next.X, next.Y] <= lowestElevation && nextNext != pos)
                        {
                            lowestElevation = WorldGenerator.HeightMap[next.X, next.Y];
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
        waterFlow = new float[WorldGenerator.WorldSize.X, WorldGenerator.WorldSize.Y];
        for (int x = 0; x < WorldGenerator.WorldSize.X; x++)
        {
            for (int y = 0; y < WorldGenerator.WorldSize.Y; y++)
            {
                if (WorldGenerator.HeightMap[x, y] < 0.7f || WorldGenerator.RainfallMap[x, y] < 0.4f)
                {
                    continue;
                }
                waterFlow[x, y] += WorldGenerator.RainfallMap[x, y];
                Vector2I pos = new Vector2I(x, y);
                float attempts = 500;
                while (flowDirMap[pos] != new Vector2I(-1, -1) && WorldGenerator.HeightMap[pos.X, pos.Y] >= WorldGenerator.SeaLevel && attempts > 0)
                {
                    attempts--;
                    waterFlow[flowDirMap[pos].X, flowDirMap[pos].Y] += waterFlow[x, y];
                    pos = flowDirMap[pos];
                }
            }
        }
    }

    public float[,] GenerateHydrologyMap()
    {
        CalculateFlowDirection();
        CalculateFlow();
        return waterFlow;
    }
}