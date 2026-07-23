using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Godot;
using MessagePack;

public class RegionGenerator
{
    SimManager simManager;
    Vector2I worldSize;
    
    Dictionary<ulong, Region> regionIds;
    Tile[,] tiles;
    readonly Random rng = null;
    public const int tilesPerRegion = 4;

    public RegionGenerator(SimManager sim)
    {
        rng = sim.worldGenerator.rng;
        simManager = sim;
        worldSize = sim.worldGenerator.WorldSize;
        tiles = sim.tiles;
        regionIds = sim.regionIds;
    }

    Dictionary<Vector2I,Vector2I> PlaceRegionSeeds(int size, TerrainType[] acceptedTerrain)
    {
        Dictionary<Vector2I,Vector2I> dict = [];
        for (int gx = 0; gx < worldSize.X/size; gx++)
        {
            for (int gy = 0; gy < worldSize.Y/size; gy++)
            {
                Vector2I gridPos = new(gx, gy);
                Vector2I pos = (gridPos * size) + new Vector2I(rng.Next(0, size), rng.Next(0, size));

                int attempts = 500;
                while (!acceptedTerrain.Contains(tiles[pos.X, pos.Y].terrainType) && attempts > 0)
                {
                    attempts--;
                    pos = (gridPos * size) + new Vector2I(rng.Next(0, size), rng.Next(0, size));
                }

                dict.Add(gridPos, pos);
                ObjectManager.CreateRegion(pos.X, pos.Y).terrainType = tiles[pos.X, pos.Y].terrainType;
            }            
        }  
        return dict;      
    }
    void CreateRegionsSquare()
    {
        for (int x = 0; x < worldSize.X/tilesPerRegion; x++)
        {
            for (int y = 0; y < worldSize.Y/tilesPerRegion; y++)
            {
                // Creates a region
                Region newRegion = ObjectManager.CreateRegion(x * tilesPerRegion + 1, y * tilesPerRegion + 1);
                for (int tx = 0; tx < tilesPerRegion; tx++)
                {
                    for (int ty = 0; ty < tilesPerRegion; ty++)
                    {
                        int tilePosX = (x * tilesPerRegion) + tx;
                        int tilePosY = (y * tilesPerRegion) + ty;
                        newRegion.AddTile(tiles[tilePosX, tilePosY]);
                    }                    
                }
            }
        }        
    }
    void CreateRegionsVoronoi()
    {
        Dictionary<TerrainType[], (Dictionary<Vector2I, Vector2I>, int)> gridSeeds = [];

        TerrainType[] acceptedTerrain = [TerrainType.LAND];
        gridSeeds.Add(acceptedTerrain, (PlaceRegionSeeds(4,acceptedTerrain), 4));

        acceptedTerrain = [TerrainType.HILLS, TerrainType.MOUNTAINS];
        gridSeeds.Add(acceptedTerrain, (PlaceRegionSeeds(4,acceptedTerrain), 4));

        acceptedTerrain = [TerrainType.SHALLOW_WATER, TerrainType.DEEP_WATER, TerrainType.ICE];
        gridSeeds.Add(acceptedTerrain, (PlaceRegionSeeds(16,acceptedTerrain), 16));

        // Region voronoi Diagrams
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                Tile tile = tiles[x,y];
                Vector2I gridSize = worldSize;
                Vector2I gridPos = new Vector2I(x,y);

                Dictionary<Vector2I,Vector2I> usedGridSeeds = [];
                foreach (var pair in gridSeeds)
                {
                    if (pair.Key.Contains(tile.terrainType))
                    {
                        usedGridSeeds = pair.Value.Item1;
                        gridSize /= pair.Value.Item2;
                        gridPos /= pair.Value.Item2;
                        break;
                    }
                }
                 
                ulong? closestId = null;
                float closestDist = float.PositiveInfinity;
                // Goes through nearby grid cells
                for (int dx = -1; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 2; dy++)
                    {
                        // Gets distance to seed
                        Vector2I samplePos = new Vector2I(Mathf.PosMod(gridPos.X + dx, gridSize.X), Mathf.PosMod(gridPos.Y + dy, gridSize.Y));
                        float dist = tile.pos.DistanceTo(usedGridSeeds[samplePos]);

                        if (dist < closestDist)
                        {   
                            // Gets seed position
                            int rx = usedGridSeeds[samplePos].X;
                            int ry = usedGridSeeds[samplePos].Y;
                            // Checks if we can be close to this seed
                            if (tile.IsLand() == tiles[rx, ry].IsLand() && (tile.IsLand() || tile.terrainType == tiles[rx,ry].terrainType))
                            {
                                closestId = tiles[rx,ry].regionId;
                                // Multiplies distance if terrain type doesnt match
                                float distMultiplier = tile.terrainType != tiles[rx, ry].terrainType ? 4.0f : 1.0f;
                                // Sets closest distance
                                closestDist = dist * distMultiplier;                                
                            }
                        }
                    }                    
                }
                // Adds tile to region
                if (closestId != null) ObjectManager.GetRegion(closestId).AddTile(tile);
            }
        }

        // List of tiles not in a region
        List<Tile> unassignedTiles = [];

        // Removes Disconnected
        Parallel.ForEach(simManager.regionIds.Values,  (region) =>
        {
            // Initializes remaining cells
            HashSet<Tile> remainingCells;
            lock (region.tiles)
            {
                remainingCells = [..region.tiles.Select(pos => tiles[pos.X, pos.Y])];
            }      
            Queue<Tile> cellsToEvaluate = new();

            // Performs a flood fill to see which tiles are connected to the region
            cellsToEvaluate.Enqueue(tiles[region.pos.X, region.pos.Y]);
            while (cellsToEvaluate.Count > 0)
            {
                Tile tile = cellsToEvaluate.Dequeue();
                Vector2I pos = tile.pos;
                for (int dx = -1; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 2; dy++)
                    {
                        if ((dx == 0 && dy == 0) || (dx != 0 && dy != 0))
                        {
                            continue;
                        }
                        Tile next = tiles[Mathf.PosMod(pos.X + dx, worldSize.X), Mathf.PosMod(pos.Y + dy, worldSize.Y)];
                        if (remainingCells.Contains(next))
                        {
                            cellsToEvaluate.Enqueue(next);
                            remainingCells.Remove(next);
                        }
                    }
                }
            }  
            // Removes remainders
            foreach (Tile tile in remainingCells)
            {
                region.RemoveTile(tile);
            }                      
        });

        // Boundary Queue
        Queue<Tile> cellsJoined = new Queue<Tile>();
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                Tile tile = tiles[x,y];
                if (tile.regionId != null)
                {
                    cellsJoined.Enqueue(tile);                    
                } else
                {
                    unassignedTiles.Add(tile);
                }
            }
        }

        GD.Print("Unassigned Tiles: " + unassignedTiles.Count);

        // Tries to grow regions into empty space
        while (cellsJoined.Count > 0)
        {
            Tile cell = cellsJoined.Dequeue();
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    if ((dx == 0 && dy == 0) || (dx != 0 && dy != 0))
                    {
                        continue;
                    }
                    Vector2I next = new Vector2I(Mathf.PosMod(cell.pos.X + dx, worldSize.X), Mathf.PosMod(cell.pos.Y + dy, worldSize.Y));
                    Tile nextTile = tiles[next.X, next.Y];
                    if (nextTile.regionId == null && cell.IsLand() == nextTile.IsLand() && (cell.IsLand() || cell.terrainType == nextTile.terrainType))
                    {
                        // Adds tile to region if we can
                        ObjectManager.GetRegion(cell.regionId).AddTile(nextTile);
                        cellsJoined.Enqueue(nextTile);
                        unassignedTiles.Remove(nextTile);
                    }
                }
            }            
        } 

        // By this point there are some small pockets left over that we want to have assigned to a new region

        GD.Print("Micro-Regions Tiles: " + unassignedTiles.Count);
        // Creates Final Micro-Regions (Islands, Small Glaciers)
        Dictionary<Vector2I, Vector2I> microRegions = [];
        foreach (Tile tile in unassignedTiles)
        {
            microRegions.Add(tile.pos, tile.pos);
        }

        // Merges Micro-Regions
        cellsJoined = new(unassignedTiles);
        int maxAttempts = unassignedTiles.Count * 2;
        while (maxAttempts > 0 && cellsJoined.Count > 0)
        {
            maxAttempts--;
            Tile cell = cellsJoined.Dequeue();
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }
                    Vector2I next = new Vector2I(Mathf.PosMod(cell.pos.X + dx, worldSize.X), Mathf.PosMod(cell.pos.Y + dy, worldSize.Y));
                    Tile nextTile = tiles[next.X, next.Y];
                    if (microRegions.ContainsKey(next) && microRegions[next] == next && cell.IsLand() == nextTile.IsLand())
                    {
                        microRegions[next] = microRegions[cell.pos];
                        cellsJoined.Enqueue(nextTile);
                    };
                }
            }            
        } 
        // Creates Micro Regions In Object Manager
        foreach (var pair in microRegions)
        {
            Vector2I pos = pair.Key;
            Vector2I rPos = pair.Value;
            
            if (tiles[rPos.X, rPos.Y].regionId == null)
            {
                ObjectManager.CreateRegion(rPos.X, rPos.Y);
            }
            Region r = ObjectManager.GetRegion(tiles[rPos.X, rPos.Y].regionId);
            r.AddTile(tiles[pos.X, pos.Y]);
        }
    }
    void RemoveEmptyRegions()
    {
        foreach (var pair in regionIds.ToArray())
        {
            Region region = pair.Value;
            if (region.tiles.Count < 1)
            {
                regionIds.Remove(region.id);
                continue;
            }       
        } 
    }
    void MergeRegions()
    {
        // Merges Region
        foreach (Region region in regionIds.Values)
        {
            if (region.tiles.Count < 4)
            {
                // Loops over borders
                foreach (Region border in region.borderingRegions.ToArray())
                {
                    // Checks conditions
                    if (border.tiles.Count > 0 && border.tiles.Count < 20 && border.terrainType == region.terrainType)
                    {
                        // Removes references to us
                        foreach (Region otherBorders in region.borderingRegions)
                        {
                            otherBorders.borderingRegions.Remove(region);
                        }

                        // Merges us with border
                        foreach (Vector2I tilePos in region.tiles.ToArray())
                        {
                            Tile tile = tiles[tilePos.X, tilePos.Y];
                            region.RemoveTile(tile);
                            border.AddTile(tile);
                        }
                        break;
                    }
                }
            }
        }
        RemoveEmptyRegions();
    }
    void InitializeRegions()
    {
        foreach (var pair in regionIds.ToArray())
        {
            Region region = pair.Value;
            region.InitRegion();
        }
        RemoveEmptyRegions();     
        simManager.BorderingRegions();   
    }
    
    public void GenerateRegions()
    {
        if (simManager.regionStyle == RegionStyle.Square) CreateRegionsSquare();
        else CreateRegionsVoronoi();

        InitializeRegions();

        if (simManager.regionStyle == RegionStyle.Voronoi) MergeRegions();
    }
}