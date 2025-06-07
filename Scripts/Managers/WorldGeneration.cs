using Godot;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using FileAccess = Godot.FileAccess;

public partial class WorldGeneration : Node2D
{
    [Export] public bool tectonicTest;
    [Export] public Sprite2D debugDisplay;
    [Export] Json biomesJson;
    TileMapLayer tileMap;
    TileMapLayer reliefMap;
    public Biome[,] biomes;
    public float[,] heightmap;
    public float[,] tempmap;
    public float[,] humidmap;

    float[] tempThresholds = [0.874f, 0.765f, 0.594f, 0.439f, 0.366f, 0.124f];
    float[] humidThresholds = [0.941f, 0.778f, 0.507f, 0.236f, 0.073f, 0.014f, 0.002f];
    Image terrainImage;
    public bool worldCreated;
    public int worldGenStage;
    public bool startedColoring = false;
    enum TempTypes
    {
        POLAR,
        ALPINE,
        BOREAL,
        COOL,
        WARM,
        SUBTROPICAL,
        TROPICAL,
        INVALID
    }
    enum HumidTypes{
        SUPER_ARID,
        PER_ARID,
        ARID,
        SEMI_ARID,
        SUB_HUMID,
        HUMID,
        PER_HUMID,
        SUPER_HUMID,
        INVALID
    }

    public const float hillThreshold = 0.75f;
    public const float mountainThreshold = 0.8f;
    public const float maxTemperature = 35;
    public const float minTemperature = -30;
    public const float maxRainfall = 4000;
    public const float minRainfall = 50;

    [Signal]
    public delegate void worldgenFinishedEventHandler();

    public Vector2I worldSize = new Vector2I(360, 180);
    [Export] public float worldSizeMult = 4;
    public float seaLevel = 0.6f;

    [ExportCategory("Noise Settings")]
    [Export] public int seed;
    [Export] public float mapScale = 1.5f;
    [Export] public int octaves = 8;
    Random rng;

    public bool generationFinished = false;
    public int worldGenStep = 0;
    public float heightMapProgress;
    public float moistMapProgress;
    public float tempMapProgress;
    public float preparationProgress;

    //[ExportCategory("Rivers Settings")]

    Tectonics tectonics = null;
    public void Init(){
        tileMap = GetNode<TileMapLayer>("Terrain Map");
        reliefMap = GetNode<TileMapLayer>("Reliefs");
        worldSize = (Vector2I)((Vector2)worldSize * worldSizeMult);
        mapScale *= worldSizeMult;

        rng = new Random(seed);
        Scale = new Vector2(1,1) * 80f/worldSize.X;
        tileMap.Scale = new Vector2(1,1) * 16f/tileMap.TileSet.TileSize.X;        
    }

    public override void _Ready()
    {
        if (tectonicTest){
            Init();
            if (tectonics == null){
                tectonics = new Tectonics();
                tectonics.InitSim(this, 16, true, debugDisplay);
            }
            tectonics.SimStep();            
        }
    }

    public override void _Process(double delta)
    {
        if (tectonicTest && tectonics != null){
            tectonics.SimStep();
        }
    }



    float[,] GenerateTempMap(float scale){
        float[,] map = new float[worldSize.X, worldSize.Y];
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(octaves);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        noise.SetSeed(rng.Next(-99999, 99999));
        float averageTemp = 0;
        float[,] falloff = Falloff.GenerateFalloffMap(worldSize.X, worldSize.Y, false, 1, 1.1f);
        for (int x = 0; x < worldSize.X; x++){
            for (int y = 0; y < worldSize.Y; y++)
            {
                tempMapProgress += 1f;
                float noiseValue = Mathf.InverseLerp(-1, 1, noise.GetNoise(x / scale, y / scale));
                map[x, y] = Mathf.Lerp(1 - falloff[x, y], noiseValue, 0.15f);
                float heightFactor = (heightmap[x, y] - seaLevel - 0.2f) / (1f - seaLevel - 0.2f);
                if (heightFactor > 0)
                {
                    map[x, y] -= heightFactor;
                }
                map[x, y] = Mathf.Clamp(map[x, y], 0, 1);

                // converts to real value
                map[x, y] = minTemperature + Mathf.Pow(map[x, y], 0.7f) * (maxTemperature - minTemperature);
                //GD.Print(map[x, y].ToString("0.0") + " C");
                averageTemp += map[x, y];
            }
        }
        GD.Print((averageTemp/(worldSize.X*worldSize.Y)).ToString("Average: 0.0") + " C");
        return map;
    } 

