using Godot;
using System;
using System.Collections.Generic;
public class WindGenerator()
{
    Vector2I worldSize;
    float[,] heightMap;
    Curve prevailingWindCurve = GD.Load<Curve>("res://Curves/PrevailingWindCurve.tres");
    Curve windSpeedCurve = GD.Load<Curve>("res://Curves/WindSpeedCurve.tres");
    public Vector2[,] GeneratePrevailingWinds(WorldGenerator world)
    {
        worldSize = world.WorldSize;
        heightMap = world.HeightMap;

        Vector2[,] windVectorMap = new Vector2[worldSize.X, worldSize.Y];
        float[,] windDirMap = new float[worldSize.X, worldSize.Y];
        float[,] windSpeedMap = new float[worldSize.X, worldSize.Y];

        FastNoiseLite noise = new FastNoiseLite(world.rng.Next());
        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(8);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                float posY = y + (noise.GetNoise(x, y) * 10f);                
                float latitudeFactor = Mathf.Abs(posY - (world.WorldSize.Y / 2f)) / (world.WorldSize.Y / 2f);

                windDirMap[x, y] = prevailingWindCurve.Sample(latitudeFactor);
                windSpeedMap[x, y] = 10 * windSpeedCurve.Sample(posY / world.WorldSize.Y);

                if (heightMap[x, y] > world.SeaLevel)
                {
                    windSpeedMap[x, y] *= 0.5f;
                    float currentElevation = world.HeightMap[x,y];

                    float sampleX = x + GetVector(windSpeedMap[x, y] ,windDirMap[x, y]).X;
                    float sampleY = y + GetVector(windSpeedMap[x, y] ,windDirMap[x, y]).Y;

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

                    float bottomX = Mathf.Lerp(heightMap[bottomCorner.X, bottomCorner.Y], heightMap[topCorner.X, bottomCorner.Y], tx);
                    float topX = Mathf.Lerp(heightMap[bottomCorner.X, topCorner.Y], heightMap[topCorner.X, topCorner.Y],tx);

                    float slope = Mathf.Lerp(bottomX, topX, ty) - currentElevation;

                    if (slope > 0)
                    {
                        windSpeedMap[x, y] *= 0.5f;
                    } else
                    {
                        windSpeedMap[x, y] *= 2f;
                    }                  
                }
            }
        }
        // Blurs wind map
        for (int i = 0; i < 4; i++)
        {
            for (int x = 0; x < worldSize.X; x++)
            {
                for (int y = 0; y < worldSize.Y; y++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            Vector2I testPos = new Vector2I(Mathf.PosMod(x + dx, world.WorldSize.X), Mathf.PosMod(y + dy, world.WorldSize.Y));
                            windSpeedMap[x,y] = Mathf.Lerp(windSpeedMap[x,y], windSpeedMap[testPos.X, testPos.Y], 0.3f);
                            windDirMap[x,y] = Mathf.Lerp(windDirMap[x,y], windDirMap[testPos.X, testPos.Y], 0.3f);
                        }
                    }
                }
            }
        }

        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                float bearing = windDirMap[x,y];
                float speed = windSpeedMap[x,y];
                windVectorMap[x,y] = GetVector(speed, bearing);
                //windVectorMap[x,y] = new Vector2(speed, 0);
            }
        }
        return windVectorMap;
    }
    public Vector2 GetVector(float speed, float bearing)
    {
        return new Vector2(speed * (float)Math.Cos(bearing), speed * (float)Math.Sin(bearing));
    }
}