using Godot;
using System;
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
    public string[,] tileBiomes;
    public Biome[,] biomes;
    float[,] heightmap;
    float[,] tempmap;
    float[,] humidmap;

    float[] tempThresholds = [0.874f, 0.765f, 0.594f, 0.439f, 0.366f, 0.124f];
    float[] humidThresholds = [0.941f, 0.778f, 0.507f, 0.236f, 0.073f, 0.014f, 0.002f];
    Image terrainImage;
    public bool worldCreated;
    public int worldGenStage;
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

    public Vector2I worldSize = new Vector2I(360, 180);
    [Export] public float worldSizeMult = 4;
    public float seaLevel = 0.6f;

    [ExportCategory("Noise Settings")]
    [Export] public int seed;
    [Export] public float mapScale = 1.5f;
    [Export] public int octaves = 8;
    public List<Biome> loadedBiomes;
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

        float[,] falloff = Falloff.GenerateFalloffMap(worldSize.X, worldSize.Y, false, 1, 1.1f);
        for (int x = 0; x < worldSize.X; x++){
            for (int y = 0; y < worldSize.Y; y++){
                tempMapProgress += 1f;
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
        for (int y = 0; y < worldSize.Y; y++){
            float moisture = 0;
            for (int x = 0; x < worldSize.X; x++){
                moistMapProgress += 1;
                if (heightmap[x,y] < seaLevel){
                    moisture += 0.05f * tempmap[x,y];
                } else if ((heightmap[x,y] - seaLevel)/(1f - seaLevel) > 0.4f){
                    moisture -= (heightmap[x,y] - seaLevel)/(1f - seaLevel) - 0.4f;
                }
                
                moisture = Mathf.Clamp(moisture, 0f, 1f);
                map[x,y] = Mathf.InverseLerp(-0.2f, 0.5f, noise.GetNoise(x,y));
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
                    tileBiomes[pos.X, pos.Y] = "river";
                }                
            }

        }
    }

    bool GoodRiverEnd(Vector2I point) {
        string[] riverEndBiomes = ["shallow ocean", "river", "ocean"];
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
                if (riverEndBiomes.Contains(tileBiomes[nPoint.X, nPoint.Y]))
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
        worldGenStage++;
        GD.Print("Heightmap Generation Started");
        ulong startTime = Time.GetTicksMsec();
        heightmap = new Tectonics().RunSimulation(this, 20);
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
        GD.Print("Biome Generation Started");
        startTime = Time.GetTicksMsec();
        // Biome generation
        tileBiomes = new string[worldSize.X, worldSize.Y];
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                tileBiomes[x, y] = GetEnvironment(x, y);
            }
        }
        GD.Print("Biome Generation Finished After " + (Time.GetTicksMsec() - startTime) + "ms");
        worldGenStage++;
        GD.Print("River Generation Started");
        startTime = Time.GetTicksMsec();
        GenerateRivers();
        GD.Print("River Generation Finished After " + (Time.GetTicksMsec() - startTime) + "ms");

    }

    public void ColorMap(){
        worldGenStage++;
        GD.Print("Map Coloring Started");
        ulong startTime = Time.GetTicksMsec();
        // Map coloring
        loadedBiomes = LoadBiomes();
        biomes = new Biome[worldSize.X, worldSize.Y]; 

        terrainImage = Image.CreateEmpty(worldSize.X, worldSize.Y, true, Image.Format.Rgb8);
        for (int x = 0; x < worldSize.X; x++){
            for (int y = 0; y < worldSize.Y; y++){
                preparationProgress++;
                foreach (Biome biome in loadedBiomes){
                    if (biome.mergedIds.Contains(tileBiomes[x,y])){
                        tileMap.SetCell(new Vector2I(x,y), 0, new Vector2I(biome.textureX,biome.textureY));
                        if (tileBiomes[x, y] != "river")
                        {
                            if (biome.fertility >= 0.1f)
                            {
                                if (rng.NextSingle() <= biome.fertility * 0.2f)
                                {
                                    reliefMap.SetCell(new Vector2I(x, y), 0, new Vector2I(3, 2));
                                }
                                if (rng.NextSingle() <= biome.fertility * 0.2f && biome.fertility > 0.75f)
                                {
                                    reliefMap.SetCell(new Vector2I(x, y), 0, new Vector2I(1, 1));
                                }
                                if (rng.NextSingle() <= biome.fertility * 0.25f)
                                {
                                    reliefMap.SetCell(new Vector2I(x, y), 0, new Vector2I(biome.textureX, biome.textureY));
                                }
                            }

                            if (heightmap[x, y] >= 0.75f)
                            {
                                reliefMap.SetCell(new Vector2I(x, y), 0, new Vector2I(3, 1));
                            }
                            if (heightmap[x, y] >= 0.8f)
                            {
                                reliefMap.SetCell(new Vector2I(x, y), 0, new Vector2I(0, 0));
                            }                            
                        }

                        biomes[x,y] = biome;
                    }
                }
            }
        }
        for (int x = 0; x < worldSize.X; x++){
            for (int y = 0; y < worldSize.Y; y++){
                preparationProgress++;
                Biome biome = biomes[x,y];
                if (biome.terrainType == Biome.TerrainType.WATER){
                    Color oceanColor = Color.FromString(GetBiome("shallow ocean").color, new Color(1, 1, 1));
                    //terrainImage.SetPixel(x,y, oceanColor * Mathf.Lerp(0.6f, 1f, Mathf.InverseLerp(seaLevel - Tectonics.oceanDepth, seaLevel, heightmap[x,y])));
                    terrainImage.SetPixel(x,y, oceanColor);
                } else {
                    terrainImage.SetPixel(x,y, Color.FromString(biome.color, new Color(1, 1, 1)));
                }
            }
        }
        GD.Print("Map Coloring Finished After " + (Time.GetTicksMsec() - startTime) + "ms");
        worldCreated = true;
        EmitSignal(SignalName.worldgenFinished);                   
    }

    string GetEnvironment(int x, int y){
        float altitude = heightmap[x,y];
        string biome = "rock";

        // If we are below the ocean threshold
        if (altitude <= seaLevel){
            switch (getTempType(x, y)){
                case TempTypes.POLAR:
                    biome = "polar ice";
                    break;
                default:
                	biome = "shallow ocean";
				    if (altitude <= seaLevel - 0.1){
                        biome = "ocean";
                    }
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
        string biomesPath = @"Data/biomes.json";
        FileAccess bio = FileAccess.Open(biomesPath, FileAccess.ModeFlags.Read);
        if (bio != null){       
            string biomeData = bio.GetAsText();

            List<Biome> biomeList = JsonSerializer.Deserialize<List<Biome>>(biomeData);
            return biomeList;
        }
        GD.Print("Biomes.json not found at path '" + biomesPath + "'");  
        return null;
    }

    public Biome GetBiome(string id){
        foreach (Biome biome in loadedBiomes){
            if (biome.id == id){
                return biome;
            }
        }
        return null;
    }
}