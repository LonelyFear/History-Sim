using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

public partial class WorldGeneration : Node2D
{
    TileMapLayer tileMap;
    [Export] Node biomeLoader;
    public String[,] tileBiomes;
    public Biome[,] biomes;
    float[,] heightmap;
    float[,] tempmap;
    float[,] humidmap;

    float[] tempThresholds = [0.874f, 0.765f, 0.594f, 0.439f, 0.366f, 0.124f];
    float[] humidThresholds = [0.941f, 0.778f, 0.507f, 0.236f, 0.073f, 0.014f, 0.002f];
    Image terrainImage;
    public bool worldCreated;

    enum TempTypes {
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

    [Signal]
    public delegate void worldgenFinishedEventHandler();

    public Vector2I worldSize = new Vector2I(1440, 720);
    [Export(PropertyHint.Range, "0, 1, 0.01")]
    public float seaLevel = 0.6f;

    [ExportCategory("Noise Settings")]
    [Export] public int seed;
    [Export] public float mapScale = 1f;
    [Export] public int octaves = 8;
    public List<Biome> loadedBiomes;
    Random rng;
    //[ExportCategory("Rivers Settings")]

    public override void _Ready(){
        rng = new Random(seed);
        tileMap = (TileMapLayer)GetNode("Terrain Map");
        tileMap.Scale = new Vector2(1,1) * 16f/tileMap.TileSet.TileSize.X;
        GenerateWorld();
    }

    float[,] GenerateTempMap(float scale){
        float[,] map = new float[worldSize.X, worldSize.Y];
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(octaves);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        noise.SetSeed(rng.Next(-99999, 99999));

        float[,] falloff = Falloff.GenerateFalloffMap(worldSize.X, worldSize.Y, false, 1.1f);
        for (int x = 0; x < worldSize.X; x++){
            for (int y = 0; y < worldSize.Y; y++){
                float noiseValue = Mathf.InverseLerp(-1, 1, noise.GetNoise(x / scale, y / scale));
                map[x, y] = Mathf.Lerp(1 - falloff[x,y], noiseValue, 0.15f);
                float heightFactor = (heightmap[x,y] - seaLevel - 0.2f)/(1f - seaLevel - 0.2f);
                if (heightFactor > 0){
                    map[x, y] -= heightFactor;
                }
                map[x,y] = Mathf.Clamp(map[x,y], 0, 1);
            }
        }
        return map;
    } 

    float[,] GenerateHumidMap(float scale){
        float[,] map = new float[worldSize.X, worldSize.Y];
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(octaves);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        noise.SetSeed(rng.Next(-99999, 99999));
        for (int x = 0; x < worldSize.X; x++){
            for (int y = 0; y < worldSize.Y; y++){
                float noiseValue = Mathf.InverseLerp(-0.7f, 0.7f, noise.GetNoise(x / scale, y / scale));
                map[x, y] = noiseValue;
            }
        }
        return map;
    }  

    void GenerateWorld(){
        GD.Print("Heightmap Generation Started");
        ulong startTime = Time.GetTicksMsec();
        //heightmap = new Tectonics().RunSimulation(this, 16);
        heightmap = new float[worldSize.X, worldSize.Y];
        GD.Print("Heightmap Generation Finished After " + (Time.GetTicksMsec() - startTime) + "ms");

        GD.Print("Temperature Generation Started");
        startTime = Time.GetTicksMsec();
        tempmap = GenerateTempMap(mapScale/4);
        GD.Print("Temperature Generation Finished After " + (Time.GetTicksMsec() - startTime) + "ms");

        GD.Print("Humidity Generation Started");
        startTime = Time.GetTicksMsec();
        humidmap = GenerateHumidMap(mapScale/2);
        GD.Print("Humidity Generation Finished After " + (Time.GetTicksMsec() - startTime) + "ms");

        GD.Print("Biome Generation Started");
        startTime = Time.GetTicksMsec();
        // Biome generation
        tileBiomes = new string[worldSize.X,worldSize.Y];
        for (int x = 0; x < worldSize.X; x++){
            for (int y = 0; y < worldSize.Y; y++){
                tileBiomes[x,y] = GetBiome(x,y);
            }
        }
        GD.Print("Biome Generation Finished After " + (Time.GetTicksMsec() - startTime) + "ms");

        GD.Print("Map Coloring Started");
        startTime = Time.GetTicksMsec();
        // Map coloring
        loadedBiomes = LoadBiomes();
        biomes = new Biome[worldSize.X, worldSize.Y]; 
        terrainImage = Image.CreateEmpty(worldSize.X, worldSize.Y, true, Image.Format.Rgba8);
        for (int x = 0; x < worldSize.X; x++){
            for (int y = 0; y < worldSize.Y; y++){
                foreach (Biome biome in loadedBiomes){
                    if (biome.mergedIds.Contains(tileBiomes[x,y])){
                        tileMap.SetCell(new Vector2I(x,y), 0, biome.texturePos);
                        terrainImage.SetPixel(x,y, Color.FromString(biome.color, new Color(1, 1, 1)));
                        biomes[x,y] = biome;
                    }
                }
            }
        }
        GD.Print("Map Coloring Finished After " + (Time.GetTicksMsec() - startTime) + "ms");
        worldCreated = true;
        EmitSignal(SignalName.worldgenFinished);
    }

