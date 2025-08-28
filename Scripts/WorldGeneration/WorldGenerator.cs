using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using MessagePack;
using MessagePack.Resolvers;
public delegate void WorldgenFinished();
[MessagePackObject][Serializable]
public class WorldGenerator
{
    public const float HillThreshold = 0.76f;
    public const float MountainThreshold = 0.9f;
    public const float MaxTemperature = 35;
    public const float MinTemperature = -30;
    public const float MaxRainfall = 3500;
    public const float MinRainfall = 50;
    public const int WorldHeight = 10000;
    [Key(0)]
    public Vector2I WorldSize { get; set; } = new Vector2I(360, 180) ;
    [IgnoreMember]
    public float Width;
    [IgnoreMember]
    public float Height;
    [Key(1)]
    public float WorldMult { get; set; } = 3f;
    [Key(2)]
    public float SeaLevel { get; set; } = 0.6f;
    [Key(3)]
    public int Seed { get; set; } 
    [Key(4)]
    public int continents { get; set; } = 12;
    [IgnoreMember] public WorldgenFinished worldgenFinishedEvent;
    [Key(5)]
    public float[,] HeightMap { get; set; } 
    [Key(6)]
    public float[,] RainfallMap { get; set; } 
    [Key(7)]
    public float[,] TempMap { get; set; } 
    [Key(8)]
    public string[,] BiomeMap { get; set; } 
    public static Random rng;
    [IgnoreMember]
    public bool TempDone;
    [IgnoreMember]
    public bool RainfallDone;
    [IgnoreMember]
    public bool HeightmapDone;
    [IgnoreMember]
    public bool WaterDone;
    [IgnoreMember]
    public bool WorldExists = false;
    [IgnoreMember]
    public int Stage;

