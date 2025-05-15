
using System;
using System.Collections.Generic;

public static class Utility
{
    private static Random rng = new Random();  

    public static void Shuffle<T>(this IList<T> list)  
    {  
        int n = list.Count;  
        while (n > 1) {  
            n--;  
            int k = rng.Next(n + 1);  
            T value = list[k];  
            list[k] = list[n];  
            list[n] = value;  
        }  
    }
    public static PopObject.ObjectType GetObjectType(this PopObject popObject){
        if (popObject.GetType() == typeof(Culture)){
            return PopObject.ObjectType.CULTURE;
        } else if (popObject.GetType() == typeof(Region)){
            return PopObject.ObjectType.REGION;
        } else if (popObject.GetType() == typeof(State)){
            return PopObject.ObjectType.STATE;
        }
        return PopObject.ObjectType.IDK;
    }

    public static string[] GetAsArray(this Godot.FileAccess f){
        List<string> result = new List<string>();
        while (!f.EofReached()){
            result.Add(f.GetLine());
        }
        f.Close();
        return result.ToArray();
    }
}
