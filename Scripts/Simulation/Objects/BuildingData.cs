using Godot;
using Godot.Collections;
using System;

public partial class BuildingData : GodotObject
{
    public string name {get; set;} = "Building";
    public string id {get; set;} = "building";
    public string description {get; set;} = "the basic building";
    public int monthsToBuild {get; set;}
    public float cost {get; set;}
    public long occupancy {get; set;}
    public Dictionary<string, float> resourcesProducedIds {get; set;}
    public Dictionary<SimResource, float> resourcesProduced {get; set;} = new Dictionary<SimResource, float>();
    public string[] productiveBiomes {get; set;} = [];
    public float minFertility {get; set;}
    public float maxFertility {get; set;}
    public float bestMinFertility {get; set;}
    public float bestMaxFertility {get; set;}
    public BuildingType type;
}

public enum BuildingType{
    AGRICULTURE,
    PRODUCTION,
    GOVERNMENT,
    HOUSING,
}
