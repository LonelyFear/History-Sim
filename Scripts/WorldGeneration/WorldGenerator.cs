using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using MessagePack;
using MessagePack.Resolvers;
public delegate void WorldgenFinished();
[MessagePackObject(AllowPrivate = true)]
[Serializable]
public class WorldGenerator
{
    public const float HillThreshold = 0.76f;
    public const float MountainThreshold = 0.9f;
    public const float MaxTemperature = 50;
    public const float MinTemperature = -50;
    public const float MaxRainfall = 3500;
    public const float MinRainfall = 50;
    public const int WorldHeight = 10000;
    [IgnoreMember] CompressedTexture2D earthHeightmap = GD.Load<CompressedTexture2D>("res://Sprites/earth_heightmap.jpg");
    [Key(0)]
    public Vector2I WorldSize { get; set; } = new Vector2I(360, 180) ;
    [IgnoreMember]
    public float Width;
    [IgnoreMember]
    public float Height;
    [Key(1)]
    public float WorldMult { get; set; } = 3f;
    [Key(2)]
    public float SeaLevel { get; set; } = 0.0001f;
    [Key(3)]
    public int Seed { get; set; } = 1;
    [Key(4)]
    public int continents { get; set; } = 6;
    [IgnoreMember] public WorldgenFinished worldgenFinishedEvent;
    [IgnoreMember] public TerrainTile[,] tiles;
    [Key(5)]
    public int[,] HeightMap { get; set; } 
    [Key(6)]
    public float[,] RainfallMap { get; set; } 
    [Key(7)]
    public float[,] TempMap { get; set; } 
    [Key(8)]
    public string[,] BiomeMap { get; set; } 
    [Key(9)]
    public Vector2[,] WindVelMap { get; set; } 
    [IgnoreMember] public float[,] ContMap { get; set; } 
    [IgnoreMember] public Random rng;
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
    public WorldGenStage Stage;

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
            HeightMap = new HeightmapGenerator().UseEarthHeightmap(this);
            //HeightMap = new HeightmapGenerator().GenerateHeightmap(this);
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
        
        GD.Print("Heightmap Generation Finished After " + ((Time.GetTicksMsec() - startTime) / 1000f).ToString("0.0s"));
        Stage = WorldGenStage.WIND;
        try
        {
            WindVelMap = new WindGenerator().GeneratePrevailingWinds(this);
        } catch (Exception e)
        {
            GD.PushError(e);
        }
        Stage = WorldGenStage.TEMPERATURE;
        TempMap = new TempmapGenerator().GenerateTempMap(1f, this, out float[,] c);
        ContMap = c;

        Stage = WorldGenStage.RAINFALL;
        try
        {
            RainfallMap = new RainfallMapGenerator().GenerateRainfallMap(2f, this, true);
        } catch (Exception e)
        {
            GD.PushError(e);
        }
        
        Stage = WorldGenStage.BIOMES;
        //HydroMap = new HydrologyGenerator().GenerateHydrologyMap();
        RiverGenerator riverGenerator = new RiverGenerator()
        {
            attemptedRivers = 10000,
            minRiverDist = 3f,
            minRiverLength = 5,
            maxRiverLength = 500000,
            minRiverHeight = 2000,
            riverMustEndInWater = true
        };

