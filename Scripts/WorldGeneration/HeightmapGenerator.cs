using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mutex = System.Threading.Mutex;

public class HeightmapGenerator
{
    float[,] heightmap;
    int gridSizeX = 16;
    int gridSizeY = 16;
    int ppcx;
    int ppcy;
    TerrainCell[,] tiles;
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
    Curve amplitudeCurve = GD.Load<Curve>("res://Curves/Landforms/AmplitudeCurve.tres");
    Curve mountainCurve = GD.Load<Curve>("res://Curves/Landforms/MountainCurve.tres");
    Curve oceanRidgeCurve = GD.Load<Curve>("res://Curves/Landforms/OceanRidgeCurve.tres");

    // Public Variables
    public float seaFloorLevel = 0.1f;
    public float landCoverage = 0.3f;
    public float maxHillHeight = 0.25f;
    float shelfDepth = 0.0f;
    const float slopeErosionThreshold = 0.3f;
    public int largePlates = 7;
    public int smallPlates = 4;
    public int largeContinents = 4;
    public int smallContinents = 2;

    public void UseEarthHeightmap(WorldGenerator worldAssigned)
    {
        world = worldAssigned;
        worldSize = world.WorldSize;
        int[,] map = new int[worldSize.X, worldSize.Y];
        float pixelPerX = 4320 / (float)worldSize.X;
        float pixelPerY = 2160 / (float)worldSize.Y;
        world.Stage = WorldGenStage.CONTINENTS;
        int[,] realElevation = ReadBinaryHeightModel("Sprites/Earth2014.SUR2014.5min.geod.bin", 4320, 2160);
        tiles = new TerrainCell[worldSize.X, worldSize.Y];

        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                tiles[x,y] = new TerrainCell()
                {
                    pos = new Vector2I(x,y)
                };
                int px = (int)(x * pixelPerX);
                int py = (int)(y * pixelPerY);
                int flippedPy = (2160 - 1) - py;
                map[x,y] = realElevation[px, flippedPy] - (int)(world.SeaLevel * WorldGenerator.WorldHeight);
            }
        }    

        Queue<TerrainCell> tilesToCheck = new();
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                for (int dx = -1; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 2; dy++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }
                        Vector2I next = new Vector2I(Mathf.PosMod(x + dx, worldSize.X), Mathf.PosMod(y + dy, worldSize.Y));
                        if (map[x,y] < world.SeaLevel * WorldGenerator.WorldHeight)
                        {
                            tiles[x,y].coastal = true;
                            tiles[x,y].coastDist = 0;
                            tilesToCheck.Enqueue(tiles[x,y]);
                        }
                    }
                }
            }
        }  
       
        GetDists(tilesToCheck, true);
        DeliverHeightData(map);            
    }
    public void DeliverHeightData(int[,] heightData)
    {
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                world.cells[x,y].elevation = heightData[x,y];
            }
        }
    }
    public static int[,] ReadBinaryHeightModel(string path, int width, int height)
    {
        int[,] heightData = new int[width,height];

        FileStream fileStream = new(path, FileMode.Open, System.IO.FileAccess.Read);
        BinaryReader binaryReader = new BinaryReader(fileStream);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                    byte[] bytes = binaryReader.ReadBytes(sizeof(short));
                    
                    Array.Reverse(bytes);
                    short value = BitConverter.ToInt16(bytes, 0);
                    heightData[x, y] = value;                
            }
        }
        return heightData;
    }
    public void GenerateHeightmap(WorldGenerator worldAssigned)
    {
        world = worldAssigned;
        seaLevel = world.SeaLevel - shelfDepth;
        worldSize = world.WorldSize;
        worldMult = world.WorldMult;
        int[,] map = new int[worldSize.X, worldSize.Y];

        heightmap = new float[worldSize.X, worldSize.Y];

        tiles = new TerrainCell[worldSize.X, worldSize.Y];
        GD.Print(worldSize);
        points = GeneratePoints();
        GenerateRegions();
        PlaceContinentSeeds();
        world.Stage = WorldGenStage.CONTINENTS;
        GenerateContinents();
        GetDistances();
        world.Stage = WorldGenStage.MEASURING;
        GeneratePlates();
        GetTectonicPressure();
        AdjustHeightMap();
        world.Stage = WorldGenStage.TECTONICS;
        TectonicEffects();

        world.Stage = WorldGenStage.EROSION;
        for (int i = 0; i <= 10; i++)
        {
            ThermalErosion();
        }

        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                int seaElevation = (int)(WorldGenerator.WorldHeight * world.SeaLevel);
                map[x,y] = (int)(heightmap[x,y] * WorldGenerator.WorldHeight) - seaElevation;  
                world.cells[x,y].coastDist = tiles[x,y].coastDist;  
                ; 
                world.cells[x,y].heightmapRegionColor = tiles[x,y].region.color; 
                if (tiles[x,y].region.seed == new Vector2I(x, y))
                {
                    world.cells[x,y].heightmapRegionColor = new Color(1,1,0);
                }
                
            }
        }
        DeliverHeightData(map);  
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
                    TerrainCell tile = tiles[x, y];
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            Vector2I testPos = new Vector2I(Mathf.PosMod(x + dx, worldSize.X), Mathf.PosMod(y + dy, worldSize.Y));
                            TerrainCell testTile = tiles[testPos.X, testPos.Y];
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
                    TerrainCell tile = tiles[x, y];

                    float noiseValue = Mathf.InverseLerp(-1, 1, heightNoise.GetNoise(x, y));
                    float boundaryFactor;
                    TerrainCell boundary = tile.nearestBoundary;

                    if (boundary == null)
                    {
                        continue;
                    }
                    float widthNoiseValue =  Mathf.InverseLerp(-1, 1, widthNoise.GetNoise(x, y));

                    bool convergent = boundary.pressure > 0;
                    bool boundaryContinental = boundary.region.IsContinental();
                    bool selfContinental = tile.region.IsContinental();
                    
                    if (selfContinental && boundaryContinental)
                    {
                        if (convergent)
                        {
                            // Continental Collisions
                            minWidth = 15f * Mathf.Lerp(0.2f, 1f, widthNoiseValue);
                            boundaryFactor = 1f - (tile.boundaryDist / minWidth);
                            if (tile.boundaryDist <= minWidth)
                            {
                                heightmap[x, y] += 0.4f * mountainCurve.Sample(boundaryFactor) * Mathf.Clamp(boundary.pressure, 0.5f, 1) * Mathf.Lerp(0.1f, 1f, noiseValue);
                            }                                
                        } else
                        {
                            // Rift Valleys
                            minWidth = 10f * Mathf.Lerp(0.2f, 1f, widthNoiseValue);
                            boundaryFactor = 1f - (tile.boundaryDist / minWidth);
                            if (tile.boundaryDist <= minWidth)
                            {
                                //heightmap[x, y] -= 0.3f * mountainCurve.Sample(boundaryFactor) * Mathf.Clamp(boundary.pressure, 0.5f, 1) * Mathf.Lerp(0.5f, 1f, noiseValue);
                            }                                
                        }
                    } else if (!selfContinental && !boundaryContinental)
                    {
                        float newElevation = 0;
                        // Oceanic Interaction
                        if (!convergent)
                        {
                            // Divergence
                            minWidth = 10f * Mathf.Lerp(0.8f, 1f, widthNoiseValue);
                            boundaryFactor = 1f - (tile.boundaryDist / minWidth);
                            if (tile.boundaryDist <= minWidth)
                            {
                                newElevation += 0.5f * oceanRidgeCurve.Sample(boundaryFactor) * Mathf.Clamp(boundary.pressure, 0.5f, 1) * Mathf.Lerp(0.5f, 1f, noiseValue);
                            }
                        } else
                        {
                            minWidth = 15f * Mathf.Lerp(0.2f, 1f, widthNoiseValue);
                            boundaryFactor = 1f - (tile.boundaryDist / minWidth);
                            if (tile.boundaryDist <= minWidth)
                            {
                                newElevation = 0.7f * mountainCurve.Sample(boundaryFactor) * Mathf.Clamp(boundary.pressure, 0.5f, 1) * Mathf.Lerp(0.1f, 1f, noiseValue);
                            }                            
                        }     
                        heightmap[x,y] = Mathf.Max(heightmap[x,y], newElevation);                   
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
                TerrainCell tile = tiles[x, y];
                if (!tile.fault)
                {
                    continue;
                }
                int otherTiles = 0;
                int density = tile.region.plate.density;

                for (int dx = -10; dx <= 10; dx++)
                {
                    for (int dy = -10; dy <= 10; dy++)
                    {
                        Vector2I testPos = new Vector2I(Mathf.PosMod(x + dx, worldSize.X), Mathf.PosMod(y + dy, worldSize.Y));
                        TerrainCell next = tiles[testPos.X, testPos.Y];
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
                            tile.collisionContinental = next.region.IsContinental();
                            
                            int otherDensity = next.region.plate.density;
                            if (!next.region.IsContinental()) {
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
    void CreatePlate(float growthChance)
    {
        VoronoiRegion region = voronoiRegions.PickRandom(world.rng);
        while (!region.IsContinental() || region.plate != null)
        {
            region = voronoiRegions.PickRandom(world.rng);
        }
        Plate plate = new Plate()
        {
            plateGrowthChance = growthChance,
            dir = new Vector2(Mathf.Lerp(-1, 1, world.rng.NextSingle()), Mathf.Lerp(-1, 1, world.rng.NextSingle())),
            density = world.rng.Next(0, 100)
        };
        region.plate = plate;
        plates.Add(plate);        
    }

    public void GeneratePlates()
    {
        int nonPlateRegions = voronoiRegions.Count();
        int attempts = 99999;

        // Growth chances
        // Small plates grow slower than big plates.
        for (int i = 0; i < smallPlates; i++)
        {
            CreatePlate(Mathf.Lerp(0.1f, 0.3f, world.rng.NextSingle()));
            nonPlateRegions--;
        }
        for (int i = 0; i < largePlates; i++)
        {
            CreatePlate(Mathf.Lerp(0.75f, 1f, world.rng.NextSingle()));
            nonPlateRegions--;                 
        }

        attempts = 5000;
        // Grows plates
        while (nonPlateRegions > 0 && attempts > 0)
        {
            attempts--;
            foreach (VoronoiRegion region in voronoiRegions)
            {
                if (region.plate == null || region.borderingRegions.Count() < 1)
                {
                    continue;
                }
                VoronoiRegion border = region.borderingRegions.PickRandom(world.rng);
                if (border.plate == null && world.rng.NextSingle() < region.plate.plateGrowthChance)
                {
                    border.plate = region.plate;
                    nonPlateRegions--;
                }
            }
        }
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
        Queue<TerrainCell> tilesToCheck = new();
        foreach (VoronoiRegion region in voronoiRegions)
        {
            foreach (Vector2I boundaryTilePos in region.boundaryTiles)
            {
                TerrainCell boundaryTile = tiles[boundaryTilePos.X, boundaryTilePos.Y];
                tilesToCheck.Enqueue(boundaryTile);
                boundaryTile.boundaryDist = 0;
                boundaryTile.nearestBoundary = boundaryTile;
            }
        }

        HashSet<TerrainCell> measuredTiles = new(worldSize.X * worldSize.Y);

        GetDists(tilesToCheck, false);
    }
    public void GenerateContinents()
    {
        int attempts = 4000;
        int maxContinentalRegions = Mathf.RoundToInt(voronoiRegions.Count * landCoverage);

        while (continentalRegions.Count != Math.Max(maxContinentalRegions, 1) && attempts > 0)
        {
            attempts--;
            foreach (VoronoiRegion region in continentalRegions.ToArray())
            {
                if (region.borderingRegions.Count == 0)
                {
                    continue;
                }
                VoronoiRegion border = region.borderingRegions[world.rng.Next(0, region.borderingRegions.Count)];
                bool borderIsCoastal = false;

                foreach (VoronoiRegion borderBorder in border.borderingRegions)
                {
                    if (borderBorder.IsContinental() && borderBorder.continent != region.continent)
                    {
                        borderIsCoastal = true;
                        break;
                    }
                }

                bool chancePassed = world.rng.NextSingle() < region.continent.growthChance && (!borderIsCoastal || world.rng.NextSingle() < 0.01f);

                if (!border.IsContinental() && continentalRegions.Count < maxContinentalRegions && chancePassed)
                {
                    SetRegionContinental(border, region.continent);                       
                }
            }
        }
    }

    public void AdjustHeightMap()
    {
        FastNoiseLite heightNoise = new FastNoiseLite(world.rng.Next(-99999, 99999));
        heightNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        heightNoise.SetFractalOctaves(8);
        heightNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        heightNoise.SetDomainWarpAmp(3f);
        heightNoise.SetDomainWarpType(FastNoiseLite.DomainWarpType.OpenSimplex2);

        FastNoiseLite erosion = new FastNoiseLite(world.rng.Next(-99999, 99999));
        erosion.SetFractalType(FastNoiseLite.FractalType.Ridged);
        erosion.SetFractalOctaves(8);
        erosion.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        erosion.SetFrequency(0.001f);
        float scale = 4f;

        float maxNoiseValue = float.MinValue;
        float minNoiseValue = float.MaxValue;
        float maxErosionValue= float.MinValue;
        float minErosionValue = float.MaxValue;
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                float value = heightNoise.GetNoise(x, y);
                float er = erosion.GetNoise(x * scale, y * scale);
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
        int divisions = 8;
        Parallel.For(1, divisions + 1, (i) =>
        {
            for (int x = worldSize.X / divisions * (i - 1); x < worldSize.X / divisions * i; x++)
            {
                for (int y = 0; y < worldSize.Y; y++)
                {
                    TerrainCell cell = tiles[x,y];
                    float noiseValue = Mathf.InverseLerp(minErosionValue, maxErosionValue, erosion.GetNoise(x * scale, y * scale));
                    //noiseValue = 0.5f;
                    float coastMultiplier;
                    if (cell.region.IsContinental())
                    {
                        // Land hills
                        coastMultiplier = Mathf.Clamp(tiles[x, y].coastDist / (worldMult * Mathf.Lerp(0, 80f, noiseValue)), 0f, 1f);
                        //float topDist = 1f - (seaLevel + 0.1f);
                        heightmap[x, y] = 0.25f * coastMultiplier;
                        heightmap[x, y] += seaLevel;
                        //heightmap[x, y] = Mathf.Clamp(tiles[x, y].boundaryDist/10f, 0.6f, 1f);
                    } else
                    {
                        coastMultiplier = Mathf.Clamp(tiles[x, y].coastDist / (worldMult * Mathf.Lerp(0, 10f, noiseValue)), 0f, 1f);
                        heightmap[x, y] = Mathf.Lerp(0f, seaLevel, (1f - coastMultiplier));
                        //heightmap[x, y] += seaLevel;                     
                    }
                }
            }
        });

        FastNoiseLite noise = new FastNoiseLite(world.rng.Next(-99999, 99999));
        heightNoise.SetFractalType(FastNoiseLite.FractalType.None);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        float minNoise = float.MaxValue;
        float maxNoise = float.MinValue;
        Parallel.For(1, divisions + 1, (i) =>
        {
            for (int x = worldSize.X / divisions * (i - 1); x < worldSize.X / divisions * i; x++)
            {
                for (int y = 0; y < worldSize.Y; y++)
                {
                    TerrainCell cell = tiles[x,y];
                    float n = GetSlopeDependentNoise(x, y, noise, 8, 0.004f, 1f, 2.0f, 0.5f);
                    float noiseValue = Mathf.InverseLerp(-1, 1, n);
                    if (n < minNoise)
                    {
                        minNoise = n;
                    }
                    if (n > maxNoise)
                    {
                        maxNoise = n;
                    }
                    heightmap[x, y] += Mathf.Lerp(-0.2f, 0.2f, noiseValue);
                }
            }
        });
        GD.Print("Min erosion noise: " + minNoise);
        GD.Print("Max erosion noise: " + maxNoise);
    }
    public float GetSlopeDependentNoise(float x, float y, FastNoiseLite noise, int octaves, float frequency, float amplitude, float lacunarity, float persistence)
    {
        float amp = amplitude;
        float eps = 1f;
        float value = 0f;
        Vector2 p = new(x, y);
        Vector2 d = Vector2.Zero;

        noise.SetFrequency(frequency);
        for (int i = 0; i < octaves; i++)
        {
            float n = noise.GetNoise(p.X, p.Y);
            float dx = (noise.GetNoise(p.X + eps, p.Y) - noise.GetNoise(p.X - eps, p.Y))/(2 * eps);
            float dy = (noise.GetNoise(p.X, p.Y + eps) - noise.GetNoise(p.X, p.Y - eps))/(2 * eps);
            d += new Vector2(dx, dy);
            //GD.Print(d.Length());
            float w = amplitudeCurve.Sample(d.Length());
            w = Mathf.Clamp(w, 0, 3);
            //GD.Print(w);
            value += amp * n * w;

            p *= lacunarity;
            amp *= persistence;  
        }
        //noise.SetFrequency(frequency);
        return value /= 3f;  
    }
    Dictionary<Vector2I, VoronoiRegion> GeneratePoints()
    {
        ppcx = Mathf.RoundToInt(worldSize.X / (float)gridSizeX);
        ppcy = Mathf.RoundToInt(worldSize.Y / (float)gridSizeY);
        Dictionary<Vector2I, VoronoiRegion> point = new Dictionary<Vector2I, VoronoiRegion>();
        for (int i = 0; i < gridSizeX; i++) {
            for (int j = 0; j < gridSizeY; j++)
            {
                VoronoiRegion region = new VoronoiRegion()
                {
                    color = new Color(world.rng.NextSingle(), world.rng.NextSingle(), world.rng.NextSingle())
                };
                region.seed = new Vector2I(i * ppcx + world.rng.Next(0, ppcx), j * ppcy + world.rng.Next(0, ppcy));
                point.Add(new Vector2I(i, j), region);
                voronoiRegions.Add(region);
            }
        }
        return point;
    }

    
    public void PlaceContinentSeeds()
    {
        int addedLand = 0;
        int attempts = 200;
        while (addedLand < largeContinents && attempts > 0)
        {
            attempts--;
            VoronoiRegion region = voronoiRegions[world.rng.Next(0, voronoiRegions.Count)];
            if (TrySetRegionContinental(region, continentalRegions))
            {
                addedLand += 1;
                SetRegionContinental(region, new TerrainContinent()
                {
                    color = new Color(world.rng.NextSingle(), world.rng.NextSingle(), world.rng.NextSingle()),
                    growthChance = Mathf.Lerp(0.1f,0.3f, world.rng.NextSingle())
                });  
            }
        }  

        addedLand = 0;
        attempts = 200;
        while (addedLand < smallContinents && attempts > 0)
        {
            attempts--;
            VoronoiRegion region = voronoiRegions[world.rng.Next(0, voronoiRegions.Count)];
            if (TrySetRegionContinental(region, continentalRegions))
            {
                addedLand += 1;
                SetRegionContinental(region, new TerrainContinent()
                {
                    color = new Color(world.rng.NextSingle(), world.rng.NextSingle(), world.rng.NextSingle()),
                    growthChance = Mathf.Lerp(0.1f,0.3f, world.rng.NextSingle())
                });  
            }
        }    
    }
    bool TrySetRegionContinental(VoronoiRegion region, List<VoronoiRegion> continentSeeds)
    {
        if (region.IsContinental())
        {
            return false;
        }
        foreach (VoronoiRegion otherSeed in continentSeeds)
        {
            if (Utility.WrappedDistance(region.seed, otherSeed.seed, worldSize) < worldSize.X / 6f)
            {
                return false;
            }
        }
        return true;     
    }
    public void GenerateRegions()
    {
        // Assigns tiles to their region
        FastNoiseLite xNoise = new FastNoiseLite(world.rng.Next());
        xNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        xNoise.SetFractalOctaves(8);
        xNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        FastNoiseLite yNoise = new FastNoiseLite(world.rng.Next());
        yNoise.SetFractalType(FastNoiseLite.FractalType.FBm);
        yNoise.SetFractalOctaves(8);
        yNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        GD.Print(new Vector2I(-3, 2).WrappedMidpoint(new Vector2I(5, 2), worldSize));

        int divisions = 8;
        Parallel.For(1, divisions + 1, (parallelIterator) =>
        {
            for (int x = worldSize.X / divisions * (parallelIterator - 1); x < worldSize.X / divisions * parallelIterator; x++)
            {
                for (int y = 0; y < worldSize.Y; y++)
                {

                    TerrainCell tile = new TerrainCell();
                    // Domain warping
                    int fx = (int)Mathf.PosMod(x + (xNoise.GetWrappedNoise(x, y, worldSize) * 40), worldSize.X);
                    int fy = (int)Mathf.PosMod(y + (yNoise.GetWrappedNoise(x, y, worldSize) * 40), worldSize.Y);

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
                            float dist = pos.WrappedDistanceSquared(points[new Vector2I(gridX, gridY)].seed, worldSize);
                            distances.Enqueue(points[new Vector2I(gridX, gridY)], dist);
                        }
                    }
                    region = distances.Dequeue();
                    lock (tiles)
                    {
                        tiles[x, y] = tile;
                    }
                    tile.pos = new Vector2I(x,y);                    
                    region.AddCell(tile);
                }
            }
        });

        // Makes sure regions are connected 
        List<(VoronoiRegion, List<Vector2I>)> enclaves = new();
        foreach (VoronoiRegion region in voronoiRegions)
        {
            HashSet<Vector2I> remainingCells;
            lock (region.cells)
            {
                remainingCells = [.. region.cells.ToArray()];
            }
            
            Queue<Vector2I> cellsToEvaluate = new();
            cellsToEvaluate.Enqueue(region.seed);
            while (cellsToEvaluate.Count > 0)
            {
                Vector2I pos = cellsToEvaluate.Dequeue();
                for (int dx = -1; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 2; dy++)
                    {
                        if ((dx == 0 && dy == 0) || (dx != 0 && dy != 0))
                        {
                            continue;
                        }
                        Vector2I next = new Vector2I(Mathf.PosMod(pos.X + dx, worldSize.X), Mathf.PosMod(pos.Y + dy, worldSize.Y));
                        if (remainingCells.Contains(next))
                        {
                            cellsToEvaluate.Enqueue(next);
                            remainingCells.Remove(next);
                        }
                    }
                }
            }
            Dictionary<VoronoiRegion, int> potenitalMergers = new Dictionary<VoronoiRegion, int>();
            // Removes Disconnected
            foreach (Vector2I pos in remainingCells)
            {
                TerrainCell cell = tiles[pos.X, pos.Y];

                lock (cell.region)
                {
                    cell.region.cells.Remove(pos); 
                    cell.region = null;                  
                }                
            }               
        }

        // Grows regions into enclaves
        Queue<TerrainCell> cellsJoined = new Queue<TerrainCell>();

        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                if (tiles[x,y].region != null)
                {
                    cellsJoined.Enqueue(tiles[x,y]);
                }
            }
        }

        // Fills in gaps from enclaves
        while (cellsJoined.Count > 0)
        {
            TerrainCell cell = cellsJoined.Dequeue();
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    if ((dx == 0 && dy == 0) || (dx != 0 && dy != 0))
                    {
                        continue;
                    }
                    Vector2I next = new Vector2I(Mathf.PosMod(cell.pos.X + dx, worldSize.X), Mathf.PosMod(cell.pos.Y + dy, worldSize.Y));
                    TerrainCell nextCell = tiles[next.X, next.Y];
                    if (nextCell.region == null)
                    {
                        cell.region.AddCell(nextCell);
                        cellsJoined.Enqueue(nextCell);
                    }
                }
            }            
        }

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
        int divisions = 8;
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
                                if (region.IsContinental() != neighbor.IsContinental() && !region.coastalTiles.Contains(pos))
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
        Queue<TerrainCell> tilesToCheck = new();
        foreach (VoronoiRegion region in voronoiRegions)
        {
            foreach (Vector2I coastalTilePos in region.coastalTiles)
            {
                TerrainCell coastalTile = tiles[coastalTilePos.X, coastalTilePos.Y];
                tilesToCheck.Enqueue(coastalTile);
                coastalTile.coastDist = 0;
                coastalTile.nearestCoast = coastalTile;
            }
        }
        GetDists(tilesToCheck, true);
        GD.Print("  Coast Dist Time " + ((Time.GetTicksMsec() - startTime) / 1000f).ToString("0.0s")); 
    }
    void GetDists(Queue<TerrainCell> tilesToCheck, bool coast)
    {
        HashSet<TerrainCell> measuredTiles = new(worldSize.X * worldSize.Y);

        while (tilesToCheck.Count > 0)
        {
            TerrainCell currentTile = tilesToCheck.Dequeue();
            for (int dx = -1; dx < 2; dx++)
            {
                for (int dy = -1; dy < 2; dy++)
                {
                    if ((dx == 0 && dy == 0) || (dx != 0 && dy != 0))
                    {
                        continue;
                    }
                    Vector2I next = new(Mathf.PosMod(currentTile.pos.X + dx, worldSize.X), Mathf.PosMod(currentTile.pos.Y + dy, worldSize.Y));
                    TerrainCell neighbor = tiles[next.X, next.Y];  
                    if (((coast && !neighbor.coastal) || (!coast && !neighbor.fault)) && !measuredTiles.Contains(neighbor))
                    {
                        float additionalDistance = 1;
                        measuredTiles.Add(neighbor);
                        if (coast)
                        {
                            neighbor.nearestCoast = currentTile.nearestCoast;
                            neighbor.coastDist = currentTile.coastDist + additionalDistance;                            
                        } else
                        {
                            neighbor.nearestBoundary = currentTile.nearestBoundary;
                            neighbor.boundaryDist = currentTile.boundaryDist + additionalDistance;
                        }
                        tilesToCheck.Enqueue(neighbor);

                        
                    }                                   
                }
            }
        }        
    }

    void SetRegionContinental(VoronoiRegion region, TerrainContinent continent) {

        if (!continentalRegions.Contains(region))
        {
            region.continent = continent;
            continentalRegions.Add(region);
        }
    }
}
public class VoronoiRegion
{

