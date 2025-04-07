using Godot;
using System;

public partial class SimResource : GodotObject
{
    public string name {get; set;} = "Resource";
    public string id {get; set;} = "defaultresource";
    public float baseCost {get; set;}
    public ResourceType[] resourceTypes {get; set;}
}

public enum ResourceType {
    FOOD,
    LUXURY_GOODS,
    CONSTRUCTION,
}