        try
        {
            BiomeMap = new BiomeGenerator().GenerateBiomes(this);
        } catch (Exception e)
        {
            GD.PushError(e);
        }
        Stage = WorldGenStage.RIVERS;
        riverGenerator.RunRiverGeneration(this);
        //GD.Print("Worldgen Started");
        Stage = WorldGenStage.FINISHING;
        // TODO: Add water flow simulations
    }
    public void FinishWorldgen()
    {
        worldgenFinishedEvent.Invoke();
    }
    /*
    public float GetUnitTemp(float value)
    {
        if (value < 0 || value > 1)
        {
            return float.NaN;
        }
        return MinTemperature + Mathf.Pow(value, 1f) * (MaxTemperature - MinTemperature);
    }
    */
    /*
    public float GetUnitRainfall(float value)
    {
        if (value < 0 || value > 1)
        {
            return float.NaN;
        }
        return MinRainfall + Mathf.Pow(value, 1f) * (MaxRainfall - MinRainfall);
    }
    */
    /*
    public float GetUnitElevation(float value)
    {
        float seaElevation = WorldHeight * SeaLevel;
        return (value * WorldHeight) - seaElevation;
    }
    */
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
    public Image GetTerrainImage(TerrainMapMode mapMode)
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
                //streamLine.Position = 
                //streamLine.Rotation = Mathf.Atan2(WindVelMap[x,y].Y, WindVelMap[x,y].X);

                Color lowFlatColor = Color.Color8(31, 126, 52);
                Color lowHillColor = Color.Color8(198, 187, 114);
                Color highHillColor = Color.Color8(95, 42, 22);
                Color shallowWatersColor = Color.Color8(71, 149, 197);
                Color deepWatersColor = Color.Color8(27, 59, 111);
                float hf = (HeightMap[x, y] - SeaLevel) / (1f - SeaLevel);
                bool isWater = AssetManager.GetBiome(BiomeMap[x, y]).type == "water";
                bool isIce = AssetManager.GetBiome(BiomeMap[x, y]).type == "water";
                switch (mapMode)
                {
                    case TerrainMapMode.HEIGHTMAP:
                        image.SetPixel(x, y, Utility.MultiColourLerp([lowFlatColor, lowHillColor, highHillColor], hf));
                        if (AssetManager.GetBiome(BiomeMap[x, y]).type == "water")
                        {
                            image.SetPixel(x, y, Utility.MultiColourLerp([shallowWatersColor, deepWatersColor], Mathf.Clamp(1f - HeightMap[x, y] / SeaLevel, 0f, 1f)));
                        }      
                        if (AssetManager.GetBiome(BiomeMap[x, y]).type == "ice")
                        {
                            image.SetPixel(x, y, Color.FromHtml(AssetManager.GetBiome(BiomeMap[x, y]).color));
                        }
                        break;  
                    case TerrainMapMode.HEIGHTMAP_REALISTIC:
                        if (AssetManager.GetBiome(BiomeMap[x, y]).type == "water")
                        {
                            image.SetPixel(x, y, Utility.MultiColourLerp([shallowWatersColor, deepWatersColor], Mathf.Clamp(1f - HeightMap[x, y] / SeaLevel, 0f, 1f)));
                        }
                        //terrainImage.SetPixel(x, y, oceanColor);
                        else
                        {
                            Color biomeColor = Color.FromString(AssetManager.GetBiome(BiomeMap[x, y]).color, new Color(0, 0, 0));
                            image.SetPixel(x, y, biomeColor * HeightMap[x, y]);
                        }                        
                        break;     
                    case TerrainMapMode.REALISTIC:
                        int sampleElevation = HeightMap[Mathf.PosMod(x + 1, WorldSize.X), Mathf.PosMod(y + 1, WorldSize.Y)];
                        float slope = (sampleElevation - HeightMap[x, y])/(float)WorldHeight;

                        Color finalColor = Color.FromHtml(AssetManager.GetBiome(BiomeMap[x, y]).color);
                        if (isWater)
                        {
                            finalColor = Utility.MultiColourLerp([shallowWatersColor, deepWatersColor], Mathf.Clamp(1f - HeightMap[x, y] / SeaLevel, 0f, 1f));
                        }      
                                
                        if (slope > 0)
                        {
                            // In light
                            image.SetPixel(x, y, Utility.MultiColourLerp([finalColor, new Color(1, 1, 1)], Math.Abs(slope) * 1.5f));
                        } else
                        {
                            if (isWater)
                            {
                                // In Shadow
                                image.SetPixel(x, y, Utility.MultiColourLerp([finalColor, new Color(0, 0, 0)], Math.Abs(slope) * 1.5f));                                
                            }
                            else
                            {
                                image.SetPixel(x, y, Utility.MultiColourLerp([finalColor, new Color(0, 0, 0)], Math.Abs(slope) * 5f));  
                            }
                        }  
                        break;    
                    case TerrainMapMode.DEBUG_PLATES:
                        Color pressureColor = Utility.MultiColourLerp([new Color(0,0,1), new Color(0,0,0,0), new Color(1,0,0)], Mathf.InverseLerp(-1, 1, tiles[x, y].pressure));
                        Color baseColor = Utility.MultiColourLerp([lowFlatColor, lowHillColor, highHillColor], hf);
                        if (AssetManager.GetBiome(BiomeMap[x, y]).type == "water")
                        {
                            baseColor = Utility.MultiColourLerp([shallowWatersColor, deepWatersColor], Mathf.Clamp(1f - HeightMap[x, y] / SeaLevel, 0f, 1f));
                        }
                        image.SetPixel(x, y, Utility.MultiColourLerp([pressureColor, baseColor], 0.5f));
                        break;     
                    case TerrainMapMode.DEBUG_COAST:
                        pressureColor = Utility.MultiColourLerp([new Color(0,0,1), new Color(0,0,0,0)], Mathf.Clamp(tiles[x, y].coastDist / 10f, 0, 1));
                        baseColor = Utility.MultiColourLerp([lowFlatColor, lowHillColor, highHillColor], hf);
                        if (AssetManager.GetBiome(BiomeMap[x, y]).type == "water")
                        {
                            baseColor = Utility.MultiColourLerp([shallowWatersColor, deepWatersColor], Mathf.Clamp(1f - HeightMap[x, y] / SeaLevel, 0f, 1f));
                        }
                        image.SetPixel(x, y, Utility.MultiColourLerp([pressureColor, baseColor], 0.5f));
                        break;  
                    case TerrainMapMode.DEBUG_RAINFALL:
                        pressureColor = Utility.MultiColourLerp([new Color(0,0,0), new Color(0,0,1), new Color(1,1,0)], RainfallMap[x,y]/3500f);
                        baseColor = Utility.MultiColourLerp([lowFlatColor, lowHillColor, highHillColor], hf);
                        if (AssetManager.GetBiome(BiomeMap[x, y]).type == "water")
                        {
                            baseColor = Utility.MultiColourLerp([shallowWatersColor, deepWatersColor], Mathf.Clamp(1f - HeightMap[x, y] / SeaLevel, 0f, 1f));
                        }
                        image.SetPixel(x, y, Utility.MultiColourLerp([pressureColor, baseColor], 0.5f));
                        break;      
                    case TerrainMapMode.DEBUG_WIND:
                        pressureColor = Utility.MultiColourLerp([new Color(0,0,0), new Color(1,0,0)], WindVelMap[x,y].Length()/8.6f);
                        baseColor = Utility.MultiColourLerp([lowFlatColor, lowHillColor, highHillColor], hf);
                        if (AssetManager.GetBiome(BiomeMap[x, y]).type == "water")
                        {
                            baseColor = Utility.MultiColourLerp([shallowWatersColor, deepWatersColor], Mathf.Clamp(1f - HeightMap[x, y] / SeaLevel, 0f, 1f));
                        }
                        image.SetPixel(x, y, Utility.MultiColourLerp([pressureColor, baseColor], 0.5f));
                        break;   
                    case TerrainMapMode.DEBUG_TEMP:
                        pressureColor = Utility.MultiColourLerp([new Color(0,0,1), new Color(1,1,1), new Color(1,0,0)], Mathf.InverseLerp(-40, 40, TempMap[x,y]));
                        baseColor = Utility.MultiColourLerp([lowFlatColor, lowHillColor, highHillColor], hf);
                        if (AssetManager.GetBiome(BiomeMap[x, y]).type == "water")
                        {
                            baseColor = Utility.MultiColourLerp([shallowWatersColor, deepWatersColor], Mathf.Clamp(1f - HeightMap[x, y] / SeaLevel, 0f, 1f));
                        }
                        image.SetPixel(x, y, Utility.MultiColourLerp([pressureColor, baseColor], 0.5f));
                        break;        
                    case TerrainMapMode.DEBUG_CONTINENTIALITY:
                        pressureColor = Utility.MultiColourLerp([new Color(0,0,1), new Color(1,1,0)], ContMap[x,y]);
                        baseColor = Utility.MultiColourLerp([lowFlatColor, lowHillColor, highHillColor], hf);
                        if (AssetManager.GetBiome(BiomeMap[x, y]).type == "water")
                        {
                            baseColor = Utility.MultiColourLerp([shallowWatersColor, deepWatersColor], Mathf.Clamp(1f - HeightMap[x, y] / SeaLevel, 0f, 1f));
                        }
                        image.SetPixel(x, y, Utility.MultiColourLerp([pressureColor, baseColor], 0.5f));
                        break;   
                }
            }
        }
        return image;
    }
}
public enum WorldGenStage
{
    CONTINENTS,
    MEASURING,
    TECTONICS,
    EROSION,
    WIND,
    TEMPERATURE,
    RAINFALL,
    BIOMES,
    RIVERS,
    FINISHING
}
public enum TerrainMapMode{
    HEIGHTMAP,
    HEIGHTMAP_REALISTIC,
    REALISTIC,
    DEBUG_PLATES,
    DEBUG_COAST,
    DEBUG_RAINFALL,
    DEBUG_WIND,
    DEBUG_TEMP,
    DEBUG_CONTINENTIALITY
}