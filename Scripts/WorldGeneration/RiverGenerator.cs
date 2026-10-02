using System;
using System.Collections.Generic;
using Godot;

public class RiverGenerator
{
    public int minRiverDist = 5;
    public float minRiverLength = 5;
    public float maxRiverLength = Mathf.Inf;
    public int minRiverHeight = 700;
    HashSet<Vector2I> validPositions = [];
    bool[,] rivers;
    void GeneratePoints(WorldGenerator world)
    {
        Random rng = world.rng;
        int gridX = Mathf.FloorToInt(world.WorldSize.X/minRiverDist);
        int gridY = Mathf.FloorToInt(world.WorldSize.Y/minRiverDist);
        for (int gx = 0; gx < gridX; gx++)
        {
            for (int gy = 0; gy < gridY; gy++)
            {
                int attempts = Mathf.RoundToInt(10 * world.WorldMult);
                while (attempts > 0)
                {
                    attempts--;
                    int px = (gx * minRiverDist) + rng.Next(0, minRiverDist);
                    int py = (gy * minRiverDist) + rng.Next(0, minRiverDist);

                    Vector2I pos = new(px, py);

                    Cell cell = world.cells[pos.X, pos.Y];

                    float riverSpawnChance = Mathf.Max(cell.GetAnnualRainfall()-500, 0)/1000f * Convert.ToInt32(cell.GetAverageTemp() > -5f);

                    bool posGood = !validPositions.Contains(pos) && cell.elevation > minRiverHeight && AssetManager.GetBiome(cell.biomeId).type == Defines.BiomeType.LAND && rng.NextSingle() < riverSpawnChance; 

                    if (posGood)
                    {
                        validPositions.Add(pos);
                        attempts = 0;
                    }                    
                }
                
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
    }

    void GenerateRivers(WorldGenerator world)
    {
        foreach (Vector2I riverStart in validPositions)
        {
            Vector2I riverEnd = Vector2I.MinValue;
            HashSet<Vector2I> visitedCells = [];
            PriorityQueue<Vector2I, float> oceanFrontier = new();
            oceanFrontier.Enqueue(riverStart, float.MaxValue);
            bool endFound = false;
            try
            {
                while (oceanFrontier.Count > 0 && !endFound)
                {
                    // Finds Nearest Mouth for River (Respecting Height)
                    Vector2I currentPos = oceanFrontier.Dequeue();
                    Cell currentCell = world.cells[currentPos.X, currentPos.Y];
                    for (int dx = -1; dx < 2; dx++)
                    {
                        for (int dy = -1; dy < 2; dy++)
                        {
                            Vector2I borderPos = new(Mathf.PosMod(currentPos.X + dx, world.WorldSize.X), Mathf.PosMod(currentPos.Y + dy, world.WorldSize.Y));
                            Cell borderCell = world.cells[borderPos.X, borderPos.Y];

                            if (!visitedCells.Contains(borderPos))
                            {
                                float heightDifference = currentCell.elevation - borderCell.elevation;
                                oceanFrontier.Enqueue(borderPos, -heightDifference);
                                visitedCells.Add(borderPos);

                                if (borderCell.elevation < 0 || rivers[borderPos.X, borderPos.Y])
                                {
                                    riverEnd = borderPos;
                                    endFound = true;
                                }
                            }
                        }
                    }
                }                
            } 
            catch (Exception e)
            {
                GD.PushError(e);
            }

            //rivers[riverStart.X, riverStart.Y] = true;
            //if (riverEnd != Vector2I.MinValue) rivers[riverEnd.X, riverEnd.Y] = true;
            
            // Paths to Mouth
            PriorityQueue<Vector2I, float> frontier = new();
            Dictionary<Vector2I, float> costSoFar = [];
            Dictionary<Vector2I, Vector2I> cameFrom = [];
            List<Vector2I> path = [];

            frontier.Enqueue(riverStart, 0);
            costSoFar[riverStart] = 0;
            cameFrom[riverStart] = Vector2I.MinValue;


            while (frontier.Count > 0)
            {
                Vector2I currentPos = frontier.Dequeue();
                Cell currentCell = world.cells[currentPos.X, currentPos.Y];

                if (currentPos == riverEnd || currentCell.elevation < 0 || (rivers[currentPos.X, currentPos.Y] && currentPos != riverStart))
                {
                    riverEnd = currentPos;
                    break;
                }

                for (int dx = -1; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 2; dy++)
                    {
                        //if (dx != 0 && dy != 0) continue;

                        Vector2I borderPos = new(Mathf.PosMod(currentPos.X + dx, world.WorldSize.X), Mathf.PosMod(currentPos.Y + dy, world.WorldSize.Y));
                        Cell borderCell = world.cells[borderPos.X, borderPos.Y];

                        float heightDifference = currentCell.elevation - borderCell.elevation;
                        float newCost = costSoFar[currentPos] - heightDifference;

                        if (!costSoFar.TryGetValue(borderPos, out float value) || newCost < value)
                        {
                            costSoFar[borderPos] = newCost;
                            frontier.Enqueue(borderPos, newCost + Heuristic(riverEnd, borderPos));
                            cameFrom[borderPos] = currentPos;
                        }
                    }
                }
            }

            if (cameFrom.ContainsKey(riverEnd))
            {
                Vector2I nextInPath = riverEnd;
                while (nextInPath != Vector2I.MinValue)
                {
                    path.Add(nextInPath);
                    nextInPath = cameFrom[nextInPath];
                }            
            }
            
            if (path.Count > minRiverLength)
            {
                foreach (Vector2I pos in path)
                {
                    rivers[pos.X, pos.Y] = true;
                }
            }
        }
    }
    static float Heuristic(Vector2 posA, Vector2 posB)
    {
        return posA.DistanceSquaredTo(posB);
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