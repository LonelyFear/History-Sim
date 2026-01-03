using System;
using Vector2 = System.Numerics.Vector2;
using Godot;
using MessagePack;
using MessagePack.Resolvers;
public delegate void WorldgenFinished();
[MessagePackObject(AllowPrivate = true)]
[Serializable]
public class WorldGenerator
{
    public const int HillThreshold = 800;
    public const int MountainThreshold = 2000;
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
    public int continents { get; set; } = 8;
    [IgnoreMember] public WorldgenFinished worldgenFinishedEvent;
    [IgnoreMember] public TerrainTile[,] tiles;
    [Key(5)] public Cell[,] cells;
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
    [IgnoreMember] public bool generateRandomMap;
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
        cells = new Cell[WorldSize.X, WorldSize.Y];
        for (int x = 0; x < WorldSize.X; x++)
        {
            for (int y = 0; y < WorldSize.Y; y++)
            {
                cells[x,y] = new Cell();
            }
        }
        ulong startTime = Time.GetTicksMsec();
        try
        {
            if (generateRandomMap)
            {
                SeaLevel = 0.6f;
                new HeightmapGenerator().GenerateHeightmap(this);                
            } else
            {
                new HeightmapGenerator().UseEarthHeightmap(this); 
            }
        }
        catch (Exception e)
        {
            GD.PushError(e);
        }
        
        GD.Print("Heightmap Generation Finished After " + ((Time.GetTicksMsec() - startTime) / 1000f).ToString("0.0s"));
        Stage = WorldGenStage.WIND;
        try
        {
            new WindGenerator().GeneratePrevailingWinds(this);
        } catch (Exception e)
        {
            GD.PushError(e);
        }
        Stage = WorldGenStage.TEMPERATURE;
        new TempmapGenerator().GenerateTempMap(this);

        Stage = WorldGenStage.SUMMER_RAINFALL;
        try
        {
            new RainfallMapGenerator().GenerateRainfallMap(this);
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
            new BiomeGenerator().GenerateBiomes(this, true);
        } catch (Exception e)
        {
            GD.PushError(e);
        }
        KoppenClassification.GetKoppenMap(this);
        Stage = WorldGenStage.RIVERS;
        //riverGenerator.RunRiverGeneration(this);
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
        float seaLevelElevation = SeaLevel * WorldHeight;
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

                float seaFloorDepth = -WorldHeight * SeaLevel;
                Color waterColor = Utility.MultiColourLerp([shallowWatersColor, deepWatersColor], Mathf.Clamp(cells[x, y].elevation/seaFloorDepth, 0f, 1f));

                float hf = cells[x, y].elevation/(WorldHeight * (1f - SeaLevel));
                bool isWater = AssetManager.GetBiome(cells[x, y].biomeId).type == "water";
                bool isIce = AssetManager.GetBiome(cells[x, y].biomeId).type == "ice";
                
