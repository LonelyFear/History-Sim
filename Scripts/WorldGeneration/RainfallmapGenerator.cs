using System;
using System.ComponentModel;
using System.Linq;
using System.Security.AccessControl;
using Godot;

public class RainfallMapGenerator
{
    float[,] map;
    float[,] moistureMap;
    float[,] rainfallMap;
    WorldGenerator world;
    Curve precipitationCurve = GD.Load<Curve>("res://Curves/Climate/PrecipitationCurve.tres");
    Curve evaporationCurve = GD.Load<Curve>("res://Curves/EvaporationCurve.tres");
    Curve simpleEvaporationCurve = GD.Load<Curve>("res://Curves/Climate/SimpleEvaporationCurve.tres");
    public float[,] GenerateRainfallMap(float scale, WorldGenerator world, bool complexRainfallGeneration = false){
        this.world = world;
        if (!complexRainfallGeneration)
        {
            return GenerateSimpleMap(scale);
        }
        return GenerateComplexMap(scale);
    }
    float[,] GenerateComplexMap(float scale)
    {
        moistureMap = new float[world.WorldSize.X, world.WorldSize.Y];
        rainfallMap = new float[world.WorldSize.X, world.WorldSize.Y];

        FastNoiseLite noise = new FastNoiseLite();
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(8);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        noise.SetSeed(world.rng.Next());
        // Evaporation
        RunRainfallPass(50);
        return rainfallMap;         
    }
    void RunRainfallPass(int stepCount)
    {
        float initialMoisture = 0;
        float moistureRatio = 0;
        for (int x = 0; x < world.WorldSize.X; x++)
        {
            for (int y = 0; y < world.WorldSize.Y; y++)
            {
                moistureMap[x,y] = GetEvaporation(x,y);
                initialMoisture += moistureMap[x,y];
            }
        }
        for (int i = 0; i < stepCount; i++)
        {
            float[,] newMap = new float[world.WorldSize.X, world.WorldSize.Y];
            // Moving Moisture
            for (int x = 0; x < world.WorldSize.X; x++)
            {
                for (int y = 0; y < world.WorldSize.Y; y++)
                {

                    Vector2 vel = -world.WindVelMap[x,y];

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
            moistureMap = newMap;
            float stepMoisture = 0;
            // Precipitation
            for (int x = 0; x < world.WorldSize.X; x++)
            {
                for (int y = 0; y < world.WorldSize.Y; y++)
                {
                    float precipitation = moistureMap[x,y] * precipitationCurve.Sample(world.TempMap[x,y]);
                    //if (world.HeightMap[x,y] < world.SeaLevel) continue;
                    moistureMap[x,y] -= precipitation;
                    rainfallMap[x,y] += precipitation;
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
                            rainfallMap[x,y] = Mathf.Lerp(rainfallMap[x,y], rainfallMap[testPos.X, testPos.Y], 0.3f);
                        }
                    }
                }
            }
        }        
    }
    float GetEvaporation(int x, int y)
    {
        double PET = GetPET(x,y);
        if (world.HeightMap[x,y] < world.SeaLevel)
        {
            //return simpleEvaporationCurve.Sample(world.TempMap[x,y]) * 12f;
            return ((float)PET) * 12f;
        }
        //float latitudeFactor = Mathf.Abs((y / world.WorldSize.Y) - 0.5f) * 2f;
        float landEvaporation = 160f * evaporationCurve.Sample(y / (float)world.WorldSize.Y);
        return Mathf.Min((float)PET, landEvaporation) * 12f;
        //return (float)PET * evaporationCurve.Sample(y / (float)world.WorldSize.Y) * 12f;
    }
    double GetPET(int x, int y)
    {
        float latitudeFactor = y / (float)world.WorldSize.Y;

        double dayLength = 12.0;
        double temp = Math.Clamp(world.TempMap[x,y], 0.0, 10000.0);
        double PET;
        /*
        if (temp > 26.5)
        {
            PET = (dayLength/12.0) * (-415.85 + (32.24 * temp) - (0.43 * Math.Pow(temp, 2)));
            return PET;
        }
        */

        double[] monthlyMeanTemperatures = new double[12];
        for (int i = 0; i < 12; i++)
        {
            double monthTemp = temp;
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
        return PET;
    }
    float[,] GenerateSimpleMap(float scale)
    {
        map = new float[world.WorldSize.X, world.WorldSize.Y];
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(8);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        noise.SetSeed(world.rng.Next());
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;
        for (int x = 0; x < world.WorldSize.X; x++)
        {
            for (int y = 0; y < world.WorldSize.Y; y++)
            {
                map[x, y] = Mathf.InverseLerp(-1f, 1f, noise.GetNoise(x / scale, y / scale));
                if (map[x, y] < minValue)
                {
                    minValue = map[x, y];
                }
                if (map[x, y] > maxValue)
                {
                    maxValue = map[x, y];
                }
            }
        }
        for (int y = 0; y < world.WorldSize.Y; y++)
        {
            for (int x = 0; x < world.WorldSize.X; x++)
            {
                map[x, y] = Mathf.InverseLerp(minValue, maxValue, map[x, y]);
                map[x, y] *= Mathf.Clamp(world.TempMap[x, y] * 1f, 0f, 1f);
            }
        }
        return map;        
    }
}