    string GetBiome(int x, int y){
        float altitude = heightmap[x,y];
        string biome = "rock";

        // If we are below the ocean threshold
        if (altitude <= seaLevel){
            switch (getTempType(x, y)){
                case TempTypes.POLAR:
                    biome = "polar ice";
                    break;
                default:
                    biome = "ocean";
                    break;
            }
        } else {
            switch (getTempType(x, y)){
                case TempTypes.POLAR:
                    switch (getHumidType(x, y)){
                        case HumidTypes.SUPER_ARID:
                            biome = "polar desert";
                            break;
                        default:
                            biome = "polar ice";
                            break;
                    }
                    break;
                case TempTypes.ALPINE:
                    switch (getHumidType(x, y)){
                        case HumidTypes.SUPER_ARID:
                            biome = "subpolar dry tundra";
                            break;
                        case HumidTypes.PER_ARID:
                            biome = "subpolar moist tundra";
                            break;
                        case HumidTypes.ARID:
                            biome = "subpolar wet tundra";
                            break;
                        default:
                            biome = "subpolar rain tundra";
                            break;
                    }
                    break;
                case TempTypes.BOREAL:
                    switch (getHumidType(x, y)){
                        case HumidTypes.SUPER_ARID:
                            biome = "boreal desert";
                            break;
                        case HumidTypes.PER_ARID:
                            biome = "boreal dry scrub";
                            break;
                        case HumidTypes.ARID:
                            biome = "boreal moist forest";
                            break;
                        case HumidTypes.SEMI_ARID:
                            biome = "boreal wet forest";
                            break;
                        default:
                            biome = "boreal rain forest";
                            break;
                    }
                    break;
                case TempTypes.COOL:
                    switch (getHumidType(x, y)){
                        case HumidTypes.SUPER_ARID:
                            biome = "cool temperate desert";
                            break;
                        case HumidTypes.PER_ARID:
                            biome = "cool temperate desert scrub";
                            break;
                        case HumidTypes.ARID:
                            biome = "cool temperate steppe";
                            break;
                        case HumidTypes.SEMI_ARID:
                            biome = "cool temperate moist forest";
                            break;
                        case HumidTypes.SUB_HUMID:
                            biome = "cool temperate wet forest";
                            break;
                        default:
                            biome = "cool temperate rain forest";
                            break;
                    }
                    break;
                case TempTypes.WARM:
                    switch (getHumidType(x, y)){
                        case HumidTypes.SUPER_ARID:
                            biome = "warm temperate desert";
                            break;
                        case HumidTypes.PER_ARID:
                            biome = "warm temperate desert scrub";
                            break;
                        case HumidTypes.ARID:
                            biome = "warm temperate thorn scrub";
                            break;
                        case HumidTypes.SEMI_ARID:
                            biome = "warm temperate dry forest";
                            break;
                        case HumidTypes.SUB_HUMID:
                            biome = "warm temperate moist forest";
                            break;
                        case HumidTypes.HUMID:
                            biome = "warm temperate wet forest";
                            break;
                        default:
                            biome = "warm temperate rain forest";
                            break;
                    }
                    break;
                case TempTypes.SUBTROPICAL:
                    switch (getHumidType(x, y)){
                        case HumidTypes.SUPER_ARID:
                            biome = "subtropical desert";
                            break;
                        case HumidTypes.PER_ARID:
                            biome = "subtropical desert scrub";
                            break;
                        case HumidTypes.ARID:
                            biome = "subtropical thorn woodland";
                            break;
                        case HumidTypes.SEMI_ARID:
                            biome = "subtropical dry forest";
                            break;
                        case HumidTypes.SUB_HUMID:
                            biome = "subtropical moist forest";
                            break;
                        case HumidTypes.HUMID:
                            biome = "subtropical wet forest";
                            break;
                        default:
                            biome = "subtropical rain forest";
                            break;
                    }
                    break;
                case TempTypes.TROPICAL:
                    switch (getHumidType(x, y)){
                        case HumidTypes.SUPER_ARID:
                            biome = "tropical desert";
                            break;
                        case HumidTypes.PER_ARID:
                            biome = "tropical desert scrub";
                            break;
                        case HumidTypes.ARID:
                            biome = "tropical thorn woodland";
                            break;
                        case HumidTypes.SEMI_ARID:
                            biome = "tropical very dry forest";
                            break;
                        case HumidTypes.SUB_HUMID:
                            biome = "tropical dry forest";
                            break;
                        case HumidTypes.HUMID:
                            biome = "tropical moist forest";
                            break;
                        case HumidTypes.PER_HUMID:
                            biome = "tropical wet forest";
                            break;
                        default:
                            biome = "tropical rain forest";
                            break;
                    }
                    break;
                default:
                    biome = "rock";
                    break;
            }
        }
        return biome;
    }
    TempTypes getTempType(int x, int y){
        float temp = tempmap[x,y];
        if (temp < tempThresholds[5]){
            return TempTypes.POLAR;
        } else if (temp >= tempThresholds[5] && temp < tempThresholds[4]){
            return TempTypes.ALPINE;
        } else if (temp >= tempThresholds[4] && temp < tempThresholds[3]){
            return TempTypes.BOREAL;
        } else if (temp >= tempThresholds[3] && temp < tempThresholds[2]){
            return TempTypes.COOL;
        } else if (temp >= tempThresholds[2] && temp < tempThresholds[1]){
            return TempTypes.WARM;
        } else if (temp >= tempThresholds[1] && temp < tempThresholds[0]){
            return TempTypes.SUBTROPICAL;
        } else if (temp >= tempThresholds[0]){
            return TempTypes.TROPICAL;
        } else {
            return TempTypes.INVALID;
        }
    }

