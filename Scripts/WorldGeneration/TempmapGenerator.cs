using Godot;
using System;

public class TempmapGenerator
{
    Curve tempCurve = GD.Load<Curve>("res://Curves/TempCurve.tres");
    float[,] map;
    public float[,] GenerateTempMap(float scale, WorldGenerator world){
        map = new float[world.WorldSize.X, world.WorldSize.Y];
        FastNoiseLite noise = new FastNoiseLite(world.rng.Next());
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(8);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        float averageTemp = 0;
        //float[,] falloff = Falloff.GenerateFalloffMap(world.WorldSize.X, world.WorldSize.Y, false, 1, 1.1f);
        GD.Print("Equator Temp: " + (1f - (Mathf.Abs(50 - (world.WorldSize.Y / 2f)) / (world.WorldSize.Y / 2f))));
        for (int x = 0; x < world.WorldSize.X; x++)
        {
            for (int y = 0; y < world.WorldSize.Y; y++)
            {
                float latitudeFactor = 1f - (Mathf.Abs(y - (world.WorldSize.Y / 2f)) / (world.WorldSize.Y / 2f));
                float noiseValue = Mathf.InverseLerp(-1, 1, noise.GetNoise(x / scale, y / scale));
                
                map[x,y] = Mathf.Lerp(Mathf.InverseLerp(WorldGenerator.MinTemperature, WorldGenerator.MaxTemperature, tempCurve.Sample(latitudeFactor)), noiseValue, 0.15f);

                float heightFactor = 6.5f * (world.GetUnitElevation(world.HeightMap[x, y])/1000f);
                if (world.GetUnitElevation(world.HeightMap[x, y]) > 0)
                {
                    map[x, y] -= Mathf.InverseLerp(0, WorldGenerator.MaxTemperature + Mathf.Abs(WorldGenerator.MinTemperature), heightFactor);
                }

                map[x, y] = Mathf.Clamp(map[x,y], 0, 1);
                averageTemp += world.GetUnitTemp(map[x, y]);
            }
        }
        GD.Print((averageTemp/(world.WorldSize.X*world.WorldSize.Y)).ToString("Average: 0.0") + " C");
        return map;
    } 
}
