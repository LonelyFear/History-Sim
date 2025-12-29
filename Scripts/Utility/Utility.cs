
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Vector2 = System.Numerics.Vector2;
public static class Utility
{
    private static Random rng = new Random();

    public static void Shuffle<T>(this IList<T> list, Random r = null)
    {
        if (r == null)
        {
            r = rng;
        }
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = r.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    public static Direction GetDirectionFromVector(Vector2I vec)
    {
        Direction direction;
        switch (vec)
        {
            case Vector2I(-1,0):
                direction = Direction.LEFT;
                break;
            case Vector2I(1,0):
                direction = Direction.RIGHT;
                break;
            case Vector2I(0,-1):
                direction = Direction.UP;
                break;
            case Vector2I(0,1):
                direction = Direction.DOWN;
                break;
            default:
                direction = Direction.LEFT;
                break;
        }
        return direction;
    }
    public static int Vec2ToIndex(Vector2I gridSize, Vector2I pos)
    {
        return pos.X * gridSize.Y + pos.Y;
    }
    public static int Vec2ToIndex(Vector2I gridSize, int x, int y)
    {
        return x * gridSize.Y + y;
    }
    public static T PickRandom<T>(this IList<T> array, Random r = null)
    {
        if (r == null)
        {
            r = rng;
        }
        int length = array.Count();
        return array[r.Next(0, length)];
    }
    public static ObjectType GetObjectType(this NamedObject popObject)
    {
        if (popObject.GetType() == typeof(Culture))
        {
            return ObjectType.CULTURE;
        }
        else if (popObject.GetType() == typeof(Region))
        {
            return ObjectType.REGION;
        }
        else if (popObject.GetType() == typeof(State))
        {
            return ObjectType.STATE;
        }
        else if (popObject.GetType() == typeof(Character))
        {
            return ObjectType.CHARACTER;
        }
        return ObjectType.UNKNOWN;
    }

    public static bool IsSaveValid(string path)
    {
        if (DirAccess.Open(path) != null)
        {
            bool saveDataExists = FileAccess.FileExists(path + "/save_data.json");
            bool terrainDataExists = FileAccess.FileExists(path + "/terrain_data.pxsave");
            bool simDataExists = FileAccess.FileExists(path + "/sim_data.pxsave");
            bool dataWritingFinished = FileAccess.Open(path + "/save_data.json", FileAccess.ModeFlags.Read).GetAsText(true).Length > 0;
            return saveDataExists && terrainDataExists && simDataExists && dataWritingFinished;
        }
        return false;
    }
    public static float CalcWeightedAverage(params (float value, float weight)[] traits)
    {
        float average = 0;
        float totalWeights = 0;
        foreach (var (value, weight) in traits)
        {
            average += value * weight;
            totalWeights += weight;
        }
        return average / totalWeights;
    }
    public static string[] GetAsArray(this FileAccess f)
    {
        List<string> result = new List<string>();
        while (!f.EofReached())
        {
            result.Add(f.GetLine());
        }
        f.Close();
        return result.ToArray();
    }

    public static float NextSingle(this Random rng, float minValue, float maxValue)
    {
        return Mathf.Lerp(rng.NextSingle(), minValue, maxValue);
    }
    public static float WrappedDistanceTo(this Vector2I pointA, Vector2I pointB, Vector2I worldSize)
    {
        float dx = Mathf.Abs(pointB.X - pointA.X);
        float dy = Mathf.Abs(pointB.Y - pointA.Y);
        if (dx > worldSize.X / 2f)
        {
            dx = worldSize.X - dx;
        }
        if (dy > worldSize.Y / 2f)
        {
            dy = worldSize.Y - dy;
        }
        return Mathf.Sqrt(Mathf.Pow(dx, 2) + Mathf.Pow(dy, 2));
    }
    public static float WrappedDistanceSquaredTo(this Vector2I pointA, Vector2I pointB, Vector2I worldSize)
    {
        float dx = Mathf.Abs(pointB.X - pointA.X);
        float dy = Mathf.Abs(pointB.Y - pointA.Y);
        if (dx > worldSize.X / 2f)
        {
            dx = worldSize.X - dx;
        }
        if (dy > worldSize.Y / 2f)
        {
            dy = worldSize.Y - dy;
        }
        return Mathf.Pow(dx, 2) + Mathf.Pow(dy, 2);
    }
    public static Vector2I WrappedDelta(this Vector2I pointA, Vector2I pointB, Vector2I worldSize)
    {
        float dx = pointB.X - pointA.X;
        if (Mathf.Abs(dx) > worldSize.X / 2f)
        {
            dx -= Math.Sign(dx) * worldSize.X;
        }
        float dy = pointB.Y - pointA.Y;
        if (Mathf.Abs(dy) > worldSize.Y / 2f)
        {
            dy -= Math.Sign(dy) * worldSize.Y;
        }
        return new Vector2I((int)dx, (int)dy);
    }
    public static Vector2I WrappedMidpoint(this Vector2I pointA, Vector2I pointB, Vector2I worldSize)
    {

        int dx = pointB.X - pointA.X;
        if (Mathf.Abs(dx) > worldSize.X / 2f)
        {
            dx -= Math.Sign(dx) * worldSize.X;
        }
        int dy = pointB.Y - pointA.Y;
        if (Mathf.Abs(dy) > worldSize.Y / 2f)
        {
            dy -= Math.Sign(dy) * worldSize.Y;
        }
        //GD.Print(dy);
        return new Vector2I(Mathf.RoundToInt(Mathf.PosMod(pointA.X + dx / 2f, worldSize.X)), Mathf.RoundToInt(Mathf.PosMod(pointA.Y + dy / 2f, worldSize.Y)));
    }
    public static float GetWrappedNoise(this FastNoiseLite noise, float x, float y, Vector2I worldSize)
    {
        float nx = y;
        float ny = Mathf.Sin(x * (Mathf.Pi * 2) / worldSize.X) / (Mathf.Pi * 2) * worldSize.X;
        float nz = Mathf.Cos(x * (Mathf.Pi * 2) / worldSize.X) / (Mathf.Pi * 2) * worldSize.X;
        return noise.GetNoise(nx, ny, nz);
    }
    public static Color MultiColourLerp(Color[] colours, float t) {

        t = Mathf.Clamp(t, 0, 1);

        float delta = 1f / (colours.Length - 1);
        int startIndex = (int)(t / delta);

        if(startIndex == colours.Length - 1) {
            return colours[colours.Length - 1];
        }

        float localT = (t % delta) / delta;

        return (colours[startIndex] * (1f - localT)) + (colours[startIndex + 1] * localT);
    }
    public static Vector2 GetGradient(float[,] grid, int x, int y)
    {
        int x0 = Mathf.PosMod(x - 1, grid.GetLength(0));
        int x1 = Mathf.PosMod(x + 1, grid.GetLength(0));
        int y0 = Mathf.PosMod(y - 1, grid.GetLength(1));
        int y1 = Mathf.PosMod(y + 1, grid.GetLength(1));

        float dx = (grid[x1, y] - grid[x0, y]) * 0.5f;
        float dy = (grid[x, y1] - grid[x, y0]) * 0.5f;

        return new Vector2(dx, dy);       
    }
    public static Vector2 GetGradient(int[,] grid, int x, int y)
    {
        int x0 = Mathf.PosMod(x - 1, grid.GetLength(0));
        int x1 = Mathf.PosMod(x + 1, grid.GetLength(0));
        int y0 = Mathf.PosMod(y - 1, grid.GetLength(1));
        int y1 = Mathf.PosMod(y + 1, grid.GetLength(1));

        float dx = (grid[x1, y] - grid[x0, y]) * 0.5f;
        float dy = (grid[x, y1] - grid[x, y0]) * 0.5f;

        return new Vector2(dx, dy);       
    }
}
