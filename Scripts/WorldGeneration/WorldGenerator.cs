using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
[Serializable]
public class WorldGenerator
{
    public const float HillThreshold = 0.76f;
    public const float MountainThreshold = 0.9f;
    public const float MaxTemperature = 35;
    public const float MinTemperature = -30;
    public const float MaxRainfall = 3500;
    public const float MinRainfall = 50;
    public const int WorldHeight = 10000;

    public Vector2I WorldSize { get; set; } = new Vector2I(360, 180) ;
    public float Width;
    public float Height;
    public float WorldMult { get; set; } = 3f;
    public float SeaLevel { get; set; } = 0.6f;
    public int Seed { get; set; } 
    public int continents { get; set; } = 12;
    public static EventHandler worldgenFinishedEvent;
    public float[,] HeightMap { get; set; } 
    public float[,] RainfallMap { get; set; } 
    public float[,] TempMap { get; set; } 
    public string[,] BiomeMap { get; set; } 
    public string[,] Features { get; set; }  // for denoting special stuff such as oasises, waterfalls, ore veins, etc
    public float[,] HydroMap { get; set; } 
    public static Random rng;
    public bool TempDone;
    public bool RainfallDone;
    public bool HeightmapDone;
    public bool WaterDone;
    public bool WorldExists = false;
    public int Stage;

    public void GenerateWorld()
    {
        Init();
        Generate();
        WorldExists = true;

        try
        {
            SaveTerrainToFile("Save1");
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
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
        HydroMap = new float[WorldSize.X, WorldSize.Y];
        Features = new string[WorldSize.X, WorldSize.Y];
        rng = new Random(Seed);
    }
    void Generate()
    {
        ulong startTime = Time.GetTicksMsec();
        HeightMap = new HeightmapGenerator().GenerateHeightmap(this);
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
        try
        {
            BiomeMap = new BiomeGenerator().GenerateBiomes(this);
            riverGenerator.RunRiverGeneration(this);
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
        
        Stage++;
        // TODO: Add water flow simulations
    }
    public void FinishWorldgen()
    {
        worldgenFinishedEvent.Invoke(null, EventArgs.Empty);
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
    public void SaveTerrainToFile(string saveName)
    {
        JsonSerializerOptions options = new()
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        };
        options.Converters.Add(new TwoDimensionalArrayConverter<float>());
        options.Converters.Add(new TwoDimensionalArrayConverter<Biome>());
        options.Converters.Add(new TwoDimensionalArrayConverter<string>());
        //GD.Print(JsonSerializer.Serialize(BiomeMap, options));
        //GD.Print("Thing");
        if (DirAccess.Open("user://saves") == null)
        {
            DirAccess.MakeDirAbsolute("user://saves");
        }
        if (DirAccess.Open($"user://saves/{saveName}") == null) {
            DirAccess.MakeDirAbsolute($"user://saves/{saveName}");
        }
        DirAccess saveDir = DirAccess.Open($"user://saves/{saveName}");

        Godot.FileAccess save = Godot.FileAccess.Open($"user://saves/{saveName}/terrainData.pxsave", Godot.FileAccess.ModeFlags.Write);
        save.StoreLine(JsonSerializer.Serialize(this, options));
    }
    public static WorldGenerator LoadFromSave(string saveName)
    {
        AssetManager.LoadMods();
        if (DirAccess.Open($"user://saves/{saveName}") == null)
        {
            GD.PushError($"Save at path saves/{saveName} not found");
            return null;
        }
        JsonSerializerOptions options = new()
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,        
        };
        options.Converters.Add(new TwoDimensionalArrayConverter<float>());
        options.Converters.Add(new TwoDimensionalArrayConverter<Biome>());
        options.Converters.Add(new TwoDimensionalArrayConverter<string>());
       
        FileAccess save = FileAccess.Open($"user://saves/{saveName}/terrainData.pxsave", FileAccess.ModeFlags.Read);
        //GD.Print(save.GetAsText());
        WorldGenerator loaded = null;
        try
        {
            loaded = JsonSerializer.Deserialize<WorldGenerator>(save.GetAsText(true), options);
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