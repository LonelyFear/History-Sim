using System;
using System.ComponentModel;
using System.Security.AccessControl;
using Godot;

public class RainfallMapGenerator
{
    float[,] map;
    float[,] moistureMap;
    float[,] rainfallMap;
    WorldGenerator world;
    Curve precipitationCurve = GD.Load<Curve>("res://Curves/PrecipitationCurve.tres");
    Curve evaporationCurve = GD.Load<Curve>("res://Curves/EvaporationCurve.tres");
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
        RunRainfallPass(100);
        return rainfallMap;         
    }
    void RunRainfallPass(int stepCount)
    {
        for (int x = 0; x < world.WorldSize.X; x++)
        {
            for (int y = 0; y < world.WorldSize.Y; y++)
            {
                moistureMap[x,y] = Mathf.Max(moistureMap[x,y], GetEvaporation(x,y));
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
                    moistureMap[x,y] += rainfallMap[x,y] * 0.02f * evaporationCurve.Sample(world.GetUnitTemp(world.TempMap[x,y]));

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
            // Precipitation
            for (int x = 0; x < world.WorldSize.X; x++)
            {
                for (int y = 0; y < world.WorldSize.Y; y++)
                {
                    float precipitation = moistureMap[x,y] * precipitationCurve.Sample(Math.Clamp(world.GetUnitTemp(world.TempMap[x,y]), -40, 30));
                    //if (world.HeightMap[x,y] < world.SeaLevel) continue;
                    moistureMap[x,y] -= precipitation;
                    rainfallMap[x,y] += precipitation;

                    rainfallMap[x,y] = Mathf.Clamp(rainfallMap[x,y], 0, 1);
                    moistureMap[x,y] = Mathf.Clamp(moistureMap[x,y], 0, 1);
                }
            }
        }
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
        if (world.HeightMap[x,y] < world.SeaLevel)
        {
            return evaporationCurve.Sample(world.GetUnitTemp(world.TempMap[x,y]));
        }
        return 0.02f * evaporationCurve.Sample(world.GetUnitTemp(world.TempMap[x,y]));
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