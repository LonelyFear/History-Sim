using Godot;
using System;
using System.Collections.Generic;
public class WindGenerator()
{
    float[,] windDirMap;
    float[,] windSpeedMap;
    Vector2I worldSize;
    float[,] heightMap;
    Curve prevailingWindCurve = GD.Load<Curve>("res://Curves/PrevailingWindCurve.tres");

    public void GeneratePrevailingWinds(WorldGenerator world)
    {
        worldSize = world.WorldSize;
        heightMap = world.HeightMap;
        windDirMap = new float[worldSize.X, worldSize.Y];
        for (int x = 0; x < worldSize.X; x++)
        {
            for (int y = 0; y < worldSize.Y; y++)
            {
                float latitudeFactor = Mathf.Abs(y - (world.WorldSize.Y / 2f)) / (world.WorldSize.Y / 2f);
                windDirMap[x, y] = prevailingWindCurve.Sample(latitudeFactor);
            }
        }
    }
}