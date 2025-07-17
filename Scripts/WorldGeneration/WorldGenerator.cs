using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Godot;
public static class WorldGenerator
{
    public const float HillThreshold = 0.8f;
    public const float MountainThreshold = 0.9f;
    public const float MaxTemperature = 35;
    public const float MinTemperature = -30;
    public const float MaxRainfall = 3500;
    public const float MinRainfall = 50;
    public const int WorldHeight = 10000;

    public static Vector2I WorldSize = new Vector2I(360, 180);
    public static float Width;
    public static float Height;
    public static float WorldMult = 2f;
    public static float SeaLevel = 0.6f;
    public static int Seed;
    public static EventHandler worldgenFinishedEvent;
    public static float[,] HeightMap;
    public static float[,] RainfallMap;
    public static float[,] TempMap;
    public static Biome[,] BiomeMap;
    public static string[,] Features; // for denoting special stuff such as oasises, waterfalls, ore veins, etc
    public static Dictionary<Vector2I, Vector2I> FlowDirMap;
    public static float[,] HydroMap;
    public static Random rng;
    public static bool TempDone;
    public static bool RainfallDone;
    public static bool HeightmapDone;
    public static bool WaterDone;
    public static bool WorldExists = false;
    public static int Stage;

    public static void GenerateWorld()
    {
        Init();
        Generate();
        WorldExists = true;
    }
    static void Init()
    {
        AssetManager.LoadMods();
        Stage = 0;
        WorldExists = false;
        WorldSize = new Vector2I(Mathf.RoundToInt(360 * WorldMult), Mathf.RoundToInt(180 * WorldMult));
        HydroMap = new float[WorldSize.X, WorldSize.Y];
        Features = new string[WorldSize.X, WorldSize.Y];
        rng = new Random(Seed);
    }
    static void Generate()
    {
        ulong startTime = Time.GetTicksMsec();
        HeightMap = new HeightmapGenerator().GenerateHeightmap();
        GD.Print("Heightmap Generation Finished After " + ((Time.GetTicksMsec() - startTime) / 1000f).ToString("0.0s"));
        Stage++;
        TempMap = new TempmapGenerator().GenerateTempMap(1f);
        Stage++;
        RainfallMap = new RainfallMapGenerator().GenerateRainfallMap(2f);
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
            BiomeMap = new BiomeGenerator().GenerateBiomes();
            riverGenerator.RunRiverGeneration();
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
        
        Stage++;
        // TODO: Add water flow simulations
    }
    public static void FinishWorldgen()
    {
        worldgenFinishedEvent.Invoke(null, EventArgs.Empty);
    }
    public static float GetUnitTemp(float value)
    {
        if (value < 0 || value > 1)
        {
            return float.NaN;
        }
        return MinTemperature + Mathf.Pow(value, 1f) * (MaxTemperature - MinTemperature);
    }
    public static float GetUnitRainfall(float value)
    {
        if (value < 0 || value > 1)
        {
            return float.NaN;
        }
        return MinRainfall + Mathf.Pow(value, 1f) * (MaxRainfall - MinRainfall);
    }
    public static float GetUnitElevation(float value)
    {
        float seaElevation = WorldHeight * SeaLevel;
        return (value * WorldHeight) - seaElevation;
    }

    public static Image GetTerrainImage(bool heightmap = false)
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

                    if (BiomeMap[x, y].type == "water")
                    {
                        image.SetPixel(x, y, Utility.MultiColourLerp([shallowWatersColor, deepWatersColor], Mathf.Clamp(1f - HeightMap[x, y]/SeaLevel, 0f, 1f)));
                    }
                }
                else
                {
                    if (BiomeMap[x, y].type == "water")
                    {
                        Color oceanColor = Color.Color8(71, 149, 197);
                        image.SetPixel(x, y, Utility.MultiColourLerp([shallowWatersColor, deepWatersColor], Mathf.Clamp(1f - HeightMap[x, y]/SeaLevel, 0f, 1f)));
                    }
                        //terrainImage.SetPixel(x, y, oceanColor);
                    else
                    {
                        Color biomeColor = Color.FromString(BiomeMap[x, y].color, new Color(0, 0, 0));
                        image.SetPixel(x, y, biomeColor * HeightMap[x,y]);
                    }
                }
            }
        }
        return image;

    }
}