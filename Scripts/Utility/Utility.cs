
using System;
using System.Collections.Generic;
using System.Security.Principal;
using Godot;

public static class Utility
{
    private static Random rng = new Random();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    public static PopObject.ObjectType GetObjectType(this PopObject popObject)
    {
        if (popObject.GetType() == typeof(Culture))
        {
            return PopObject.ObjectType.CULTURE;
        }
        else if (popObject.GetType() == typeof(Region))
        {
            return PopObject.ObjectType.REGION;
        }
        else if (popObject.GetType() == typeof(State))
        {
            return PopObject.ObjectType.STATE;
        }
        return PopObject.ObjectType.IDK;
    }

    public static string[] GetAsArray(this Godot.FileAccess f)
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
    public static float WrappedDistanceTo(this Vector2 pointA, Vector2 pointB, Vector2 worldSize)
    {
        float dx = pointA.X - pointB.X;
        float dy = pointA.Y - pointB.Y;
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
    public static float WrappedDistanceTo(this Vector2I pointA, Vector2I pointB, Vector2I worldSize, bool squared = false)
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
    public static Vector2I WrappedDifference(this Vector2I pointA, Vector2I pointB, Vector2I worldSize)
    {
        return new Vector2I(Mathf.PosMod(pointA.X - pointB.X, worldSize.X), Mathf.PosMod(pointA.Y - pointB.Y, worldSize.Y));
    }
    public static Vector2I WrappedMidpoint(this Vector2I pointA, Vector2I pointB, Vector2I worldSize)
    {

        int dx = Mathf.Abs(pointB.X - pointA.X);
        if (dx > worldSize.X / 2f)
        {
            dx -= Math.Sign(dx) * worldSize.X;
        }
        int dy = Mathf.Abs(pointB.Y - pointA.Y);
        if (dy > worldSize.Y / 2f)
        {
            dy -= Math.Sign(dy) * worldSize.Y;
        }
        //GD.Print(dy);
        return new Vector2I(Mathf.RoundToInt(Mathf.PosMod(pointA.X + dx / 2f, worldSize.X)), Mathf.RoundToInt(Mathf.PosMod(pointA.Y + dy / 2f, worldSize.Y)));
    }
}