    HumidTypes getHumidType(int x, int y){
        float humid = humidmap[x,y];
        //humid = 0;
        if ( humid < humidThresholds[6]){
            return  HumidTypes.SUPER_ARID;
        } else if (humid >= humidThresholds[6] && humid < humidThresholds[5]){
            return HumidTypes.PER_ARID;
        } else if (humid >= humidThresholds[5] && humid < humidThresholds[4]){
            return HumidTypes.ARID;
        } else if (humid >= humidThresholds[4] && humid < humidThresholds[3]){
            return HumidTypes.SEMI_ARID;
        } else if (humid >= humidThresholds[3] && humid < humidThresholds[2]){
            return HumidTypes.SUB_HUMID;
        } else if (humid >= humidThresholds[2] && humid < humidThresholds[1]){
            return HumidTypes.HUMID;
        } else if (humid >= humidThresholds[1] && humid < humidThresholds[0]){
            return HumidTypes.PER_HUMID; 
        } else if (humid >= humidThresholds[0]){
            return HumidTypes.SUPER_HUMID;
        } else {
            return HumidTypes.INVALID;
        }
    }

    List<Biome> LoadBiomes(){
        string biomesPath = "Json Resources/biomes.json";
        if (File.Exists(biomesPath)){
            StreamReader reader = new StreamReader(biomesPath);
            string biomeData = reader.ReadToEnd();
            return JsonSerializer.Deserialize<List<Biome>>(biomeData);
        }
        //GD.Print("Biomes.json not found at path '" + biomesPath + "'");  
        return null;
    }
}
