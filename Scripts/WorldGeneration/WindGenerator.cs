using Godot;
using System;
using System.Collections.Generic;
public class WindGenerator()
{
    Vector2I worldSize;
    int[,] heightMap;
    Curve prevailingWindCurve = GD.Load<Curve>("res://Curves/PrevailingWindCurve.tres");
    Curve windSpeedCurve = GD.Load<Curve>("res://Curves/WindSpeedCurve.tres");
    WorldGenerator world;
    public Vector2[,] GeneratePrevailingWinds(WorldGenerator world)
    {
        this.world = world;
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
                float posY = y;

                float normalizedY = posY / world.WorldSize.Y;
                float latitudeFactor = Mathf.Abs(normalizedY - 0.5f) * 2f;

                float dir = prevailingWindCurve.Sample(latitudeFactor);

                windDirMap[x, y] = dir;
                if (normalizedY < 0.5f)
                {
                    windDirMap[x, y] = Mathf.PosMod(180f - dir, 360f);
                }
                
                
                
                windSpeedMap[x, y] = 15f * windSpeedCurve.Sample(posY / world.WorldSize.Y);

                if (heightMap[x, y] > world.SeaLevel * WorldGenerator.WorldHeight)
                {
                    windSpeedMap[x, y] *= 0.5f;
                    Vector2 windVector = GetVector(windSpeedMap[x,y], windDirMap[x,y]);
                    Vector2 terrainGradient = -GetTerrainGradient(x,y) * 0.01f;

                    windVector += terrainGradient;    

                    windSpeedMap[x, y] = windVector.Length();
                    windDirMap[x,y] = GetBearing(windVector);
                }
            }
        }
        // Blurs wind map
        for (int i = 0; i < 4; i++)
        {
            float[,] newWindSpeedMap = new float[worldSize.X, worldSize.Y];
            float[,] newWindDirMap = new float[worldSize.X, worldSize.Y];
            for (int x = 0; x < worldSize.X; x++)
            {
                for (int y = 0; y < worldSize.Y; y++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            Vector2I testPos = new Vector2I(Mathf.PosMod(x + dx, world.WorldSize.X), Mathf.PosMod(y + dy, world.WorldSize.Y));
                            newWindSpeedMap[x,y] = Mathf.Lerp(windSpeedMap[x,y], windSpeedMap[testPos.X, testPos.Y], 0.5f);
                            newWindDirMap[x,y] = Mathf.Lerp(windDirMap[x,y], windDirMap[testPos.X, testPos.Y], 0.5f);
                        }
                    }
                }
            }
            windSpeedMap = newWindSpeedMap;
            windDirMap = newWindDirMap;
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
    public Vector2 GetTerrainGradient(int x, int y)
    {
        int x0 = Mathf.PosMod(x - 1, worldSize.X);
        int x1 = Mathf.PosMod(x + 1, worldSize.X);
        int y0 = Mathf.PosMod(y - 1, worldSize.Y);
        int y1 = Mathf.PosMod(y + 1, worldSize.Y);

        float dx = (world.HeightMap[x1, y] - world.HeightMap[x0, y]) * 0.5f;
        float dy = (world.HeightMap[x, y1] - world.HeightMap[x, y0]) * 0.5f;

        return new Vector2(dx, dy);       
    }
    public Vector2 GetVector(float speed, float bearing)
    {
        //return new(speed,0);
        float rad = Mathf.DegToRad(bearing);
        return new Vector2(speed * Mathf.Sin(rad), speed * Mathf.Cos(rad));
    }
    public float GetBearing(Vector2 vector)
    {
        float rad = Mathf.Atan2(vector.X, vector.Y);
        return Mathf.RadToDeg(rad);
    }
}