using System;
using System.ComponentModel;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;
using Godot;
using Vector2 = System.Numerics.Vector2;
public class RainfallMapGenerator
{
    float[,] moistureMap;
    WorldGenerator world;
    Curve precipitationCurve = GD.Load<Curve>("res://Curves/Climate/PrecipitationCurve.tres");
    Curve evaporationCurve = GD.Load<Curve>("res://Curves/EvaporationCurve.tres");
    Curve daylightCurve = GD.Load<Curve>("res://Curves/DaylightCurve.tres");
    Curve simpleEvaporationCurve = GD.Load<Curve>("res://Curves/Climate/SimpleEvaporationCurve.tres");
    public void GenerateRainfallMap(WorldGenerator world, out float[,] summerRainfallMap, out float[,] winterRainfallMap){
        this.world = world;
        GenerateComplexMap(out summerRainfallMap, out winterRainfallMap);
    }
    void GenerateComplexMap(out float[,] summerRainfallMap, out float[,] winterRainfallMap)
    {
        moistureMap = new float[world.WorldSize.X, world.WorldSize.Y];

        world.SummerPETMap = new float[world.WorldSize.X, world.WorldSize.Y];
        world.WinterPETMap = new float[world.WorldSize.X, world.WorldSize.Y];

        FastNoiseLite noise = new FastNoiseLite();
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(8);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        noise.SetSeed(world.rng.Next());
        // Evaporation
        summerRainfallMap = RunRainfallPass(25, false);
        world.Stage = WorldGenStage.WINTER_RAINFALL;
        winterRainfallMap = RunRainfallPass(25, true);      
    }
    float[,] RunRainfallPass(int stepCount, bool winter)
    {
        float[,] map = new float[world.WorldSize.X, world.WorldSize.Y];
        float initialMoisture = 0;
        float moistureRatio = 0;
        for (int x = 0; x < world.WorldSize.X; x++)
        {
            for (int y = 0; y < world.WorldSize.Y; y++)
            {
                moistureMap[x,y] = GetEvaporation(x,y, winter);
                initialMoisture += moistureMap[x,y];
            }
        }
        for (int i = 0; i < stepCount; i++)
        {
            float[,] newMap = new float[world.WorldSize.X, world.WorldSize.Y];
            // Moving Moisture
            int divisions = 8;
            Parallel.For(1, divisions + 1, (i) =>
            {
                for (int x = world.WorldSize.X / divisions * (i - 1); x < world.WorldSize.X / divisions * i; x++)
                {
                    for (int y = 0; y < world.WorldSize.Y; y++)
                    {

                        Vector2 vel = winter ? -world.WinterWindVelMap[x,y] : -world.SummerWindVelMap[x,y];
                        
                        float sampleX = x + vel.X;
                        float sampleY = y + vel.Y;

                        Vector2I bottomCorner = new(
                            Mathf.PosMod(Mathf.FloorToInt(sampleX), world.WorldSize.X),
                            Mathf.PosMod(Mathf.FloorToInt(sampleY), world.WorldSize.Y)
                        );

                        Vector2I topCorner = new(
                            Mathf.PosMod(bottomCorner.X + 1, world.WorldSize.X),
                            Mathf.PosMod(bottomCorner.Y + 1, world.WorldSize.Y)
                        );

                        float tx = sampleX - Mathf.Floor(sampleX);
                        float ty = sampleY - Mathf.Floor(sampleY);

                        float bottomX = Mathf.Lerp(
                            moistureMap[bottomCorner.X, bottomCorner.Y],
                            moistureMap[topCorner.X, bottomCorner.Y],
                            tx
                        );

                        float topX = Mathf.Lerp(
                            moistureMap[bottomCorner.X, topCorner.Y],
                            moistureMap[topCorner.X, topCorner.Y],
                            tx
                        );
                        newMap[x, y] = Mathf.Lerp(bottomX, topX, ty);
                    }
                }
            });
            moistureMap = newMap;
            float stepMoisture = 0;
            // Precipitation
            for (int x = 0; x < world.WorldSize.X; x++)
            {
                for (int y = 0; y < world.WorldSize.Y; y++)
                {
                    float precipitation = moistureMap[x,y] * precipitationCurve.Sample(winter ? world.WinterTempMap[x,y] : world.SummerTempMap[x,y]);
                    moistureMap[x,y] -= precipitation;
                    map[x,y] += precipitation;
                    stepMoisture += moistureMap[x,y];
                }
            }
            moistureRatio = 1f - (stepMoisture/initialMoisture);
            GD.Print($"Moisture Precipitated After Step {i}: " + moistureRatio.ToString("#0.0%"));
        }

        GD.Print("Total Moisture Precipitated: " + moistureRatio.ToString("#0.0%"));
        for (int i = 0; i < 3; i++)
        {
            for (int x = 0; x < world.WorldSize.X; x++)
            {
                for (int y = 0; y < world.WorldSize.Y; y++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            Vector2I testPos = new Vector2I(Mathf.PosMod(x + dx, world.WorldSize.X), Mathf.PosMod(y + dy, world.WorldSize.Y));
                            map[x,y] = Mathf.Lerp(map[x,y], map[testPos.X, testPos.Y], 0.3f);
                        }
                    }
                }
            }
        }     
        return map;   
    }
    float GetEvaporation(int x, int y, bool winter)
    {
        double PET = GetPET(world, x,y, winter);
        if (world.HeightMap[x,y] < 0)
        {
            //return simpleEvaporationCurve.Sample(world.TempMap[x,y]) * 12f;
            return (float)PET;
        }
        float latitudeFactor = Mathf.Abs((y / (float)world.WorldSize.Y) - 0.5f) * 2f;
        float landEvaporation = 168f * evaporationCurve.Sample(latitudeFactor);
        return Mathf.Min((float)PET, landEvaporation) * 1;
    }
    public double GetPET(WorldGenerator world, int x, int y, bool winter)
    {
        float latitudeFactor = y / (float)world.WorldSize.Y;

        double dayLength = daylightCurve.Sample(latitudeFactor);
        if (winter)
        {
            dayLength = daylightCurve.Sample(1f - latitudeFactor);
        }
        double temp = Math.Clamp(winter ? world.WinterTempMap[x,y] : world.SummerTempMap[x,y], 0.0, 10000.0);
        double PET;

        double[] monthlyMeanTemperatures = new double[12];
        for (int i = 0; i < 12; i++)
        {
            double monthTemp = world.GetTempForMonth(x, y, i);
            monthlyMeanTemperatures[i] = Math.Pow(monthTemp/5.0, 1.514);
        }
        double heatIndex = monthlyMeanTemperatures.Sum();

        double a = (6.75e-07 * Math.Pow(heatIndex, 3)) - 
        (7.71e-05 * Math.Pow(heatIndex, 2)) + 
        (1.792e-02 * heatIndex) +
        0.49239;

        PET = 16.0 * (dayLength/12.0) * 1d * Math.Pow(10.0 * temp/heatIndex, a);
        if (double.IsNaN(PET))
        {
            PET = 0;
        }

        if (winter)
        {
            world.WinterPETMap[x,y] = (float)PET;
        } else
        {
            world.SummerPETMap[x,y] = (float)PET;
        }
        return PET;
    }
}