    float[,] GenerateHumidMap(float scale){
        float[,] map = new float[worldSize.X, worldSize.Y];
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(octaves);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        noise.SetSeed(rng.Next(-99999, 99999));
        for (int y = 0; y < worldSize.Y; y++){
            for (int x = 0; x < worldSize.X; x++)
            {
                moistMapProgress += 1;
                map[x, y] = Mathf.InverseLerp(-0.8f, 1f, noise.GetNoise(x, y));

                // Turns moisture into real value
                map[x, y] = minRainfall + Mathf.Pow(map[x, y], 0.7f) * (maxRainfall - minRainfall);
            }
        }
        return map;
    }

    public void GenerateRivers()
    {
        List<Vector2I> sources = new List<Vector2I>();
        List<Vector2I> riverPositions = new List<Vector2I>();

        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                bool nearSource = false;
                if (rng.NextSingle() < 0.5f && heightmap[x, y] > 0.7f)
                {
                    foreach (Vector2I source in sources)
                    {
                        if (!nearSource)
                        {
                            if (new Vector2I(x, y).DistanceTo(source) <= 5f)
                            {
                                nearSource = true;
                            }
                        }
                    }
                    if (!nearSource)
                    {
                        sources.Add(new Vector2I(x,y));
                    }
                }
            }
        }
        GD.Print("Rivers: " + sources.Count);
        foreach (Vector2I source in sources)
        {
            List<Vector2I> currentRiver = new List<Vector2I>();
            PriorityQueue<Vector2I, float> frontier = new PriorityQueue<Vector2I, float>();
            frontier.Enqueue(source, 0);
            Dictionary<Vector2I, Vector2I> flow = new Dictionary<Vector2I, Vector2I>();
            flow[source] = new Vector2I(0, 0);
            Dictionary<Vector2I, float> flowCost = new Dictionary<Vector2I, float>();
            flowCost[source] = 0;

            uint attempts = 0;
            Vector2I riverEnd = new Vector2I(-1, -1);

            while (attempts < 10000 && frontier.Count > 0)
            {
                attempts++;
                Vector2I current = frontier.Dequeue();
                riverEnd = current;
                if (GoodRiverEnd(current))
                {
                    riverEnd = current;
                    break;
                }
                for (int dx = -1; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 2; dy++)
                    {
                        if ((dx != 0 && dy != 0) || (dx == 0 && dy == 0))
                        {
                            continue;
                        }
                        Vector2I next = new Vector2I(Mathf.PosMod(current.X + dx, worldSize.X), Mathf.PosMod(current.Y + dy, worldSize.Y));
                        float newCost = flowCost[current] + (1f - heightmap[next.X, next.Y]);
                        if ((!flowCost.ContainsKey(next) || newCost < flowCost[next]) && heightmap[next.X, next.Y] <= 0.95)
                        {
                            frontier.Enqueue(next, newCost);
                            flowCost[next] = newCost;
                            flow[next] = current;
                        }
                    }
                }
            }
            if (riverEnd != new Vector2I(-1, -1))
            {
                Vector2I pos = riverEnd;
                while (pos != source)
                {
                    currentRiver.Add(pos);
                    pos = flow[pos];
                }
            }
            if (currentRiver.Count > 1)
            {
                foreach (Vector2I pos in currentRiver)
                {
                    biomes[pos.X, pos.Y] = AssetManager.GetBiome("river");
                }                
            }

        }
    }

    bool GoodRiverEnd(Vector2I point) {
        Biome[] riverEndBiomes = [AssetManager.GetBiome("river"), AssetManager.GetBiome("ocean")];
        bool inLake = true;
        for (int dx = -1; dx < 2; dx++)
        {
            for (int dy = -1; dy < 2; dy++)
            {
                if (dx != 0 && dy != 0)
                {
                    continue;
                }
                Vector2I nPoint = new Vector2I(Mathf.PosMod(point.X + dx, worldSize.X), Mathf.PosMod(point.Y + dy, worldSize.Y));
                if (riverEndBiomes.Contains(biomes[nPoint.X, nPoint.Y]))
                {
                    return true;
                }
                if (heightmap[nPoint.X, nPoint.Y] <= heightmap[point.X, point.Y] + 0.05)
                {
                    inLake = false;
                }
            }           
        }
        return inLake;
    }
    public void GenerateWorld()
    {
        AssetManager.LoadMods();
        worldGenStage++;
        GD.Print("Heightmap Generation Started");
        ulong startTime = Time.GetTicksMsec();
        heightmap = new Tectonics().RunSimulation(this, 30);
        GD.Print("Heightmap Generation Finished After " + (Time.GetTicksMsec() - startTime) + "ms");

        worldGenStage++;
        GD.Print("Temperature Generation Started");
        startTime = Time.GetTicksMsec();
        tempmap = GenerateTempMap(mapScale / 4);
        GD.Print("Temperature Generation Finished After " + (Time.GetTicksMsec() - startTime) + "ms");

        worldGenStage++;
        GD.Print("Humidity Generation Started");
        startTime = Time.GetTicksMsec();
        humidmap = GenerateHumidMap(mapScale / 2);
        GD.Print("Humidity Generation Finished After " + (Time.GetTicksMsec() - startTime) + "ms");

        worldGenStage++;
        biomes = new Biome[worldSize.X, worldSize.Y];
        GenerateBiomes();
        worldGenStage++;
        GD.Print("River Generation Started");
        startTime = Time.GetTicksMsec();
        GenerateRivers();
        GD.Print("River Generation Finished After " + (Time.GetTicksMsec() - startTime) + "ms");
    }

    public void GenerateBiomes()
    {
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                preparationProgress++;
                Biome selectedBiome = AssetManager.GetBiome("ocean");
                biomes[x, y] = selectedBiome;
                float temp = tempmap[x, y];
                float elevation = heightmap[x, y];
                float moist = humidmap[x, y];

                foreach (Biome biome in AssetManager.biomes.Values)
                {
                    bool tempInRange = temp >= biome.minTemperature && temp <= biome.maxTemperature;
                    bool heightInRange = elevation >= biome.minElevation && elevation <= biome.maxElevation;
                    bool moistInRange = moist >= biome.minMoisture && moist <= biome.maxMoisture;

                    if (tempInRange && moistInRange && !biome.special && elevation >= seaLevel)
                    {
                        selectedBiome = biome;
                        tempInRange = biome.minTemperature >= temp && biome.maxTemperature <= temp;
                        heightInRange = biome.minElevation >= elevation && biome.maxElevation <= elevation;
                        moistInRange = biome.minMoisture >= moist && biome.maxMoisture <= moist;
                        biomes[x, y] = selectedBiome;
                        break;                        
                    }

                }
            }
        }        
    }

    public void ColorMap()
    {
        startedColoring = true;
        worldGenStage++;
        //GD.Print("Map Coloring Started");
        ulong startTime = Time.GetTicksMsec();

        terrainImage = Image.CreateEmpty(worldSize.X, worldSize.Y, true, Image.Format.Rgb8);
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                preparationProgress++;
                
                Biome biome = biomes[x, y];
                GD.Print(biome);
                if (biome.type != "water")
                {
                    tileMap.SetCell(new Vector2I(x, y), 0, new Vector2I(biome.textureX, biome.textureY));
                    // Plant Reliefs    
                    if (rng.NextSingle() <= biome.plantDensity * 0.2f)
                    {
                        // Grass
                        reliefMap.SetCell(new Vector2I(x, y), 0, new Vector2I(3, 2));
                    }
                    if (rng.NextSingle() <= biome.plantDensity * 0.2f && biome.plantDensity > 0.75f)
                    {
                        // Bushes
                        reliefMap.SetCell(new Vector2I(x, y), 0, new Vector2I(1, 1));
                    }
                    if (rng.NextSingle() <= biome.plantDensity * 0.25f)
                    {
                        // Biome Specific Plants
                        reliefMap.SetCell(new Vector2I(x, y), 0, new Vector2I(biome.textureX, biome.textureY));
                    }

                    // Height Reliefs
                    if (heightmap[x, y] >= hillThreshold)
                    {
                        reliefMap.SetCell(new Vector2I(x, y), 0, new Vector2I(3, 1));
                    }
                    if (heightmap[x, y] >= mountainThreshold)
                    {
                        reliefMap.SetCell(new Vector2I(x, y), 0, new Vector2I(0, 0));
                    }
                }
            }
        }
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                preparationProgress++;
                Biome biome = biomes[x, y];
                if (biome.type == "water")
                {
                    Color oceanColor = Color.FromString(AssetManager.GetBiome("shallow_ocean").color, new Color(1, 1, 1));
                    //terrainImage.SetPixel(x,y, oceanColor * Mathf.Lerp(0.6f, 1f, Mathf.InverseLerp(seaLevel - Tectonics.oceanDepth, seaLevel, heightmap[x,y])));
                    terrainImage.SetPixel(x, y, oceanColor);
                }
                else
                {
                    terrainImage.SetPixel(x, y, Color.FromString(biome.color, new Color(1, 1, 1)));
                }
            }
        }
        GD.Print("Map Coloring Finished After " + (Time.GetTicksMsec() - startTime) + "ms");
        worldCreated = true;
        EmitSignal(SignalName.worldgenFinished);
    }
}