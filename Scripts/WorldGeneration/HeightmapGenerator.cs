using Godot;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Mutex = System.Threading.Mutex;

public class HeightmapGenerator
{
    float[,] heightmap;
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

    Mutex m = new Mutex();
    WorldGenerator world;
    Dictionary<Vector2I, VoronoiRegion> points;
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
    const float slopeErosionThreshold = 0.1f;


    public float[,] GenerateHeightmap(WorldGenerator worldAssigned)
    {
        world = worldAssigned;
        seaLevel = world.SeaLevel - shelfDepth;
        worldSize = world.WorldSize;
        worldMult = world.WorldMult;
        heightmap = new float[worldSize.X, worldSize.Y];
        tiles = new TerrainTile[worldSize.X, worldSize.Y];
        GD.Print(worldSize);
        ulong startTime = Time.GetTicksMsec();
        points = GeneratePoints();
        GD.Print("Point Gen Time " + ((Time.GetTicksMsec() - startTime) / 1000f).ToString("0.0s"));
        startTime = Time.GetTicksMsec();
        GenerateRegions(world.continents);
        GD.Print("Voronoi Region Time " + ((Time.GetTicksMsec() - startTime) / 1000f).ToString("0.0s"));
        world.Stage++;
        startTime = Time.GetTicksMsec();
        GenerateContinents();
        GD.Print("Continent Time " + ((Time.GetTicksMsec() - startTime) / 1000f).ToString("0.0s"));
        startTime = Time.GetTicksMsec();
        GetDistances();
        GD.Print("Distance Time " + ((Time.GetTicksMsec() - startTime) / 1000f).ToString("0.0s"));
        startTime = Time.GetTicksMsec();
        world.Stage++;
        try
        {
            GeneratePlates(15);
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
        GD.Print("Plate Gen Time " + ((Time.GetTicksMsec() - startTime) / 1000f).ToString("0.0s"));
        startTime = Time.GetTicksMsec();
        GetTectonicPressure();
        GD.Print("Pressure Calc Time " + ((Time.GetTicksMsec() - startTime) / 1000f).ToString("0.0s"));
        startTime = Time.GetTicksMsec();
        AdjustHeightMap();
        GD.Print("Heightmap Time " + ((Time.GetTicksMsec() - startTime) / 1000f).ToString("0.0s"));
        startTime = Time.GetTicksMsec();

        TectonicEffects();

        world.Stage++;
        for (int i = 0; i <= 10; i++)
        {
            ThermalErosion();
        }
        GD.Print("Collision Time " + ((Time.GetTicksMsec() - startTime) / 1000f).ToString("0.0s"));
        world.Stage++;
        world.tiles = tiles;
        return heightmap;
    }

    public void ThermalErosion()
    {
        int divisions = 1;
        Parallel.For(1, divisions + 1, (i) =>
        {
            for (int x = worldSize.X / divisions * (i - 1); x < worldSize.X / divisions * i; x++)
            {
                for (int y = 0; y < worldSize.Y; y++)
                {
                    TerrainTile tile = tiles[x, y];
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            Vector2I testPos = new Vector2I(Mathf.PosMod(x + dx, worldSize.X), Mathf.PosMod(y + dy, worldSize.Y));
                            TerrainTile testTile = tiles[testPos.X, testPos.Y];
                            float slope = heightmap[testPos.X, testPos.Y] - heightmap[x,y];

                            if (Mathf.Abs(slope) > slopeErosionThreshold)
                            {
                                float elevation = heightmap[x,y];
                                float testElevation = heightmap[testPos.X, testPos.Y];

                                heightmap[x,y] = Mathf.Lerp(elevation, testElevation, 0.5f);
                            }
                        }
                    }
                }
            }
        });          
    }

    public void TectonicEffects()
    {
        FastNoiseLite widthNoise = new FastNoiseLite(world.rng.Next(-99999, 99999));
        widthNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        widthNoise.SetFractalOctaves(8);
        widthNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        widthNoise.SetFrequency(0.1f);
        FastNoiseLite heightNoise = new FastNoiseLite(world.rng.Next(-99999, 99999));
        heightNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        heightNoise.SetFractalOctaves(8);
        heightNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        heightNoise.SetFrequency(0.1f);
        int divisions = 8;
        Parallel.For(1, divisions + 1, (i) =>
        {
            for (int x = worldSize.X / divisions * (i - 1); x < worldSize.X / divisions * i; x++)
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
                    float widthNoiseValue =  Mathf.InverseLerp(-1, 1, widthNoise.GetNoise(x, y));
                    if (boundary.region.continental)
                    {
                        // Mountain Ranges
                        if (boundary.pressure > 0)
                        {
                            // Normal Ranges
                            minWidth = 15f * Mathf.Lerp(0.2f, 1f, widthNoiseValue);
                            boundaryFactor = 1f - (tile.boundaryDist / minWidth);
                            if (tile.boundaryDist <= minWidth)
                            {
                                heightmap[x, y] += 0.3f * mountainCurve.Sample(boundaryFactor) * Mathf.Clamp(boundary.pressure, 0, 1) * Mathf.Lerp(0.1f, 1f, noiseValue);
                            }
                        }
                    }
                }
            }
        });       
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

                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        Vector2I testPos = new Vector2I(Mathf.PosMod(x + dx, worldSize.X), Mathf.PosMod(y + dy, worldSize.Y));
                        TerrainTile next = tiles[testPos.X, testPos.Y];
                        if (next.region.plate != tile.region.plate)
                        {
                            otherTiles++;
                            Vector2 relativeVel = tile.region.plate.dir - next.region.plate.dir;
                            float relativeSpeed = relativeVel.Length();
                            if (relativeVel.Length() * relativeVel.Normalized().Dot(testPos - new Vector2I(x, y)) < 0)
                            {
                                tile.pressure += 1f * relativeSpeed;
                            }
                            else
                            {
                                tile.pressure += -1f * relativeSpeed;
                            }
                            tile.collisionContinental = next.region.continental;
                            
                            int otherDensity = next.region.plate.density;
                            if (!next.region.continental) {
                                otherDensity += 1000;
                            }
                            tile.sank = density > otherDensity;
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
            VoronoiRegion region = voronoiRegions.PickRandom(world.rng);
            if (region.continental && region.plate == null)
            {
                Plate plate = new Plate()
                {
                    dir = new Vector2(Mathf.Lerp(-1, 1, world.rng.NextSingle()), Mathf.Lerp(-1, 1, world.rng.NextSingle())),
                    density = world.rng.Next(0, 100)
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
                VoronoiRegion border = region.borderingRegions.PickRandom(world.rng);
                if (border.plate == null)
                {
                    border.plate = region.plate;
                    nonPlateRegions--;
                }
            }
        }
        GD.Print("Plates grown");
        // Checks if tiles are on a plate border
        int divisions = 8;
        Parallel.For(1, divisions + 1, (i) =>
        {
            for (int x = worldSize.X / divisions * (i - 1); x < worldSize.X / divisions * i; x++)
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
        });
        // Gets Distance From Nearest Boundary
        Parallel.For(1, divisions + 1, (i) =>
        {
            for (int x = worldSize.X / divisions * (i - 1); x < worldSize.X / divisions * i; x++)
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
        });  
    }
    public void GenerateContinents()
    {
        int attempts = 4000;
        GD.Print(Mathf.RoundToInt(voronoiRegions.Count * landCoverage));
        while (continentalRegions.Count != Mathf.RoundToInt(voronoiRegions.Count * landCoverage) && attempts > 0)
        {
            attempts--;
            foreach (VoronoiRegion region in continentalRegions.ToArray())
            {
                if (region.borderingRegions.Count == 0)
                {
                    continue;
                }
                VoronoiRegion border = region.borderingRegions[world.rng.Next(0, region.borderingRegions.Count)];
                if (continentalRegions.Count < Mathf.RoundToInt(voronoiRegions.Count * landCoverage))
                {
                    SetRegionContinental(true, border);
                }
            }
        }
        GD.Print(continentalRegions.Count);
    }

    public void AdjustHeightMap()
    {
        FastNoiseLite heightNoise = new FastNoiseLite(world.rng.Next(-99999, 99999));
        heightNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        heightNoise.SetFractalOctaves(8);
        heightNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        FastNoiseLite erosion = new FastNoiseLite(world.rng.Next(-99999, 99999));
        erosion.SetFractalType(FastNoiseLite.FractalType.Ridged);
        erosion.SetFractalOctaves(4);
        erosion.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        float scale = 1f;
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
        int divisions = 4;
        Parallel.For(1, divisions + 1, (i) =>
        {
            for (int x = worldSize.X / divisions * (i - 1); x < worldSize.X / divisions * i; x++)
            {
                for (int y = 0; y < worldSize.Y; y++)
                {
                    float noiseValue = Mathf.InverseLerp(minErosionValue, maxErosionValue, erosion.GetNoise(x / erosionScale, y / erosionScale));
                    float coastMultiplier;
                    if (tiles[x, y].region.continental)
                    {
                        // Land hills
                        coastMultiplier = Mathf.Clamp(tiles[x, y].coastDist / (worldMult * Mathf.Lerp(2f, 30f, noiseValue)), 0f, 1f);
                        float topDist = 1f - seaLevel - 0.05f;
                        heightmap[x, y] = seaLevel + (Mathf.InverseLerp(minNoiseValue, maxNoiseValue, heightNoise.GetNoise(x / scale, y / scale)) * topDist * Mathf.Clamp(coastalErosionCurve.Sample(coastMultiplier), 0, 1));
                        heightmap[x, y] = Mathf.Clamp(heightmap[x, y], seaLevel, 1f);
                        //heightmap[x, y] = Mathf.Clamp(tiles[x, y].boundaryDist/10f, 0.6f, 1f);
                    }
                    else
                    {
                        // Sea Floor
                        coastMultiplier = Mathf.Clamp(tiles[x, y].coastDist / (worldMult * Mathf.Lerp(2f, 5f, noiseValue)), 0f, 1f);
                        float seaFloorHeight = seaFloorLevel + (Mathf.InverseLerp(minNoiseValue, maxNoiseValue, heightNoise.GetNoise(x / scale, y / scale)) * (seaLevel / 2f));
                        heightmap[x, y] = Mathf.Lerp(seaLevel, seaFloorHeight, Mathf.Clamp(coastalErosionCurve.Sample(coastMultiplier), 0, 1));
                    }
                }
            }
        });
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
                region.seed = new Vector2I(i * ppcx + world.rng.Next(0, ppcx), j * ppcy + world.rng.Next(0, ppcy));
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
            VoronoiRegion region = voronoiRegions[world.rng.Next(0, voronoiRegions.Count)];
            if (!region.continental)
            {
                addedLand += 1;
                SetRegionContinental(true, region);
            }
        }

        // Assigns tiles to their region
        FastNoiseLite xNoise = new FastNoiseLite(world.rng.Next());
        xNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        xNoise.SetFractalOctaves(8);
        xNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        FastNoiseLite yNoise = new FastNoiseLite(world.rng.Next());
        yNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        yNoise.SetFractalOctaves(8);
        yNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        float scale = 2;
        GD.Print(new Vector2I(-3, 2).WrappedMidpoint(new Vector2I(5, 2), worldSize));

        int divisions = 18;
        Parallel.For(1, divisions + 1, (parallelIterator) =>
        {
            for (int x = worldSize.X / divisions * (parallelIterator - 1); x < worldSize.X / divisions * parallelIterator; x++)
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
                    lock (tiles)
                    {
                        tiles[x, y] = tile;
                        tile.pos = new Vector2I(x,y);
                    }
                }
            }
        });
        Parallel.For(1, divisions + 1, (parallelIterator) =>
        {
            // Gets Region Borders
            for (int x = worldSize.X / divisions * (parallelIterator - 1); x < worldSize.X / divisions * parallelIterator; x++)
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
                                lock (region)
                                {
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
        });
    }

    void GetDistances()
    {
        ulong startTime = Time.GetTicksMsec();
        int divisions = 20;
        Parallel.For(1, divisions + 1, (i) =>
        {
            for (int x = worldSize.X / divisions * (i - 1); x < worldSize.X / divisions * i; x++)
            {
                for (int y = 0; y < worldSize.Y; y++)
                {
                    VoronoiRegion region = tiles[x, y].region;
                    Vector2I pos = new(x, y);
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
                                if (region.continental != neighbor.continental && !region.coastalTiles.Contains(pos))
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
        });
        GD.Print("  Neighbor Time " + ((Time.GetTicksMsec() - startTime) / 1000f).ToString("0.0s"));
        startTime = Time.GetTicksMsec();
        PriorityQueue<TerrainTile, int> tilesToCheck = new();
        foreach (VoronoiRegion region in voronoiRegions)
        {
            foreach (Vector2I coastalTilePos in region.coastalTiles)
            {
                TerrainTile coastalTile = tiles[coastalTilePos.X, coastalTilePos.Y];
                tilesToCheck.Enqueue(coastalTile, 0);
                coastalTile.coastDist = 0;
            }
        }

        HashSet<TerrainTile> measuredTiles = new(worldSize.X * worldSize.Y);

        bool fullQueue = true;
        while (fullQueue)
        {
            fullQueue = tilesToCheck.TryDequeue(out TerrainTile currentTile, out int priority);
            if (!fullQueue) break;
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }
                    Vector2I next = new(Mathf.PosMod(currentTile.pos.X + dx, worldSize.X), Mathf.PosMod(currentTile.pos.Y + dy, worldSize.Y));
                    TerrainTile neighbor = tiles[next.X, next.Y];              
                    if (!neighbor.coastal && !measuredTiles.Contains(neighbor))
                    {
                        measuredTiles.Add(neighbor);
                        neighbor.coastDist = currentTile.coastDist + 1;
                        tilesToCheck.Enqueue(neighbor, (int)neighbor.coastDist);
                    }          
                }
            }
        }
        GD.Print("  Coast Dist Time " + ((Time.GetTicksMsec() - startTime) / 1000f).ToString("0.0s")); 
    }

    void SetRegionContinental(bool value, VoronoiRegion region) {

        if (value == true && !continentalRegions.Contains(region))
        {
            region.continental = true;
            continentalRegions.Add(region);
        }
    }
}
public class VoronoiRegion
{
    public Vector2I seed;
    public bool continental = false;
    public bool coastal = false;
    public Plate plate;
    public List<Vector2I> coastalTiles = new List<Vector2I>();
    public List<Vector2I> boundaryTiles = new List<Vector2I>();
    public List<VoronoiRegion> borderingRegions = new List<VoronoiRegion>();
    public List<Vector2I> edges = new List<Vector2I>();
    //public Dictionary<VoronoiRegion, Vector2I> edges = Dictionary<VoronoiRegion, Vector2I>();
}
public class TerrainTile
{
    public Vector2I pos;
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

    public TerrainTile Clone()
    {
        return new TerrainTile()
        {
            region = region,
            coastDist = coastDist,
            boundaryDist = boundaryDist,
            nearestBoundary = nearestBoundary,
            edgeDistancesSquared = edgeDistancesSquared,
            pressure = pressure,
            collisionContinental = collisionContinental,
            convergent = convergent,
            coastal = coastal,
            border = border,
            fault = fault,
            sank = sank,
            offshore = offshore
        };
    }
}
public class Plate
{
    public List<VoronoiRegion> regions;
    public Vector2 dir;
    public int density;
}
