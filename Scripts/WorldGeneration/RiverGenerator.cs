using System;
using System.Collections.Generic;
using Godot;

public class RiverGenerator
{
    public int attemptedRivers = 100;
    public float minRiverDist = 5f;
    public float minRiverLength = 5;
    public bool allowCornerConnections = true;
    public float maxRiverLength = Mathf.Inf;
    public bool riverMustEndInWater = true;
    public int minRiverHeight = 1000;
    int invalidRivers = 0;
    List<Vector2I> validPositions = new List<Vector2I>();
    bool[,] rivers;
    void GeneratePoints(WorldGenerator world)
    {
        Random rng = world.rng;

        for (int i = 0; i < attemptedRivers; i++)
        {
            bool posGood = true;
            Vector2I pos = new Vector2I(rng.Next(0, world.WorldSize.X), rng.Next(0, world.WorldSize.Y));
            if (!validPositions.Contains(pos) && world.cells[pos.X, pos.Y].elevation > minRiverHeight && rng.NextSingle() < world.cells[pos.X,pos.Y].GetAnnualRainfall()/3500f && AssetManager.GetBiome(world.cells[pos.X, pos.Y].biomeId).type == "land")
            {
                foreach (Vector2I oPos in validPositions)
                {
                    if (Utility.WrappedDistanceSquared(pos, oPos, world.WorldSize) <= minRiverDist * minRiverDist)
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
    public void RunRiverGeneration(WorldGenerator world)
    {
        GD.Print("Generating Rivers, Make sure you got a good heightmap!");
        rivers = new bool[world.WorldSize.X, world.WorldSize.Y];
        GeneratePoints(world);
        GenerateRivers(world);
        BiomeRivers(world);
        GD.Print("Generated " + (attemptedRivers - invalidRivers) + " rivers");
    }

    void GenerateRivers(WorldGenerator world)
    {
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
                float lowestElevation = world.cells[pos.X, pos.Y].elevation * 20;
                for (int dx = -1; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 2; dy++)
                    {
                        if ((dx != 0 && dy != 0 && !allowCornerConnections) || (dx == 0 && dy == 0))
                        {
                            continue;
                        }
                        Vector2I next = new Vector2I(Mathf.PosMod(pos.X + dx, world.WorldSize.X), Mathf.PosMod(pos.Y + dy, world.WorldSize.Y));
                        if (world.cells[next.X, next.Y].elevation <= lowestElevation && !currentRiver.Contains(next))
                        {
                            lowestElevation = world.cells[next.X, next.Y].elevation;
                            lowestPos = next;
                        }
                    }
                }
                currentRiver.Add(lowestPos);
                if (world.cells[lowestPos.X, lowestPos.Y].elevation < world.SeaLevel)
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

    void BiomeRivers(WorldGenerator world)
    {
        for (int x = 0; x < world.WorldSize.X; x++)
        {
            for (int y = 0; y < world.WorldSize.Y; y++)
            {
                if (rivers[x, y])
                {
                    world.cells[x,y].biomeId = "river";
                }
            }
        }
    }
}