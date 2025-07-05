using System;
using Godot;

public class RainfallMapGenerator
{
    float[,] map;

    public float[,] GenerateRainfallMap(float scale){
        map = new float[WorldGenerator.WorldSize.X, WorldGenerator.WorldSize.Y];
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(8);
        noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
        noise.SetSeed(WorldGenerator.rng.Next(-99999, 99999));
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;
        for (int y = 0; y < WorldGenerator.WorldSize.Y; y++)
        {
            for (int x = 0; x < WorldGenerator.WorldSize.X; x++)
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
        for (int y = 0; y < WorldGenerator.WorldSize.Y; y++)
        {
            for (int x = 0; x < WorldGenerator.WorldSize.X; x++)
            {
                map[x, y] = Mathf.Lerp(map[x, y], minValue, maxValue);
                map[x, y] *= Mathf.Clamp(WorldGenerator.TempMap[x, y] * 0.8f, 0f, 1f);
            }
        }
        return map;
    }
}