    public Color color;
    public Vector2I seed;
    public TerrainContinent continent = null;
    public bool coastal = false;
    public Plate plate;
    public List<Vector2I> cells = new List<Vector2I>();
    public List<Vector2I> coastalTiles = new List<Vector2I>();
    public List<Vector2I> boundaryTiles = new List<Vector2I>();
    public List<VoronoiRegion> borderingRegions = new List<VoronoiRegion>();
    public List<Vector2I> edges = new List<Vector2I>();
    //public Dictionary<VoronoiRegion, Vector2I> edges = Dictionary<VoronoiRegion, Vector2I>();
    public bool IsContinental()
    {
        return continent != null;
    }
    public void AddCell(TerrainCell cell)
    {
        if (cell.region != null)
        {
            lock (cell.region)
            {        
                cell.region.cells.Remove(cell.pos);
                lock (cell)
                {
                    cell.region = null;  
                }                    
            }            
        }

        cell.region = this;   
        lock (cells)
        {
            cells.Add(cell.pos);
        }     
    }
}
public class TerrainContinent
{
    public Color color;
    public float growthChance = 0.0f;

}
public class TerrainCell
{
    public Vector2I pos;
    public VoronoiRegion region;
    public float coastDist = Mathf.Inf;
    public float boundaryDist = Mathf.Inf;
    public TerrainCell nearestBoundary = null;
    public TerrainCell nearestCoast = null;
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

public class Plate
{
    public List<VoronoiRegion> regions;
    public Vector2 dir;
    public List<TerrainCell> cells;
    public int density;
    public float plateGrowthChance = 0.0f;
}