                switch (mapMode)
                {
                    case TerrainMapMode.HEIGHTMAP:
                        image.SetPixel(x, y, Utility.MultiColourLerp([lowFlatColor, lowHillColor, highHillColor], hf));
                        if (AssetManager.GetBiome(cells[x, y].biomeId).type == "water")
                        {
                            image.SetPixel(x, y, waterColor);
                        }      
                        if (AssetManager.GetBiome(cells[x, y].biomeId).type == "ice")
                        {
                            image.SetPixel(x, y, Color.FromHtml(AssetManager.GetBiome(cells[x, y].biomeId).color));
                        }
                        break;  
                    case TerrainMapMode.HEIGHTMAP_REALISTIC:
                        if (AssetManager.GetBiome(cells[x, y].biomeId).type == "water")
                        {
                            image.SetPixel(x, y, waterColor);
                        }
                        //terrainImage.SetPixel(x, y, oceanColor);
                        else
                        {
                            Color biomeColor = Color.FromString(AssetManager.GetBiome(cells[x, y].biomeId).color, new Color(0, 0, 0));
                            image.SetPixel(x, y, biomeColor * cells[x, y].elevation);
                        }                        
                        break;     
                    case TerrainMapMode.REALISTIC:
                        int sampleElevation = cells[Mathf.PosMod(x + 1, WorldSize.X), Mathf.PosMod(y + 1, WorldSize.Y)].elevation;
                        float slope = (sampleElevation - cells[x, y].elevation)/(float)WorldHeight;

                        Color finalColor = Color.FromHtml(AssetManager.GetBiome(cells[x, y].biomeId).color);
                        if (isWater)
                        {
                            finalColor = waterColor;
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
                    case TerrainMapMode.KOPPEN:
                        image.SetPixel(x, y, KoppenClassification.GetColor(cells[x, y].classification));
                        break;   
                    case TerrainMapMode.DEBUG_PLATES:
                        Color pressureColor = Utility.MultiColourLerp([new Color(0,0,1), new Color(0,0,0,0), new Color(1,0,0)], Mathf.InverseLerp(-1, 1, tiles[x, y].pressure));
                        Color baseColor = Utility.MultiColourLerp([lowFlatColor, lowHillColor, highHillColor], hf);
                        if (AssetManager.GetBiome(cells[x, y].biomeId).type == "water")
                        {
                            baseColor = waterColor;
                        }
                        image.SetPixel(x, y, Utility.MultiColourLerp([pressureColor, baseColor], 0.5f));
                        break;     
                    case TerrainMapMode.DEBUG_COAST:
                        pressureColor = Utility.MultiColourLerp([new Color(0,0,1), new Color(0,0,0,0)], Mathf.Clamp(cells[x, y].coastDist / 30f, 0, 1));
                        baseColor = Utility.MultiColourLerp([lowFlatColor, lowHillColor, highHillColor], hf);
                        if (AssetManager.GetBiome(cells[x, y].biomeId).type == "water")
                        {
                            baseColor = waterColor;
                        }
                        image.SetPixel(x, y, Utility.MultiColourLerp([pressureColor, baseColor], 0.5f));
                        break;  
                    case TerrainMapMode.DEBUG_RAINFALL:
                        pressureColor = Utility.MultiColourLerp([new Color(0,0,0), new Color(0,0,1), new Color(1,1,0)], cells[x,y].GetAnnualRainfall()/3500f);
                        baseColor = Utility.MultiColourLerp([lowFlatColor, lowHillColor, highHillColor], hf);
                        if (AssetManager.GetBiome(cells[x, y].biomeId).type == "water")
                        {
                            baseColor = waterColor;
                        }
                        image.SetPixel(x, y, Utility.MultiColourLerp([pressureColor, baseColor], 0.5f));
                        break;      
                    case TerrainMapMode.DEBUG_WIND:
                        pressureColor = Utility.MultiColourLerp([new Color(0,0,0), new Color(1,0,0)], cells[x, y].julyWindVel.Length()/8.6f);
                        baseColor = Utility.MultiColourLerp([lowFlatColor, lowHillColor, highHillColor], hf);
                        if (AssetManager.GetBiome(cells[x, y].biomeId).type == "water")
                        {
                            baseColor = waterColor;
                        }
                        image.SetPixel(x, y, Utility.MultiColourLerp([pressureColor, baseColor], 0.5f));
                        break;   
                    case TerrainMapMode.DEBUG_TEMP:
                        pressureColor = Utility.MultiColourLerp([new Color(0,0,1), new Color(1,1,1), new Color(1,0,0)], Mathf.InverseLerp(-40, 40, cells[x,y].GetAverageTemp()));
                        baseColor = Utility.MultiColourLerp([lowFlatColor, lowHillColor, highHillColor], hf);
                        if (AssetManager.GetBiome(cells[x, y].biomeId).type == "water")
                        {
                            baseColor = waterColor;
                        }
                        image.SetPixel(x, y, Utility.MultiColourLerp([pressureColor, baseColor], 0.5f));
                        break;        
                    case TerrainMapMode.DEBUG_LATITUDE:
                        pressureColor = Utility.MultiColourLerp([new Color(0,0,1), new Color(1,1,0)], y / (float)WorldSize.Y);
                        baseColor = Utility.MultiColourLerp([lowFlatColor, lowHillColor, highHillColor], hf);
                        if (AssetManager.GetBiome(cells[x, y].biomeId).type == "water")
                        {
                            baseColor = waterColor;
                        }
                        image.SetPixel(x, y, Utility.MultiColourLerp([pressureColor, baseColor], 0.5f));
                        break;   
                }
            }
        }
        return image;
    }
}
[MessagePackObject(AllowPrivate = true)]
public class Cell  
{
    [Key(1)] public int elevation;
    [Key(2)] public float januaryTemp;
    [Key(3)] public float julyTemp;
    [Key(4)] public float januaryPET;
    [Key(5)] public float julyPET;
    [Key(6)] public int januaryRainfall;
    [Key(7)] public int julyRainfall;
    [Key(8)] public Vector2 januaryWindVel;
    [Key(9)] public Vector2 julyWindVel;
    [Key(10)] public float coastDist;
    [Key(11)] public string biomeId;
    [Key(12)] public string classification;
    public float GetTempForMonth(int month)
    {
        float phase = (month / 12f) * Mathf.Pi * 2f;
        float seasonal = (Mathf.Cos(phase - Mathf.Pi) + 1f) * 0.5f;
        return Mathf.Lerp(januaryTemp, julyTemp, seasonal);
    }
    public float GetAverageTemp()
    {
        float annualTemp = (januaryTemp + julyTemp) / 2f;
        return annualTemp;
    }
    public float GetRainfallForMonth(int month)
    {
        float phase = (month / 12f) * Mathf.Pi * 2f;
        float seasonal = (Mathf.Cos(phase - Mathf.Pi) + 1f) * 0.5f;
        return Mathf.Lerp(januaryRainfall, julyRainfall, seasonal);        
    }
    public float GetPETForMonth(int month)
    {
        float phase = (month / 12f) * Mathf.Pi * 2f;
        float seasonal = (Mathf.Cos(phase - Mathf.Pi) + 1f) * 0.5f;
        return (float)Mathf.Lerp(januaryPET, julyPET, seasonal);        
    }
    public float GetAnnualRainfall()
    {
        float annualRainfall = 12f * (januaryRainfall + julyRainfall) / 2f;
        return annualRainfall;
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
    SUMMER_RAINFALL,
    WINTER_RAINFALL,
    BIOMES,
    RIVERS,
    FINISHING
}
public enum TerrainMapMode{
    HEIGHTMAP,
    HEIGHTMAP_REALISTIC,
    REALISTIC,
    KOPPEN,
    DEBUG_PLATES,
    DEBUG_COAST,
    DEBUG_RAINFALL,
    DEBUG_WIND,
    DEBUG_TEMP,
    DEBUG_LATITUDE
}