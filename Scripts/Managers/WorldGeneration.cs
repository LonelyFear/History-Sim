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
    public Dictionary<Vector2I, Vector2I> flowDirMap;
    public float[,] waterFlow;

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
    public const float maxRainfall = 3500;
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
                float heightFactor = (heightmap[x, y] - seaLevel) / (1f - seaLevel);
                if (heightFactor > 0)
                {
                    map[x, y] -= heightFactor * 0.3f;
                }
                map[x, y] = Mathf.Clamp(map[x, y], 0, 1);
                averageTemp += GetUnitTemp(map[x, y]);
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
        float minVal = float.PositiveInfinity;
        for (int y = 0; y < worldSize.Y; y++)
        {
            for (int x = 0; x < worldSize.X; x++)
            {
                moistMapProgress += 1;
                map[x, y] = Mathf.InverseLerp(-0.5f, 0.5f, noise.GetNoise(x/scale, y/scale));
                if (noise.GetNoise(x, y) < minVal)
                {
                    minVal = noise.GetNoise(x, y);
                }
            }
        }
        GD.Print("Min Rainfall Value: " + minVal);
        return map;
    }

    public static float GetUnitTemp(float value)
    {
        if (value < 0 || value > 1) {
            return float.NaN;
        }
        return minTemperature + Mathf.Pow(value, 0.7f) * (maxTemperature - minTemperature);
    }
    public static float GetUnitRainfall(float value)
    {
        if (value < 0 || value > 1) {
            return float.NaN;
        }
        return minRainfall + Mathf.Pow(value, 2f) * (maxRainfall - minRainfall);
    }
    public void CalculateFlowDirection()
    {
        flowDirMap = new Dictionary<Vector2I, Vector2I>();
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                Vector2I pos = new Vector2I(x, y);
                Vector2I flowDir = new Vector2I(-1, -1);
                float lowestElevation = heightmap[x, y] * 1.1f;
                for (int dx = -1; dx < 2; dx++)
                {
                    for (int dy = -1; dy < 2; dy++)
                    {
                        if ((dx != 0 && dy != 0) || (dx == 0 && dy == 0))
                        {
                            continue;
                        }
                        Vector2I next = new Vector2I(Mathf.PosMod(pos.X + dx, worldSize.X), Mathf.PosMod(pos.Y + dy, worldSize.Y));
                        if (heightmap[next.X, next.Y] <= lowestElevation)
                        {
                            lowestElevation = heightmap[next.X, next.Y];
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
        waterFlow = new float[worldSize.X, worldSize.Y];
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                if (heightmap[x, y] < 0.6 || humidmap[x, y] < 0.4f)
                {
                    continue;
                }
                waterFlow[x, y] += humidmap[x, y];
                Vector2I pos = new Vector2I(x, y);
                float attempts = 500;
                while (flowDirMap[pos] != new Vector2I(-1, -1) && heightmap[pos.X, pos.Y] >= seaLevel && attempts > 0)
                {
                    attempts--;
                    waterFlow[flowDirMap[pos].X, flowDirMap[pos].Y] += waterFlow[x, y];
                    pos = flowDirMap[pos];
                }
            }
        }
    }
    public void GenerateWorld()
    {
        AssetManager.LoadMods();
        worldGenStage++;
        GD.Print("Heightmap Generation Started");
        ulong startTime = Time.GetTicksMsec();
        try
        {
            heightmap = new Tectonics().GenerateHeightmap(this);
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
        
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
        //CalculateFlowDirection();
        //CalculateFlow();
        GenerateBiomes();
        worldGenStage++;
        GD.Print("River Generation Started");
        startTime = Time.GetTicksMsec();
        GD.Print("River Generation Finished After " + (Time.GetTicksMsec() - startTime) + "ms");
    }

    public void GenerateBiomes()
    {
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                preparationProgress++;
                Biome selectedBiome = AssetManager.GetBiome("ice_sheet");
                float temp = GetUnitTemp(tempmap[x, y]);
                float elevation = heightmap[x, y];
                float moist = GetUnitRainfall(humidmap[x, y]);
                Dictionary<Biome, float> candidates = new Dictionary<Biome, float>();

                foreach (Biome biome in AssetManager.biomes.Values)
                {
                    bool tempInRange = temp >= biome.minTemperature && temp <= biome.maxTemperature;
                    bool moistInRange = moist >= biome.minMoisture && moist <= biome.maxMoisture;

                    if (tempInRange && moistInRange && elevation >= seaLevel)
                    {
                        candidates.Add(biome, 0);
                    }
                    if (elevation < seaLevel)
                    {
                        selectedBiome = AssetManager.GetBiome("ocean");
                    }

                }
                float minTRange = float.PositiveInfinity;
                float minMRange = float.PositiveInfinity;
                if (candidates.Count > 0)
                {
                    for (int i = 0; i < 2; i++){
                        foreach (Biome biome in candidates.Keys)
                        {
                            if (minTRange > biome.maxTemperature - biome.minTemperature)
                            {
                                minTRange = biome.maxTemperature - biome.minTemperature;
                                selectedBiome = biome;
                            }
                            if (minMRange > biome.maxMoisture - biome.minMoisture)
                            {
                                minMRange = biome.maxMoisture - biome.minMoisture;
                                selectedBiome = biome;
                            }
                        }
                    }

                }
                //GD.Print(waterFlow[x, y]);
                /*
                if (waterFlow[x, y] > 7f && elevation >= seaLevel)
                {
                    //selectedBiome = AssetManager.GetBiome("river");
                }
                */
                biomes[x, y] = selectedBiome;
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
                    Color oceanColor = Color.Color8(71,149,197);
                    terrainImage.SetPixel(x, y, oceanColor);
                    //terrainImage.SetPixel(x, y, oceanColor);
                }
                else
                {
                    float hf = (heightmap[x, y] - seaLevel) / (1f - seaLevel);
                    terrainImage.SetPixel(x, y, Color.FromString(biome.color, new Color(hf, hf, hf)));
                    
                    Color lowFlatColor = Color.Color8(31,126,52);
                    Color lowHillColor = Color.Color8(198,187,114);
                    Color highHillColor = Color.Color8(95,42,22);
                    terrainImage.SetPixel(x, y, Utility.MultiColourLerp([lowFlatColor, lowHillColor, highHillColor], hf));
                }
                
            }
        }
        GD.Print("Map Coloring Finished After " + (Time.GetTicksMsec() - startTime) + "ms");
        worldCreated = true;
        EmitSignal(SignalName.worldgenFinished);
    }
}