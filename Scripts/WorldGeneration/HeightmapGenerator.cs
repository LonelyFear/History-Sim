using Godot;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

public class HeightmapGenerator
{
    float[,] heightmap;
    bool[,] midpoints;
    int gridSizeX = 16;
    int gridSizeY = 16;
    int ppcx;
    int ppcy;
    TerrainTile[,] tiles;
    List<Vector2I> offshore = new List<Vector2I>();
    List<Plate> plates = new List<Plate>();
    List<VoronoiRegion> continentalRegions = new List<VoronoiRegion>();
    List<VoronoiRegion> voronoiRegions = new List<VoronoiRegion>();
    Vector2I worldSize;
    float worldMult;
    float seaLevel;
    
    
    
    Dictionary<Vector2I, VoronoiRegion> points;
    static Random rng = new Random();
    Curve riftValleyCurve = GD.Load<Curve>("res://Curves/Landforms/RiftValley.tres");
    Curve islandArcCurve = GD.Load<Curve>("res://Curves/Landforms/IslandArc.tres");
    Curve volcanicRangeCurve = GD.Load<Curve>("res://Curves/Landforms/VolcanicRange.tres");
    Curve coastalErosionCurve = GD.Load<Curve>("res://Curves/CoastalCurve.tres");
    Curve mountainCurve = GD.Load<Curve>("res://Curves/Landforms/MountainCurve.tres");
    Curve plateauCurve = GD.Load<Curve>("res://Curves/Landforms/PlateauCurve.tres");

    // Public Variables
    public float seaFloorLevel = 0.1f;
    float landCoverage = 0.5f;
    float shelfDepth = 0.05f;
    float avgElevationAboveSea = 0f;



