using Godot;
using System;

public partial class SimResource : GodotObject
{
    public string name = "Resource";
    public string id = "defaultresource";
    public float baseCost;
    public ResourceType[] resourceTypes;
}

public enum ResourceType {
    FOOD,
    LUXURY_GOODS,
    CONSTRUCTION,
}
