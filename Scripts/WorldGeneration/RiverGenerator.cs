using System;
using System.Collections.Generic;
using Godot;

public class RiverGenerator
{
    public int attemptedRivers = 100;
    public float minRiverDist = 5f;
    public float minRiverLength = 5;
    public float maxRiverLength = Mathf.Inf;
    public bool riverMustEndInWater = true;
    public float minRiverHeight = 0.6f;
    int invalidRivers = 0;
    List<Vector2I> validPositions = new List<Vector2I>();
    bool[,] rivers;
    void GeneratePoints()
    {
        Random rng = WorldGenerator.rng;

        for (int i = 0; i < attemptedRivers; i++)
        {
            bool posGood = true;
            Vector2I pos = new Vector2I(rng.Next(0, WorldGenerator.WorldSize.X), rng.Next(0, WorldGenerator.WorldSize.Y));
            if (!validPositions.Contains(pos) && WorldGenerator.HeightMap[pos.X, pos.Y] > minRiverHeight && rng.NextSingle() < WorldGenerator.RainfallMap[pos.X, pos.Y])
            {
                foreach (Vector2I oPos in validPositions)
                {
                    if (Utility.WrappedDistanceSquaredTo(pos, oPos, WorldGenerator.WorldSize) <= minRiverDist * minRiverDist)
                    {
                        posGood = false;
                        break;
                    }
                }
            }
            else
            {
                posGood = false;
            }

            if (posGood)
            {
                validPositions.Add(pos);
            }
            else
            {
                invalidRivers++;
            }
        }
        GD.Print("Attempting to generate " + validPositions.Count + " rivers");
    }
    public void RunRiverGeneration()
    {
        GD.Print("Generating Rivers, Make sure you got a good heightmap!");
        rivers = new bool[WorldGenerator.WorldSize.X, WorldGenerator.WorldSize.Y];
        GeneratePoints();
        GenerateRivers();
        BiomeRivers();
        GD.Print("Generated " + (attemptedRivers - invalidRivers) + " rivers");
    }

    void GenerateRivers()
    {
        float[,] heightmap = WorldGenerator.HeightMap;
        int maxAttempts = 10000;
        foreach (Vector2I riverStart in validPositions)
        {

            Vector2I pos = riverStart;
            List<Vector2I> currentRiver = new List<Vector2I>();
            bool endFound = false;
            bool waterEnd = false;

            int attempts = 0;
            currentRiver.Add(pos);
            while (!endFound && currentRiver.Count <= maxRiverLength && attempts < maxAttempts)
            {
                attempts++;
                Vector2I lowestPos = pos;
                float lowestElevation = heightmap[pos.X, pos.Y] * 20;
                for (int dx = -1; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 2; dy++)
                    {
                        if (/*(dx != 0 && dy != 0) ||*/ (dx == 0 && dy == 0))
                        {
                            continue;
                        }
                        Vector2I next = new Vector2I(Mathf.PosMod(pos.X + dx, WorldGenerator.WorldSize.X), Mathf.PosMod(pos.Y + dy, WorldGenerator.WorldSize.Y));
                        if (heightmap[next.X, next.Y] <= lowestElevation && !currentRiver.Contains(next))
                        {
                            lowestElevation = heightmap[next.X, next.Y];
                            lowestPos = next;
                        }
                    }
                }
                currentRiver.Add(lowestPos);
                if (heightmap[lowestPos.X, lowestPos.Y] < WorldGenerator.SeaLevel)
                {
                    endFound = true;
                    waterEnd = true;
                }
                else if (lowestPos == pos)
                {
                    endFound = true;
                }
                pos = lowestPos;
            }
            if ((currentRiver.Count >= minRiverLength && currentRiver.Count <= maxRiverLength) || endFound)
            {
                if (waterEnd)
                {
                    AddRiverToGlobal(currentRiver);
                }
                else if (!riverMustEndInWater)
                {
                    AddRiverToGlobal(currentRiver);
                }
                else
                {
                    invalidRivers += 1;
                }
            }
            else
            {
                invalidRivers += 1;
            }
        }
    }

    void AddRiverToGlobal(List<Vector2I> river)
    {
        foreach (Vector2I pos in river)
        {
            rivers[pos.X, pos.Y] = true;
        }
    }

    void BiomeRivers()
    {
        for (int x = 0; x < WorldGenerator.WorldSize.X; x++)
        {
            for (int y = 0; y < WorldGenerator.WorldSize.Y; y++)
            {
                if (rivers[x, y])
                {
                    WorldGenerator.BiomeMap[x,y] = AssetManager.GetBiome("river");
                }
            }
        }
    }
}