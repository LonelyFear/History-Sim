using Godot;
using Godot.Collections;
using System;

public partial class BuildingData : GodotObject
{
    public string name = "Building";
    public string id = "building";
    public string description = "the basic building";
    public int monthsToBuild;
    public float cost;
    public long occupancy;
    public Dictionary<string, float> resourcesProducedIds;
    public Dictionary<SimResource, float> resourcesProduced;
    public string[] productiveBiomes = [];
    public float minFertility;
    public float maxFertility;
    public float bestMinFertility;
    public float bestMaxFertility;
    public BuildingType type;
}

public enum BuildingType{
    AGRICULTURE,
    PRODUCTION,
    GOVERNMENT,
    HOUSING,
}