    public float[,] GenerateHeightmap()
    {
        rng = new Random(WorldGenerator.Seed);
        seaLevel = WorldGenerator.SeaLevel - shelfDepth;
        worldSize = WorldGenerator.WorldSize;
        worldMult = WorldGenerator.WorldMult;
        heightmap = new float[worldSize.X, worldSize.Y];
        midpoints = new bool[worldSize.X, worldSize.Y];
        tiles = new TerrainTile[worldSize.X, worldSize.Y];
        points = GeneratePoints();
        GenerateRegions(6);
        GenerateContinents();
        GetDistances();
        try
        {
            GeneratePlates(10);
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
        GetTectonicPressure();
        AdjustHeightMap();
        try
        {
            TectonicEffects();
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }

        float aboveSeaLevelTiles = 0;
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                if (heightmap[x, y] > WorldGenerator.SeaLevel) {
                    avgElevationAboveSea += heightmap[x, y];
                    aboveSeaLevelTiles++;
                }
            }
        }
        avgElevationAboveSea /= aboveSeaLevelTiles;
        GD.Print("Avg Elevation Above Sea Level: " + WorldGenerator.GetUnitElevation(avgElevationAboveSea).ToString("#,###0 meters") + " meters");
        return heightmap;
    }

    public void TectonicEffects()
    {
        FastNoiseLite widthNoise = new FastNoiseLite(rng.Next(-99999, 99999));
        widthNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        widthNoise.SetFractalOctaves(8);
        widthNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        FastNoiseLite heightNoise = new FastNoiseLite(rng.Next(-99999, 99999));
        heightNoise.SetFractalType(FastNoiseLite.FractalType.Ridged);
        heightNoise.SetFractalOctaves(8);
        heightNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                float minWidth = 0f;
                TerrainTile tile = tiles[x, y];

                float noiseValue = Mathf.InverseLerp(-1, 1, heightNoise.GetNoise(x, y));
                float boundaryFactor;
                TerrainTile boundary = tile.nearestBoundary;
                
                if (boundary == null)
                {
                    continue;
                }

                if (boundary.region.continental)
                {
                    if (boundary.collisionContinental)
                    {
                        // Mountain Ranges
                        if (boundary.pressure > 0)
                        {
                            // Normal Ranges
                            minWidth = 8f + (widthNoise.GetNoise(x, y) * 3f);
                            boundaryFactor = 1f - (tile.boundaryDist / minWidth);
                            if (tile.boundaryDist <= minWidth)
                            {
                                heightmap[x, y] += 0.35f * mountainCurve.Sample(boundaryFactor) * Mathf.Clamp(boundary.pressure, 0, 1) * Mathf.Lerp(0.4f, 1f, noiseValue);
                            }
                        }
                        else
                        {
                            // Rift Valleys
                            minWidth = 8f + (widthNoise.GetNoise(x, y) * 3f);
                            boundaryFactor = 1f - (tile.boundaryDist / minWidth);
                            if (tile.boundaryDist <= minWidth)
                            {
                                heightmap[x, y] -= 0.3f * (1f - riftValleyCurve.Sample(boundaryFactor)) * Mathf.Clamp(boundary.pressure, 0, 1) * Mathf.Lerp(0.4f, 1f, noiseValue);
                            }
                        }
                    }
                    else
                    {
                        // Inland Volcanoes
                        if (boundary.pressure > 0)
                        {
                            minWidth = 12f + (widthNoise.GetNoise(x, y) * 3f);
                            boundaryFactor = 1f - (tile.boundaryDist / minWidth);
                            if (tile.boundaryDist <= minWidth)
                            {
                                heightmap[x, y] += 0.4f * volcanicRangeCurve.Sample(boundaryFactor) * Mathf.Clamp(boundary.pressure, 0, 1) * Mathf.Lerp(0.4f, 1f, noiseValue);
                            }
                        }
                    }
                }
                else
                {
                    if (boundary.collisionContinental)
                    {
                        // TODO
                    }
                    else
                    {
                        // Island Arcs
                        if (boundary.pressure > 0)
                        {
                            minWidth = 8f + (widthNoise.GetNoise(x, y) * 3f);
                            boundaryFactor = 1f - (tile.boundaryDist / minWidth);
                            if (tile.boundaryDist <= minWidth)
                            {
                                heightmap[x, y] += 0.5f * islandArcCurve.Sample(boundaryFactor) * Mathf.Clamp(boundary.pressure, 0, 1) * Mathf.Lerp(0.1f, 1f, noiseValue);
                            }

                        }
                    }
                }
            }
        }        
    }
    public void GetTectonicPressure()
    {
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                TerrainTile tile = tiles[x, y];
                if (!tile.fault)
                {
                    continue;
                }
                int otherTiles = 0;
                int density = tile.region.plate.density;
                if (!tile.region.continental)
                {
                    density += 1000;
                }
                for (int dx = -3; dx < 4; dx++)
                {
                    for (int dy = -3; dy < 4; dy++)
                    {
                        Vector2I testPos = new Vector2I(Mathf.PosMod(x + dx, worldSize.X), Mathf.PosMod(y + dy, worldSize.Y));
                        TerrainTile next = tiles[testPos.X, testPos.Y];
                        if (next.region.plate != tile.region.plate)
                        {
                            otherTiles++;
                            Vector2 relativeVel = tile.region.plate.dir - next.region.plate.dir;
                            if (relativeVel.Length() * relativeVel.Normalized().Dot(testPos - new Vector2I(x, y)) < 0)
                            {
                                tile.pressure += 0.5f * relativeVel.Length();
                            }
                            else
                            {
                                tile.pressure += -0.5f * relativeVel.Length();
                            }
                            tile.collisionContinental = next.region.continental;
                            if (tile.collisionContinental)
                            {
                                tile.sank = density > next.region.plate.density;
                            }
                        }
                    }
                }
                tile.pressure /= otherTiles;
                if (tile.pressure >= 1)
                {
                    tile.convergent = true;
                }
            }
        }
    }
    public void GeneratePlates(int amount)
    {
        int nonPlateRegions = voronoiRegions.Count();
        int platesToGenerate = Mathf.Clamp(amount, 0, gridSizeX * gridSizeY);
        int attempts = 99999;
        while (platesToGenerate > 0)
        {
            VoronoiRegion region = voronoiRegions.PickRandom(rng);
            if (region.continental && region.plate == null)
            {
                Plate plate = new Plate()
                {
                    dir = new Vector2(Mathf.Lerp(-2, 2, rng.NextSingle()), Mathf.Lerp(-2, 2, rng.NextSingle())),
                    density = rng.Next(0, 100)
                };
                region.plate = plate;
                plates.Add(plate);
                nonPlateRegions--;
                platesToGenerate--;
            }
        }
        GD.Print("Plates generated");
        attempts = 5000;
        while (nonPlateRegions > 0 && attempts > 0)
        {
            attempts--;
            foreach (VoronoiRegion region in voronoiRegions)
            {
                if (region.plate == null)
                {
                    continue;
                }
                VoronoiRegion border = region.borderingRegions.PickRandom(rng);
                if (border.plate == null)
                {
                    border.plate = region.plate;
                    nonPlateRegions--;
                }
            }
        }
        GD.Print("Plates grown");
        // Checks if tiles are on a plate border
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                VoronoiRegion region = tiles[x, y].region;
                Vector2I pos = new Vector2I(x, y);
                for (int dx = -1; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 2; dy++)
                    {
                        if ((dx == 0 && dy == 0) || tiles[x, y].fault)
                        {
                            continue;
                        }
                        Vector2I next = new Vector2I(Mathf.PosMod(pos.X + dx, worldSize.X), Mathf.PosMod(pos.Y + dy, worldSize.Y));
                        VoronoiRegion neighbor = tiles[next.X, next.Y].region;
                        if (neighbor.plate != region.plate)
                        {
                            tiles[x, y].fault = true;
                            region.boundaryTiles.Add(pos);
                        }
                    }
                }
            }
        }
        // Gets Distance From Nearest Boundary
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                TerrainTile tile = tiles[x, y];
                TerrainTile boundary = null;
                float shortestDistSquared = Mathf.Inf;
                foreach (var entry in tile.edgeDistancesSquared)
                {
                    TerrainTile nextTile = tiles[entry.Key.X, entry.Key.Y];
                    if (entry.Value < shortestDistSquared && nextTile.fault)
                    {
                        boundary = nextTile;
                        shortestDistSquared = entry.Value;
                    }
                }
                tile.nearestBoundary = boundary;
                tile.boundaryDist = Mathf.Sqrt(shortestDistSquared);
            }
        }   
    }
    public void GenerateContinents()
    {
        int attempts = 2000;
        while (continentalRegions.Count < Mathf.RoundToInt(voronoiRegions.Count * landCoverage) && attempts > 0)
        {
            attempts--;
            foreach (VoronoiRegion region in continentalRegions.ToArray())
            {
                VoronoiRegion border = region.borderingRegions[rng.Next(0, region.borderingRegions.Count - 1)];
                if (continentalRegions.Count < Mathf.RoundToInt(voronoiRegions.Count * landCoverage))
                {
                    SetRegionContinental(true, border);
                }
            }
        }
    }

    public void AdjustHeightMap()
    {
        FastNoiseLite heightNoise = new FastNoiseLite(rng.Next(-99999, 99999));
        heightNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        heightNoise.SetFractalOctaves(8);
        heightNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        FastNoiseLite erosion = new FastNoiseLite(rng.Next(-99999, 99999));
        erosion.SetFractalType(FastNoiseLite.FractalType.Ridged);
        erosion.SetFractalOctaves(4);
        erosion.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        float scale = 2f;
        float erosionScale = 0.5f;
        float maxNoiseValue = float.MinValue;
        float minNoiseValue = float.MaxValue;
        float maxErosionValue= float.MinValue;
        float minErosionValue = float.MaxValue;
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                float value = heightNoise.GetNoise(x / scale, y / scale);
                float er = erosion.GetNoise(x / erosionScale, y / erosionScale);
                if (value > maxNoiseValue)
                {
                    maxNoiseValue = value;
                }
                if (value < minNoiseValue)
                {
                    minNoiseValue = value;
                }
                if (er > maxErosionValue)
                {
                    maxErosionValue = er;
                }
                if (er < minErosionValue)
                {
                    minErosionValue = er;
                }
            }
        }
        for (int x = 0; x < worldSize.X; x++){
            for (int y = 0; y < worldSize.Y; y++)
            {
                float noiseValue = Mathf.InverseLerp(minErosionValue, maxErosionValue, erosion.GetNoise(x / erosionScale, y / erosionScale));
                float coastMultiplier;
                if (tiles[x, y].region.continental)
                {
                    // Land hills
                    coastMultiplier = Mathf.Clamp(tiles[x, y].coastDist / (worldMult * Mathf.Lerp(2f, 20f, noiseValue)), 0f, 1f);
                    float topDist = 1f - seaLevel - 0.05f;
                    heightmap[x, y] = seaLevel + (Mathf.InverseLerp(minNoiseValue, maxNoiseValue, heightNoise.GetNoise(x / scale, y / scale)) * topDist * Mathf.Clamp(coastalErosionCurve.Sample(coastMultiplier), 0, 1));
                    heightmap[x, y] = Mathf.Clamp(heightmap[x, y], seaLevel, 1f);
                    //heightmap[x, y] = Mathf.Clamp(tiles[x, y].boundaryDist/10f, 0.6f, 1f);
                }
                else
                {
                    // Sea Floor
                    coastMultiplier = Mathf.Clamp(tiles[x, y].coastDist / (worldMult * Mathf.Lerp(2f, 5f, noiseValue)), 0f, 1f);
                    float seaFloorHeight = seaFloorLevel + (Mathf.InverseLerp(minNoiseValue, maxNoiseValue, heightNoise.GetNoise(x / scale, y / scale)) * (seaLevel / 4f));
                    heightmap[x, y] = Mathf.Lerp(seaLevel, seaFloorHeight, Mathf.Clamp(coastalErosionCurve.Sample(coastMultiplier), 0, 1));
                }
            }
        }
    }
    Dictionary<Vector2I, VoronoiRegion> GeneratePoints()
    {
        ppcx = Mathf.RoundToInt(worldSize.X / (float)gridSizeX);
        ppcy = Mathf.RoundToInt(worldSize.Y / (float)gridSizeY);
        Dictionary<Vector2I, VoronoiRegion> point = new Dictionary<Vector2I, VoronoiRegion>();
        for (int i = 0; i < gridSizeX; i++) {
            for (int j = 0; j < gridSizeY; j++)
            {
                VoronoiRegion region = new VoronoiRegion();
                region.seed = new Vector2I(i * ppcx + rng.Next(0, ppcx), j * ppcy + rng.Next(0, ppcy));
                point.Add(new Vector2I(i, j), region);
                voronoiRegions.Add(region);
            }
        }
        return point;
    }
    public void GenerateRegions(int landCount)
    {
        int addedLand = 0;
        int attempts = 5000;
        while (addedLand < landCount && attempts > 0)
        {
            attempts--;
            VoronoiRegion region = voronoiRegions[rng.Next(0, voronoiRegions.Count)];
            if (!region.continental)
            {
                addedLand += 1;
                SetRegionContinental(true, region);
            }
        }

        // Assigns tiles to their region
        FastNoiseLite xNoise = new FastNoiseLite(rng.Next(-99999, 99999));
        xNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        xNoise.SetFractalOctaves(8);
        xNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        FastNoiseLite yNoise = new FastNoiseLite(rng.Next(-99999, 99999));
        yNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        yNoise.SetFractalOctaves(8);
        yNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        float scale = 2;
        GD.Print(new Vector2I(-3, 2).WrappedMidpoint(new Vector2I(5, 2), worldSize));
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {

                TerrainTile tile = new TerrainTile();
                // Domain warping
                int fx = (int)Mathf.PosMod(x + (xNoise.GetWrappedNoise(x / scale, y / scale, worldSize) * 50), worldSize.X);
                int fy = (int)Mathf.PosMod(y + (yNoise.GetWrappedNoise(x / scale, y / scale, worldSize) * 50), worldSize.Y);

                Vector2I pos = new Vector2I(fx, fy);
                VoronoiRegion region = null;
                // Loops through the points
                int gx = fx / ppcx;
                int gy = fy / ppcy;
                try
                {
                    PriorityQueue<VoronoiRegion, float> distances = new PriorityQueue<VoronoiRegion, float>();
                    for (int i = -1; i < 2; i++)
                    {
                        for (int j = -1; j < 2; j++)
                        {
                            int gridX = Mathf.PosMod(gx - i, gridSizeX);
                            int gridY = Mathf.PosMod(gy - j, gridSizeY);
                            float dist = pos.WrappedDistanceSquaredTo(points[new Vector2I(gridX, gridY)].seed, worldSize);
                            distances.Enqueue(points[new Vector2I(gridX, gridY)], dist);
                        }
                    }
                    region = distances.Dequeue();
                    tile.region = region;
                    tiles[x, y] = tile;
                }
                catch (Exception e)
                {
                    GD.PushError(e);
                }


            }
        }

        // Gets Region Borders
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                VoronoiRegion region = tiles[x, y].region;
                Vector2I pos = new Vector2I(x, y);
                for (int dx = -1; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 2; dy++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }
                        Vector2I next = new Vector2I(Mathf.PosMod(pos.X + dx, worldSize.X), Mathf.PosMod(pos.Y + dy, worldSize.Y));
                        VoronoiRegion neighbor = tiles[next.X, next.Y].region;
                        if (neighbor != region)
                        {
                            tiles[x, y].border = true;
                            if (!region.borderingRegions.Contains(neighbor))
                            {
                                region.borderingRegions.Add(neighbor);
                            }
                            if (!region.edges.Contains(pos))
                            {
                                region.edges.Add(pos);
                            }
                        }

                    }
                }
            }
        }    
    }

    void GetDistances()
    {
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                VoronoiRegion region = tiles[x, y].region;
                Vector2I pos = new Vector2I(x, y);
                for (int dx = -1; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 2; dy++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }
                        Vector2I next = new Vector2I(Mathf.PosMod(pos.X + dx, worldSize.X), Mathf.PosMod(pos.Y + dy, worldSize.Y));
                        VoronoiRegion neighbor = tiles[next.X, next.Y].region;
                        if (neighbor != region)
                        {
                            tiles[x, y].border = true;
                            if (!region.borderingRegions.Contains(neighbor))
                            {
                                region.borderingRegions.Add(neighbor);
                            }          
                            if (region.continental != neighbor.continental)
                            {
                                region.coastal = true;
                                tiles[x, y].coastal = true;
                                region.coastalTiles.Add(pos);
                            }
                        }

                    }
                }
            }
        }

        // Gets Relevant Tiles for each Tile
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                TerrainTile tile = tiles[x, y];
                List<Vector2I> tilesToCheck = [.. tile.region.edges];
                foreach (VoronoiRegion region in tile.region.borderingRegions)
                {
                    if (region.coastal && tile.region.continental && region.continental)
                    {
                        tilesToCheck.AddRange(region.edges);
                    }
                }

                Vector2I pos = new Vector2I(x, y);
                if (tilesToCheck.Count > 0)
                {
                    foreach (Vector2I next in tilesToCheck)
                    {
                        tile.edgeDistancesSquared.Add(next, pos.WrappedDistanceSquaredTo(next, worldSize));
                    }
                }
            }
        }
        
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                TerrainTile tile = tiles[x, y];
                Vector2I pos = new Vector2I(x, y);
                float shortestDistSquared = Mathf.Inf;
                foreach (var entry in tile.edgeDistancesSquared)
                {
                    TerrainTile nextTile = tiles[entry.Key.X, entry.Key.Y];
                    if (entry.Value < shortestDistSquared && nextTile.coastal)
                    {
                        shortestDistSquared = entry.Value;
                    }
                }
                tile.coastDist = Mathf.Sqrt(shortestDistSquared);
            }
        }        
    }

    void SetRegionContinental(bool value, VoronoiRegion region) {

        if (value == true)
        {
            region.continental = true;
            continentalRegions.Add(region);
        }
        else
        {
            region.continental = false;
            continentalRegions.Remove(region);
        }
    }
}
internal class VoronoiRegion
{
    public Vector2I seed;
    public bool continental = false;
    public bool coastal = false;
    public Plate plate;
    public List<Vector2I> coastalTiles = new List<Vector2I>();
    public List<Vector2I> boundaryTiles = new List<Vector2I>();
    public List<VoronoiRegion> borderingRegions = new List<VoronoiRegion>();
    public List<Vector2I> edges = new List<Vector2I>();
}
internal class TerrainTile
{
    public VoronoiRegion region;
    public float coastDist = Mathf.Inf;
    public float boundaryDist = Mathf.Inf;
    public TerrainTile nearestBoundary = null;
    public Dictionary<Vector2I, float> edgeDistancesSquared = new Dictionary<Vector2I, float>();
    public float pressure = 0f;
    public bool collisionContinental = false;
    public bool convergent;
    public bool coastal;
    public bool border;
    public bool fault;
    public bool sank = false;
    public bool offshore;
}
internal class Plate
{
    public List<VoronoiRegion> regions;
    public Vector2 dir;
    public int density;
}
