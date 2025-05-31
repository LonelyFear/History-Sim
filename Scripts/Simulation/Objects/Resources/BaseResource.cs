using Godot;
using System;
using System.Runtime.CompilerServices;

public abstract class BaseResource
{
    public string name { get; set; } = "Base Resource";
    public string id { get; set; } = "baseresource";
    public float baseWorth { get; set; } = 1;
    public float weight { get; set; } = 1;

    public bool IsFood()
    {
        return GetType() == typeof(FoodResouce);
    }
}