    public void GenerateWorld()
    {
        Init();
        Generate();
        WorldExists = true;
    }
    void InitAfterLoad()
    {
        WorldSize = new Vector2I(Mathf.RoundToInt(360 * WorldMult), Mathf.RoundToInt(180 * WorldMult));
        rng = new Random(Seed);
        WorldExists = true;
    }
    void Init()
    {
        AssetManager.LoadMods();
        Stage = 0;
        WorldExists = false;
        WorldSize = new Vector2I(Mathf.RoundToInt(360 * WorldMult), Mathf.RoundToInt(180 * WorldMult));
        rng = new Random(Seed);
    }
    void Generate()
    {
        GD.Print("Seed: " + Seed);
        ulong startTime = Time.GetTicksMsec();
        try
        {
            HeightMap = new HeightmapGenerator().GenerateHeightmap(this);
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
        
        GD.Print("Heightmap Generation Finished After " + ((Time.GetTicksMsec() - startTime) / 1000f).ToString("0.0s"));
        Stage++;
        TempMap = new TempmapGenerator().GenerateTempMap(1f, this);
        Stage++;
        RainfallMap = new RainfallMapGenerator().GenerateRainfallMap(2f, this);
        Stage++;
        //HydroMap = new HydrologyGenerator().GenerateHydrologyMap();
        RiverGenerator riverGenerator = new RiverGenerator()
        {
            attemptedRivers = 10000,
            minRiverDist = 3f,
            minRiverLength = 5,
            maxRiverLength = 500000,
            minRiverHeight = 0.7f,
            riverMustEndInWater = true
        };
        BiomeMap = new BiomeGenerator().GenerateBiomes(this);
        riverGenerator.RunRiverGeneration(this);
        //GD.Print("Worldgen Started");
        Stage++;
        // TODO: Add water flow simulations
    }
    public void FinishWorldgen()
    {
        worldgenFinishedEvent.Invoke();
    }
    public float GetUnitTemp(float value)
    {
        if (value < 0 || value > 1)
        {
            return float.NaN;
        }
        return MinTemperature + Mathf.Pow(value, 1f) * (MaxTemperature - MinTemperature);
    }
    public float GetUnitRainfall(float value)
    {
        if (value < 0 || value > 1)
        {
            return float.NaN;
        }
        return MinRainfall + Mathf.Pow(value, 1f) * (MaxRainfall - MinRainfall);
    }
    public float GetUnitElevation(float value)
    {
        float seaElevation = WorldHeight * SeaLevel;
        return (value * WorldHeight) - seaElevation;
    }
    public void SaveTerrainToFile(string path)
    {
        //GD.Print(JsonSerializer.Serialize(BiomeMap, options));
        //GD.Print("Thing");
        var resolver = CompositeResolver.Create(
            [new Vector2IFormatter(), new ColorFormatter(), new NodePathFormatter(), new GDStringNameFormatter()],
            [StandardResolver.Instance]
        );

        var moptions = MessagePackSerializerOptions.Standard.WithResolver(resolver).WithCompression(MessagePackCompression.Lz4Block);
        
        FileAccess save = FileAccess.Open($"{path}/terrain_data.pxsave", FileAccess.ModeFlags.Write);
        
        GD.Print(save.StoreBuffer(MessagePackSerializer.Typeless.Serialize(this, moptions)));
        //save.StoreLine(JsonSerializer.Serialize(this, options));
    }
    public static WorldGenerator LoadFromSave(string path)
    {
        if (DirAccess.Open(path) == null)
        {
            GD.PushError($"Save at path {path} not found");
            return null;
        }
        var resolver = CompositeResolver.Create(
            [new Vector2IFormatter(), new ColorFormatter(), new NodePathFormatter(), new GDStringNameFormatter()],
            [StandardResolver.Instance]
        );

        var moptions = MessagePackSerializerOptions.Standard.WithResolver(resolver).WithCompression(MessagePackCompression.Lz4Block);
       
        FileAccess save = FileAccess.Open($"{path}/terrain_data.pxsave", FileAccess.ModeFlags.Read);
        //GD.Print(save.GetAsText());
        WorldGenerator loaded = null;
        try
        {
            loaded = MessagePackSerializer.Deserialize<WorldGenerator>(save.GetBuffer((long)save.GetLength()), moptions);
            //loaded = JsonSerializer.Deserialize<WorldGenerator>(save.GetAsText(true), options);
            if (loaded != null)
            {
                loaded.InitAfterLoad();
            }
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
        return loaded;
    }
    public Image GetTerrainImage(bool heightmap = false)
    {
        if (!WorldExists)
        {
            return null;
        }
        Image image = Image.CreateEmpty(WorldSize.X, WorldSize.Y, false, Image.Format.Rgb8);
        for (int x = 0; x < WorldSize.X; x++)
        {
            for (int y = 0; y < WorldSize.Y; y++)
            {
                Color lowFlatColor = Color.Color8(31, 126, 52);
                Color lowHillColor = Color.Color8(198, 187, 114);
                Color highHillColor = Color.Color8(95, 42, 22);
                Color shallowWatersColor = Color.Color8(71, 149, 197);
                Color deepWatersColor = Color.Color8(27, 59, 111);
                if (heightmap)
                {
                    float hf = (HeightMap[x, y] - SeaLevel) / (1f - SeaLevel);
                    image.SetPixel(x, y, Utility.MultiColourLerp([lowFlatColor, lowHillColor, highHillColor], hf));

                    if (AssetManager.GetBiome(BiomeMap[x, y]).type == "water")
                    {
                        image.SetPixel(x, y, Utility.MultiColourLerp([shallowWatersColor, deepWatersColor], Mathf.Clamp(1f - HeightMap[x, y] / SeaLevel, 0f, 1f)));
                    }
                }
                else
                {
                    if (AssetManager.GetBiome(BiomeMap[x, y]).type == "water")
                    {
                        Color oceanColor = Color.Color8(71, 149, 197);
                        image.SetPixel(x, y, Utility.MultiColourLerp([shallowWatersColor, deepWatersColor], Mathf.Clamp(1f - HeightMap[x, y] / SeaLevel, 0f, 1f)));
                    }
                    //terrainImage.SetPixel(x, y, oceanColor);
                    else
                    {
                        Color biomeColor = Color.FromString(AssetManager.GetBiome(BiomeMap[x, y]).color, new Color(0, 0, 0));
                        /*
                        if (HeightMap[x, y] > HillThreshold)
                            biomeColor = new Color(0.5f, 0.5f, 0.5f);
                        if (HeightMap[x, y] > MountainThreshold)
                            biomeColor = new Color(0.2f, 0.2f, 0.2f);
                        */
                        image.SetPixel(x, y, biomeColor * HeightMap[x, y]);
                    }
                }
            }
        }
        return image;

    }
}